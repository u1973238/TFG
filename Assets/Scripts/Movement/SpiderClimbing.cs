using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SpiderClimbing : MonoBehaviour
{
    [Header("Configuració d'Escalada")]
    public LayerMask climbableLayers;
    public float detectionRange = 0.8f;
    public float climbSpeed = 4f;
    public float rotationSpeed = 12f; // Un xic més ràpida per a una resposta instantània

    [Header("Referències Visuals i de Càmera")]
    [SerializeField] private Transform characterMesh;
    [SerializeField] private Transform cam; // NOU: Arrossega aquí la teva Main Camera!

    [Header("Ajustos del Model Visual a la Paret")]
    public Vector3 climbingRotationOffset = new Vector3(0, 90, 0);

    private Rigidbody rb;
    private SpiderLocomotion locomotion;
    private bool isClimbing = false;
    private Vector3 wallNormal = Vector3.up;

    private Quaternion groundInitialLocalRotation;

    [Header("CONFIGURACIÓ DEL SALT DE PARET")]
    [SerializeField] private InputReader inputReader; // La teva referència d'inputs
    [SerializeField] private float forcaSaltAmunt = 8f;   // Força cap al cel
    [SerializeField] private float forcaSaltEnrere = 10f; // Força per separar-se de la paret
    [SerializeField] private float cooldownTornarAEscalar = 0.25f; // Temps de seguretat a l'aire

    private float tempsCooldownEscalada = 0f;


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        locomotion = GetComponent<SpiderLocomotion>();
    }
    void Start()
    {
        isClimbing = false;
        wallNormal = Vector3.up;

        if (locomotion != null)
        {
            locomotion.enabled = true; // Assegurem que el locomotion comenci activat
        }
        if (characterMesh != null) {
            groundInitialLocalRotation = characterMesh.localRotation;
        }

        if (inputReader != null)
        {
            inputReader.jumpEvent += ExecutarSaltDeParet; // Substitueix 'jumpEvent' pel nom exacte que tinguis al teu InputReader
        }


        // Si no has posat la càmera a l'inspector, la busquem automàticament
        if (cam == null && Camera.main != null)
        {
            cam = Camera.main.transform;
        }

        if (characterMesh != null)
        {
            groundInitialLocalRotation = characterMesh.localRotation;
        }
    }

    void FixedUpdate()
    {
        DetectWall();

        if (isClimbing)
        {
            ExecuteClimbBehavior();
        }
        else
        {
            ResetMeshRotation();
        }
    }

    private void DetectWall()
    {
        bool hitSomething = false;
        RaycastHit hitInfo;
        Vector3 targetNormal = wallNormal;

        // Calculem l'origen una mica separat de la paret per evitar falsos negatius
        Vector3 rayOrigin = transform.position + wallNormal * 0.2f;
        float adjustedRange = detectionRange + 0.2f;

        if (!isClimbing)
        {
            Debug.DrawRay(transform.position, transform.forward * detectionRange, Color.blue);
            hitSomething = Physics.Raycast(transform.position, transform.forward, out hitInfo, detectionRange, climbableLayers);
            if (hitSomething) targetNormal = hitInfo.normal;
        }
        else
        {
            // Triple detecció adaptativa per a angles, sostres i relleus
            // Llamp A - Sota la panxa
            if (Physics.Raycast(rayOrigin, -wallNormal, out RaycastHit hitBelly, adjustedRange, climbableLayers))
            {
                hitSomething = true;
                targetNormal = hitBelly.normal;
            }

            // Llamp B - Davant de l'aranya
            if (Physics.Raycast(rayOrigin, transform.forward * adjustedRange, out RaycastHit hitFront, adjustedRange, climbableLayers))
            {
                hitSomething = true;
                targetNormal = hitFront.normal;
            }

            // Llamp C - Diagonal davant-avall (per saltar vores de plataformes)
            Vector3 diagonalDir = (transform.forward - transform.up).normalized;
            if (Physics.Raycast(rayOrigin, diagonalDir, out RaycastHit hitDiag, adjustedRange * 1.3f, climbableLayers))
            {
                hitSomething = true;
                targetNormal = hitDiag.normal;
            }
        }

        if (hitSomething)
        {
            wallNormal = Vector3.Slerp(wallNormal, targetNormal, Time.fixedDeltaTime * rotationSpeed);
            if (!isClimbing) StartClimbing();
        }
        else
        {
            if (isClimbing) StopClimbing();
        }
    }

    private void StartClimbing()
    {
        isClimbing = true;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;

        if (locomotion != null) locomotion.enabled = false;
    }

    private void StopClimbing()
    {
        isClimbing = false;
        rb.useGravity = true;

        // NOU TRUC: Quan l'aranya baixa de la paret, forcem que el cos real es torni a posar dret 
        // respecte al món, però mantenint cap a on mirava horitzontalment.
        Vector3 currentForward = transform.forward;
        currentForward.y = 0;
        if (currentForward == Vector3.zero) currentForward = Vector3.forward;
        transform.rotation = Quaternion.LookRotation(currentForward.normalized, Vector3.up);

        if (locomotion != null) locomotion.enabled = true;
    }

    private void ExecuteClimbBehavior()
    {
        float verticalInput = Input.GetAxisRaw("Vertical");
        float horizontalInput = Input.GetAxisRaw("Horizontal");

        // ======================================================================
        // CÀLCUL DE MOVIMENT RELATIU A LA PANTALLA (CAMERA-RELATIVE)
        // ======================================================================
        // Creem vectors direccionals projectats a la paret segons la vista de la càmera
        Vector3 screenClimbUp = Vector3.Cross(cam.right, wallNormal).normalized;
        Vector3 screenClimbRight = Vector3.Cross(wallNormal, screenClimbUp).normalized;

        Vector3 climbDirection = (screenClimbUp * verticalInput + screenClimbRight * horizontalInput).normalized;

        // ======================================================================
        // ROTACIÓ DEL COS REAL (ORIENTACIÓ AUTOMÀTICA)
        // ======================================================================
        Quaternion targetBodyRotation;

        if (climbDirection != Vector3.zero)
        {
            // Si ens movem, el cos mira cap a on anem (climbDirection) i les potes van cap a la paret (wallNormal)
            targetBodyRotation = Quaternion.LookRotation(climbDirection, wallNormal);
        }
        else
        {
            // Si estem quiets, mantenim l'eix UP enganxat a la paret sense alterar la direcció de mirada
            targetBodyRotation = Quaternion.FromToRotation(transform.up, wallNormal) * transform.rotation;
        }

        // Girem el Rigidbody real del personatge
        rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetBodyRotation, Time.fixedDeltaTime * rotationSpeed));

        // Girem el teu mesh visual aplicant l'offset que tinguis configurat de la teva malla
        if (characterMesh != null)
        {
            characterMesh.localRotation = Quaternion.Slerp(characterMesh.localRotation, Quaternion.Euler(climbingRotationOffset), Time.fixedDeltaTime * rotationSpeed);
        }

        // Movem l'objecte de forma neta en la direcció calculada
        rb.MovePosition(rb.position + climbDirection * climbSpeed * Time.fixedDeltaTime);
    }
    private void ExecutarSaltDeParet()
    {
        // Només podem fer el salt de paret si estem escalant actualment
        if (!isClimbing) return;

        // Sortim de l'estat d'escalada immediatament
        isClimbing = false;
        if (locomotion != null) locomotion.enabled = true;

        rb.useGravity = true;

        // Calculem la direcció de l'impuls:
        // Utilitzem la 'wallNormal' (que mira cap enfora de la paret) i la sumem amb una mica de força cap amunt
        Vector3 direccioSalt = (wallNormal * forcaSaltEnrere) + (Vector3.up * forcaSaltAmunt);

        // Apliquem la força física al Rigidbody de l'aranya
        rb.linearVelocity = Vector3.zero; // Netejem la velocitat actual perquè el salt sigui perfecte i controlat
        rb.AddForce(direccioSalt, ForceMode.Impulse);

        // Activem el temporitzador de seguretat perquè no es reenganxi immediatament
        tempsCooldownEscalada = cooldownTornarAEscalar;
    }

    private void ResetMeshRotation()
    {
        if (characterMesh != null && characterMesh.localRotation != groundInitialLocalRotation)
        {
            characterMesh.localRotation = Quaternion.Slerp(characterMesh.localRotation, groundInitialLocalRotation, Time.fixedDeltaTime * rotationSpeed);
        }
    }

    void OnDestroy()
    {
        // Ens desubscrivim per evitar errors de memòria
        if (inputReader != null)
        {
            inputReader.jumpEvent -= ExecutarSaltDeParet;
        }
    }


}

