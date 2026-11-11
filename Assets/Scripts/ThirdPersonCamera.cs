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

    [Header("Ensenyar un lloc (palanques)")]
    public float showBlendTime = 0.6f; // Segons que tarda la càmera a anar-hi i a tornar

    private Camera cameraComponent;
    private Rigidbody playerBody;
    private float baseFov;
    private Vector3 lastPlayerPosition;

    private float yaw = 0f;
    private float pitch = 0f;
    private Vector2 lookInput;

    // La càmera de seguir l'aranya (es continua calculant mentre s'ensenya un lloc, per tornar-hi sense salts)
    private Vector3 followPosition;
    private bool showing;
    private float showStartTime;
    private float showDuration;
    private Vector3 showFrom;
    private Vector3 showTarget;

    public bool IsShowingPoint => showing;

    void Start()
    {
        // Ens subscrivim a la lectura de moviment del ratolí
        inputReader.lookEvent += HandleLookInput;

        cameraComponent = GetComponent<Camera>();
        if (cameraComponent != null) baseFov = cameraComponent.fieldOfView;
        followPosition = transform.position;
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

        // Si es canvia d'escena a mig ensenyar un lloc, l'aranya no es pot quedar bloquejada
        if (showing) inputReader.SetGameplayBlocked(false);
    }

    // La càmera deixa l'aranya uns segons per ensenyar un lloc (per exemple, la roca que acaba d'obrir una palanca)
    // i després hi torna. Mentrestant, l'aranya no es mou ni fa accions
    public void ShowPoint(Vector3 point, float duration, float viewDistance)
    {
        if (playerTransform == null) return;

        // Es mira el lloc des del costat on és l'aranya i una mica des de dalt
        Vector3 toPlayer = playerTransform.position - point;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.01f) toPlayer = -transform.forward;
        Vector3 direction = (toPlayer.normalized + Vector3.up * 0.5f).normalized;

        // Si hi ha alguna cosa pel mig, la càmera s'hi acosta
        float distance = viewDistance;
        if (Physics.SphereCast(point, collisionRadius, direction, out RaycastHit hit, viewDistance, collisionLayers, QueryTriggerInteraction.Ignore))
        {
            distance = Mathf.Max(hit.distance - 0.3f, 2f);
        }

        showTarget = point;
        showFrom = point + direction * distance;
        showStartTime = Time.time;
        showDuration = Mathf.Max(duration, showBlendTime * 2f);
        if (!showing) inputReader.SetGameplayBlocked(true);
        showing = true;
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
        // Mentre el joc està aturat (menú, pausa o victòria) no acumulem el moviment, així la càmera no salta en tornar.
        // Tampoc mentre s'ensenya un lloc: en acabar, la càmera torna a on era
        if (Time.timeScale > 0f && !showing)
        {
            // El ratolí ja dona un desplaçament per fotograma; el joystick dona una direcció i s'escala pel temps
            // La sensibilitat també es pot ajustar des del menú d'opcions (GameSettings)
            float lookScale = inputReader.IsLookFromGamepad
                ? gamepadSensitivity * GameSettings.GamepadSensitivity * Time.deltaTime
                : sensitivity * GameSettings.MouseSensitivity;
            yaw += lookInput.x * lookScale;
            // Amb «Invertir l'eix vertical» (menú d'opcions), moure amunt fa mirar avall
            pitch -= lookInput.y * lookScale * (GameSettings.InvertY ? -1f : 1f);
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
        followPosition = Vector3.Lerp(followPosition, targetPosition, smoothFactor);
        Vector3 lookDirection = playerTransform.position + Vector3.up * offset.y - followPosition;
        Quaternion followRotation = lookDirection.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(lookDirection) : transform.rotation;

        if (showing) UpdateShow(followRotation);
        else transform.SetPositionAndRotation(followPosition, followRotation);

        UpdateSpeedFov();
    }

    // Va cap al lloc que s'ensenya, s'hi queda i torna a l'aranya, amb entrades i sortides suaus
    private void UpdateShow(Quaternion followRotation)
    {
        float elapsed = Time.time - showStartTime;
        if (elapsed >= showDuration)
        {
            showing = false;
            inputReader.SetGameplayBlocked(false);
            transform.SetPositionAndRotation(followPosition, followRotation);
            return;
        }

        // 0 = la càmera de sempre, 1 = ensenyant el lloc
        float weight = Mathf.SmoothStep(0f, 1f, Mathf.Min(elapsed, showDuration - elapsed) / showBlendTime);
        Quaternion showRotation = Quaternion.LookRotation(showTarget - showFrom);
        transform.SetPositionAndRotation(Vector3.Lerp(followPosition, showFrom, weight), Quaternion.Slerp(followRotation, showRotation, weight));
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
