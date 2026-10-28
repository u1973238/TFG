using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Referències d'Arquitectura")]
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Transform playerTransform;

    [Header("Configuració de Distàncies")]
    public Vector3 offset = new Vector3(0f, 1.5f, -4f);
    public float sensitivity = 0.2f;
    public float gamepadSensitivity = 150f; // Graus per segon amb el joystick dret del comandament al màxim
    public float smoothSpeed = 8f;
    public LayerMask collisionLayers; // Capes que bloquegen la visió
    public float collisionRadius = 0.3f; // Radi de l'esfera que comprova si hi ha obstacles entre la càmera i el jugador

    [Header("Sensació de Velocitat")]
    public float speedFovBoost = 10f; // Graus que s'obre el camp de visió quan l'aranya va molt de pressa (0 = desactivat)
    public float fovSpeedStart = 8f; // A partir d'aquesta velocitat (m/s) el camp de visió s'obre
    public float fovSpeedFull = 22f; // A aquesta velocitat ja s'ha obert del tot

    private Camera cameraComponent;
    private Rigidbody playerBody;
    private float baseFov;
    private Vector3 lastPlayerPosition;

    private float yaw = 0f;
    private float pitch = 0f;
    private Vector2 lookInput;

    void Start()
    {
        // Ens subscrivim a la lectura de moviment del ratolí
        inputReader.lookEvent += HandleLookInput;

        cameraComponent = GetComponent<Camera>();
        if (cameraComponent != null) baseFov = cameraComponent.fieldOfView;
        if (playerTransform != null)
        {
            playerBody = playerTransform.GetComponent<Rigidbody>();
            lastPlayerPosition = playerTransform.position;
        }

        // Bloquegem el cursor al centre per poder jugar còmodament
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnDestroy()
    {
        // Ens desubscrivim de l'event per evitar errors quan l'objecte es destrueixi
        inputReader.lookEvent -= HandleLookInput;
    }

    private void HandleLookInput(Vector2 input)
    {
        // Guardem l'input del ratolí per utilitzar-lo a LateUpdate
        lookInput = input;
    }

    void LateUpdate()
    {
        if (playerTransform == null) return;

        // 1. Calculem la rotació orbital segons el moviment del ratolí
        // Mentre el joc està aturat (menú, pausa o victòria) no acumulem el moviment, així la càmera no salta en tornar
        if (Time.timeScale > 0f)
        {
            // El ratolí ja dona un desplaçament per fotograma; el joystick dona una direcció i s'escala pel temps
            // La sensibilitat també es pot ajustar des del menú d'opcions (GameSettings)
            float lookScale = inputReader.IsLookFromGamepad
                ? gamepadSensitivity * GameSettings.GamepadSensitivity * Time.deltaTime
                : sensitivity * GameSettings.MouseSensitivity;
            yaw += lookInput.x * lookScale;
            pitch -= lookInput.y * lookScale;
        }

        // Limitem el moviment vertical per evitar que la càmera es giri de cap per avall
        pitch = Mathf.Clamp(pitch, -60f, 60f);

        // 2. Apliquem la rotació acumulada
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        // Calculem on hauria d'estar idealment la càmera
        Vector3 targetPosition = playerTransform.position + (rotation * offset);

        // 3. Sistema d'oclusió bàsic (evitem que la càmera travessi parets si toquen)
        targetPosition = CheckCameraCollision(playerTransform.position + Vector3.up * offset.y, targetPosition);

        // 4. Movem i mirem cap al jugador de forma suau
        // El factor exponencial fa que el suavitzat sigui igual a 60, 144 o més FPS
        float smoothFactor = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothFactor);
        transform.LookAt(playerTransform.position + Vector3.up * offset.y);

        UpdateSpeedFov();
    }

    // Quan l'aranya va molt de pressa (balancejant-se o caient), el camp de visió s'obre una mica i transmet velocitat
    private void UpdateSpeedFov()
    {
        if (cameraComponent == null || Time.deltaTime <= 0f) return;

        Vector3 delta = playerTransform.position - lastPlayerPosition;
        lastPlayerPosition = playerTransform.position;
        float speed = Mathf.Max(delta.magnitude / Time.deltaTime, playerBody != null ? playerBody.linearVelocity.magnitude : 0f);

        float amount = Mathf.InverseLerp(fovSpeedStart, fovSpeedFull, speed);
        float targetFov = baseFov + speedFovBoost * amount;
        cameraComponent.fieldOfView = Mathf.Lerp(cameraComponent.fieldOfView, targetFov, 1f - Mathf.Exp(-3f * Time.deltaTime));
    }

    private Vector3 CheckCameraCollision(Vector3 startPoint, Vector3 endPoint)
    {
        // Llancem una esfera (no un raig) des del jugador fins a la càmera: així no queda enganxada a les vores
        // ni travessa geometries primes. Els Triggers s'ignoren
        Vector3 direction = endPoint - startPoint;
        float distance = direction.magnitude;
        if (distance < 0.001f) return endPoint;
        direction /= distance;

        if (Physics.SphereCast(startPoint, collisionRadius, direction, out RaycastHit hit, distance, collisionLayers, QueryTriggerInteraction.Ignore))
        {
            return startPoint + direction * Mathf.Max(hit.distance - 0.1f, 0.2f);
        }

        return endPoint;
    }
}
