using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SpiderClimbing : MonoBehaviour
{
    [Header("Configuració d'Escalada")]
    public LayerMask climbableLayers;
    public float detectionRange = 0.8f;
    public float climbSpeed = 4f;
    public float sprintClimbMultiplier = 1.6f; // Mantenint Córrer, l'aranya escala més ràpid
    public float rotationSpeed = 12f;

    [Header("Adherència a la Superfície")]
    public float surfaceStickTime = 0.25f; // Temps que l'aranya aguanta sense detectar la superfície abans de deixar-la anar
    public float surfaceSnapSpeed = 5f; // Velocitat màxima amb què es corregeix la distància a la superfície
    public float surfaceSkin = 0.05f; // Petit marge entre el cos i la superfície

    [Header("Referències Visuals i de Càmera")]
    [SerializeField] private Transform characterMesh;
    [SerializeField] private Transform cam;

    [Header("Ajustos del Model Visual a la Paret")]
    public Vector3 climbingRotationOffset = new Vector3(0, 90, 0);

    private Rigidbody rb;
    private SpiderLocomotion locomotion;
    private bool isClimbing = false;
    private Vector3 wallNormal = Vector3.up;
    private Vector2 moveInput;
    private float currentClimbMultiplier = 1f;
    private float climbBlockedUntil;
    private float climbStepTimer;

    private SpiderSwingingSimple swinging;
    private SpiderBodyAnimator bodyAnimator;
    private float bodyRadius = 1f;

    // El model visual de l'aranya (el fa servir també SpiderBodyAnimator)
    public Transform CharacterMesh => characterMesh;
    public bool IsClimbing => isClimbing;
    private float lastSurfaceTime;
    private bool hasSurfaceHit;
    private Vector3 surfacePoint;
    private Vector3 surfaceHitNormal;

    private Quaternion groundInitialLocalRotation;

    [Header("CONFIGURACIÓ DEL SALT DE PARET")]
    [SerializeField] private InputReader inputReader;
    [SerializeField] private float forcaSaltAmunt = 8f; 
    [SerializeField] private float forcaSaltEnrere = 10f; 
    [SerializeField] private float cooldownTornarAEscalar = 0.25f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        locomotion = GetComponent<SpiderLocomotion>();
        swinging = GetComponent<SpiderSwingingSimple>();
    }
    void Start()
    {
        isClimbing = false;
        wallNormal = Vector3.up;

        // Radi del cos de l'aranya, per saber a quina distància de la superfície l'hem de mantenir
        Collider bodyCollider = GetComponent<Collider>();
        if (bodyCollider != null && bodyCollider.bounds.extents.y > 0f) bodyRadius = bodyCollider.bounds.extents.y;

        if (locomotion != null)
        {
            locomotion.enabled = true; // Assegurem que el locomotion comenci activat
            // Em donava problemes a la hora d'inicar el joc que el locomotion estigués desactivat, així que ho deixo activat per defecte i només el desactivo quan escalem.
        }
        if (characterMesh != null) {
            groundInitialLocalRotation = characterMesh.localRotation;
        }

        if (inputReader != null)
        {
            inputReader.jumpEvent += ExecutarSaltDeParet; // Substitoeixo 'jumpEvent'
            inputReader.moveEvent += HandleMoveInput; // El moviment per la paret també arriba des de l'InputReader
            inputReader.sprintStartedEvent += StartSprintClimb;
            inputReader.sprintCanceledEvent += StopSprintClimb;
        }

        // Si no hi ha la càmera a l'inspector, la busquem
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
        hasSurfaceHit = false;

        // Just després d'un salt de paret esperem uns instants abans de tornar-nos a enganxar
        if (!isClimbing && Time.time < climbBlockedUntil) return;

        bool hitSomething = false;
        RaycastHit hitInfo;
        Vector3 targetNormal = wallNormal;

        // Fem servir la posició i la rotació del Rigidbody (amb la interpolació activa, el Transform va un pas de física endarrerit)
        Vector3 bodyPosition = rb.position;
        Vector3 bodyForward = rb.rotation * Vector3.forward;
        Vector3 bodyUp = rb.rotation * Vector3.up;

        // Calculem l'origen una mica separat de la paret per evitar falsos negatius
        Vector3 rayOrigin = bodyPosition + wallNormal * 0.2f;
        float adjustedRange = detectionRange + 0.2f;

        if (!isClimbing)
        {
            Debug.DrawRay(bodyPosition, bodyForward * detectionRange, Color.blue);
            hitSomething = Physics.Raycast(bodyPosition, bodyForward, out hitInfo, detectionRange, climbableLayers);
            if (hitSomething) targetNormal = hitInfo.normal;
        }
        else
        {
            // Triple detecció adaptativa per a angles, sostres i relleus
            // Sota la panxa
            if (Physics.Raycast(rayOrigin, -wallNormal, out RaycastHit hitBelly, adjustedRange, climbableLayers))
            {
                hitSomething = true;
                targetNormal = hitBelly.normal;

                // Guardem el punt de contacte per mantenir l'aranya enganxada a la superfície (adherència)
                hasSurfaceHit = true;
                surfacePoint = hitBelly.point;
                surfaceHitNormal = hitBelly.normal;
            }

            // Davant de l'aranya
            if (Physics.Raycast(rayOrigin, bodyForward * adjustedRange, out RaycastHit hitFront, adjustedRange, climbableLayers))
            {
                hitSomething = true;
                targetNormal = hitFront.normal;
            }

            // Diagonal davant-avall (per saltar cantonades)
            Vector3 diagonalDir = (bodyForward - bodyUp).normalized;
            if (Physics.Raycast(rayOrigin, diagonalDir, out RaycastHit hitDiag, adjustedRange * 1.3f, climbableLayers))
            {
                hitSomething = true;
                targetNormal = hitDiag.normal;
            }

            // Cantonades exteriors (per exemple, en arribar dalt d'una paret o en voltar una branca):
            // des d'un punt davant i per sota de la superfície mirem enrere per trobar la cara de l'altra banda
            if (!hitSomething)
            {
                Vector3 wrapOrigin = bodyPosition + bodyForward * (bodyRadius + 0.3f) - wallNormal * (bodyRadius + 0.5f);
                if (Physics.Raycast(wrapOrigin, -bodyForward, out RaycastHit hitWrap, bodyRadius + 0.8f, climbableLayers))
                {
                    hitSomething = true;
                    targetNormal = hitWrap.normal;
                }
            }
        }

        if (hitSomething)
        {
            lastSurfaceTime = Time.time;

            if (!isClimbing)
            {
                // En enganxar-nos fem servir directament la normal de la paret nova; si no, els primers instants
                // el raig de la panxa apuntaria cap a la superfície anterior. El cos ja gira suaument per separat
                wallNormal = targetNormal;
                StartClimbing();
            }
            else
            {
                wallNormal = Vector3.Slerp(wallNormal, targetNormal, Time.fixedDeltaTime * rotationSpeed);
            }
        }
        else if (isClimbing)
        {
            // No deixem anar la superfície per un sol raig fallit (cantonades, sostres corbats, juntes de la malla):
            // només si fa una estona que no la trobem. Si hi ha un fil enganxat, el fil mana i es deixa anar de seguida
            float stickTime = IsWebAttached() ? 0f : surfaceStickTime;
            if (Time.time - lastSurfaceTime > stickTime) StopClimbing();
        }
    }

    private bool IsWebAttached()
    {
        return swinging != null && swinging.IsWebAttached;
    }

    private void StartClimbing()
    {
        isClimbing = true;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        AudioManager.Play("grab", 0.7f);

        if (locomotion != null) locomotion.enabled = false;
    }

    private void StopClimbing()
    {
        isClimbing = false;
        rb.useGravity = true;

        // Quan l'aranya baixa de la paret, forcem que el cos real es torni a posar dret 
        Vector3 currentForward = rb.rotation * Vector3.forward;
        currentForward.y = 0;
        if (currentForward == Vector3.zero) currentForward = Vector3.forward;
        rb.rotation = Quaternion.LookRotation(currentForward.normalized, Vector3.up);

        if (locomotion != null) locomotion.enabled = true;
    }

    private void HandleMoveInput(Vector2 input) => moveInput = input;
    private void StartSprintClimb() => currentClimbMultiplier = sprintClimbMultiplier;
    private void StopSprintClimb() => currentClimbMultiplier = 1f;

    private void ExecuteClimbBehavior()
    {
        float verticalInput = moveInput.y;
        float horizontalInput = moveInput.x;

        // Creem vectors direccionals projectats a la paret segons la vista de la càmera
        Vector3 screenClimbUp = Vector3.Cross(cam.right, wallNormal).normalized;
        Vector3 screenClimbRight = Vector3.Cross(wallNormal, screenClimbUp).normalized;

        Vector3 climbDirection = (screenClimbUp * verticalInput + screenClimbRight * horizontalInput).normalized;

        Quaternion targetBodyRotation;

        if (climbDirection != Vector3.zero)
        {
            // Si ens movem, el cos mira cap a on anem (climbDirection) i les potes van cap a la paret (wallNormal)
            targetBodyRotation = Quaternion.LookRotation(climbDirection, wallNormal);
        }
        else
        {
            // Si estem quiets, mantenim l'eix UP enganxat a la paret sense alterar la direcció de mirada
            targetBodyRotation = Quaternion.FromToRotation(rb.rotation * Vector3.up, wallNormal) * rb.rotation;
        }

        // Girem el Rigidbody real del personatge
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetBodyRotation, Time.fixedDeltaTime * rotationSpeed));

        // Girem el teu mesh visual aplicant l'offset
        if (characterMesh != null)
        {
            characterMesh.localRotation = Quaternion.Slerp(characterMesh.localRotation, Quaternion.Euler(climbingRotationOffset), Time.fixedDeltaTime * rotationSpeed);
        }

        // Movem l'objecte en la direcció calculada (més ràpid si es manté Córrer)
        Vector3 movement = climbDirection * climbSpeed * currentClimbMultiplier * PlayerUpgrades.ClimbSpeedMultiplier * Time.fixedDeltaTime;

        // Passes mentre escala (més seguides si corre)
        if (climbDirection != Vector3.zero)
        {
            climbStepTimer -= Time.fixedDeltaTime;
            if (climbStepTimer <= 0f)
            {
                AudioManager.Play("step", 0.4f, 0.1f);
                climbStepTimer = 0.16f / currentClimbMultiplier;
            }
        }

        // Adherència (si no hi ha cap fil enganxat, que llavors mana el fil)
        if (!IsWebAttached())
        {
            // Mentre escalem, l'aranya només es mou amb MovePosition: anul·lem qualsevol velocitat que li hagi quedat
            // (per exemple, d'un cop contra el sostre), perquè no se'n vagi separant de la superfície a poc a poc
            rb.linearVelocity = Vector3.zero;

            float maxCorrection = surfaceSnapSpeed * Time.fixedDeltaTime;
            if (hasSurfaceHit)
            {
                // La mantenim a la distància justa de la superfície: si s'allunya (superfícies corbes) l'apropem i si s'hi fica la separem
                float surfaceGap = Vector3.Dot(rb.position - surfacePoint, surfaceHitNormal);
                float correction = Mathf.Clamp(surfaceGap - (bodyRadius + surfaceSkin), -maxCorrection, maxCorrection);
                movement -= surfaceHitNormal * correction;
            }
            else
            {
                // Hem perdut la superfície un moment: ens hi apropem per tornar-la a trobar
                movement -= wallNormal * maxCorrection;
            }
        }

        rb.MovePosition(rb.position + movement);
    }
    private void ExecutarSaltDeParet()
    {
        // Només podem fer el salt de paret si estem escalant actualment
        if (!isClimbing) return;

        // Sortim de l'estat d'escalada immediatament
        isClimbing = false;
        if (locomotion != null) locomotion.enabled = true;

        // Evitem que l'aranya es torni a enganxar a la mateixa paret just després de saltar
        climbBlockedUntil = Time.time + cooldownTornarAEscalar;

        rb.useGravity = true;

        // Calculem la direcció de l'impuls: sempre cap enfora de la superfície i, si és una paret, també cap amunt
        // (en un sostre l'impuls cap amunt empenyeria l'aranya contra el sostre, per això es redueix fins a zero)
        float impulsAmunt = forcaSaltAmunt * Mathf.Clamp01(wallNormal.y + 1f);
        Vector3 direccioSalt = (wallNormal * forcaSaltEnrere) + (Vector3.up * impulsAmunt);

        // Apliquem la força física al Rigidbody de l'aranya
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(direccioSalt, ForceMode.Impulse);
        AudioManager.Play("jump");
        if (bodyAnimator == null) bodyAnimator = GetComponent<SpiderBodyAnimator>();
        if (bodyAnimator != null) bodyAnimator.OnJump();
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
            inputReader.moveEvent -= HandleMoveInput;
            inputReader.sprintStartedEvent -= StartSprintClimb;
            inputReader.sprintCanceledEvent -= StopSprintClimb;
        }
    }


}

