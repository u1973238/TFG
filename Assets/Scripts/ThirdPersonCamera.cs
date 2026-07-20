using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Referències d'Arquitectura")]
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Transform playerTransform;

    [Header("Configuració de Distàncies")]
    public Vector3 offset = new Vector3(0f, 1.5f, -4f);
    public float sensitivity = 0.2f;
    public float smoothSpeed = 8f;
    public LayerMask collisionLayers; // Capes que bloquegen la visió

    private float yaw = 0f;
    private float pitch = 0f;
    private Vector2 lookInput;

    void Start()
    {
        // Ens subscrivim a la lectura de moviment del ratolí
        inputReader.lookEvent += HandleLookInput;

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
        yaw += lookInput.x * sensitivity;
        pitch -= lookInput.y * sensitivity;

        // Limitem el moviment vertical per evitar que la càmera es giri de cap per avall
        pitch = Mathf.Clamp(pitch, -60f, 60f);

        // 2. Apliquem la rotació acumulada
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        // Calculem on hauria d'estar idealment la càmera
        Vector3 targetPosition = playerTransform.position + (rotation * offset);

        // 3. Sistema d'oclusió bàsic (evitem que la càmera travessi parets si toquen)
        targetPosition = CheckCameraCollision(playerTransform.position + Vector3.up * offset.y, targetPosition);

        // 4. Movem i mirem cap al jugador de forma suau
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
        transform.LookAt(playerTransform.position + Vector3.up * offset.y);
    }

    private Vector3 CheckCameraCollision(Vector3 startPoint, Vector3 endPoint)
    {
        // Fem un raycast des del punt de partida fins al punt final per veure si hi ha alguna col·lisió
        RaycastHit hit;
        Vector3 direction = endPoint - startPoint;
        float distance = direction.magnitude;

        if (Physics.Raycast(startPoint, direction.normalized, out hit, distance, collisionLayers))
        {
            return hit.point - direction.normalized * 0.2f;
        }

        return endPoint;
    }
}
