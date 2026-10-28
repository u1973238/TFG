using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SpiderLocomotion : MonoBehaviour
{
    [Header("Referències de l'Arquitectura")]
    [SerializeField] private InputReader inputReader;

    [Header("Configuració de Moviment")]
    public float walkSpeed = 5f;
    public float sprintMultiplier = 1.8f;
    private float currentSpeed;
    public float rotationSpeed = 10f;

    [Header("Suavitat del Moviment")]
    public float groundAcceleration = 40f; // m/s² per arribar a la velocitat de caminar (abans era instantani)
    public float groundDeceleration = 50f; // m/s² per frenar en deixar anar les tecles
    public float airAcceleration = 15f; // Control a l'aire, més suau que a terra

    [Header("Configuració de Salt")]
    public float jumpForce = 5f;
    public float raycastDistance = 1.1f;
    public LayerMask walkableLayers;
    public float groundCheckRadius = 0.4f; // Radi de l'esfera que busca el terra (més tolerant que un sol raig a les vores)
    public float coyoteTime = 0.12f; // Marge per poder saltar just després de sortir d'una vora
    public float jumpBufferTime = 0.12f; // Temps que es recorda la tecla de salt si es prem just abans de tocar terra

    [Header("Seguretat")]
    public float respawnMargin = 50f; // Distància per sota del col·lider més baix del nivell a partir de la qual l'aranya ha caigut del mapa

    private Rigidbody rb;
    private SpiderSwingingSimple swinging;
    private SpiderBodyAnimator bodyAnimator;
    private Vector3 planarVelocity; // Velocitat de control que s'aplica amb MovePosition
    private Vector2 movementInput;
    private bool isGrounded;

    // Si l'aranya trepitja terra (el fa servir, per exemple, el circuit de cèrcols)
    public bool IsGrounded => isGrounded;

    private float lastGroundedTime = -1000f;
    private float lastJumpPressedTime = -1000f;
    private float groundCheckBlockedUntil;
    private Vector3 lastSafePosition;
    private float killHeight = float.NegativeInfinity;

    // So: passes i aterratges
    private bool wasGrounded = true;
    private float maxFallSpeed;
    private float stepTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        swinging = GetComponent<SpiderSwingingSimple>();

        // L'animació del cos (balanceig en caminar, aixafar-se en aterrar...) s'afegeix sola
        bodyAnimator = GetComponent<SpiderBodyAnimator>();
        if (bodyAnimator == null) bodyAnimator = gameObject.AddComponent<SpiderBodyAnimator>();

        // La física va a 50 Hz i la pantalla a 60 o més: la interpolació suavitza el moviment visual entre passos de física
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        lastSafePosition = rb.position;
        killHeight = CalculateKillHeight();

        // Inicialitzem la velocitat dinàmica a la de caminada base
        currentSpeed = walkSpeed;

        // Ens subscrivim als esdeveniments
        inputReader.moveEvent += HandleMoveInput;
        inputReader.jumpEvent += HandleJump;
        inputReader.sprintStartedEvent += StartSprinting;
        inputReader.sprintCanceledEvent += StopSprinting;
    }

    void OnDestroy()
    {
        // Ens desubscrivim dels esdeveniments per evitar errors quan l'objecte es destrueix
        inputReader.moveEvent -= HandleMoveInput;
        inputReader.jumpEvent -= HandleJump;
        inputReader.sprintStartedEvent -= StartSprinting;
        inputReader.sprintCanceledEvent -= StopSprinting;
    }

    private void HandleMoveInput(Vector2 input) => movementInput = input;

    void OnEnable()
    {
        // En tornar d'escalar no volem que continuï el moviment d'abans
        planarVelocity = Vector3.zero;
    }

    // Canvis de velocitat asíncrons
    private void StartSprinting() => currentSpeed = walkSpeed * sprintMultiplier;
    private void StopSprinting() => currentSpeed = walkSpeed;

    private void HandleJump()
    {
        // Només recordem quan s'ha premut; el salt s'executa a FixedUpdate (TryJump) amb el marge del coyote time i del buffer
        // Mentre escalem el component està desactivat i el salt el gestiona SpiderClimbing
        if (enabled) lastJumpPressedTime = Time.time;
    }

    // Actualitzacions de física
    void FixedUpdate()
    {
        CheckGround();
        TryJump();
        HandleMovement();
        CheckFallOutOfMap();
    }

    private void CheckGround()
    {
        // Just després de saltar ignorem el terra uns instants perquè el sensor no el torni a detectar
        if (Time.time < groundCheckBlockedUntil)
        {
            isGrounded = false;
            return;
        }

        // Llancem una esfera cap avall (arriba igual de lluny que el raig original) i ignorem els Triggers
        Debug.DrawRay(rb.position, Vector3.down * raycastDistance, Color.red);
        float castDistance = Mathf.Max(0f, raycastDistance - groundCheckRadius);
        isGrounded = Physics.SphereCast(rb.position, groundCheckRadius, Vector3.down, out RaycastHit hit, castDistance, walkableLayers, QueryTriggerInteraction.Ignore);

        if (isGrounded)
        {
            lastGroundedTime = Time.time;

            // Guardem l'últim punt segur (terra prou pla) per si l'aranya cau fora del mapa
            if (hit.normal.y > 0.7f) lastSafePosition = rb.position;
        }

        // So d'aterratge, més fort com més ràpid queia
        if (!isGrounded)
        {
            maxFallSpeed = Mathf.Max(maxFallSpeed, -rb.linearVelocity.y);
        }
        else if (!wasGrounded)
        {
            if (maxFallSpeed > 2f)
            {
                float intensity = Mathf.Clamp(maxFallSpeed / 12f, 0.3f, 1f);
                AudioManager.Play("land", intensity);
                if (bodyAnimator != null) bodyAnimator.OnLand(intensity);
                if (maxFallSpeed > 5f) Effects.Puff(hit.point, Vector3.up, new Color(0.75f, 0.7f, 0.6f, 0.6f), Mathf.RoundToInt(6 + 10 * intensity), 0.45f, 2.5f);
            }
            maxFallSpeed = 0f;
        }
        wasGrounded = isGrounded;
    }

    private void TryJump()
    {
        bool jumpRequested = Time.time - lastJumpPressedTime <= jumpBufferTime;
        bool canJump = Time.time - lastGroundedTime <= coyoteTime;

        if (jumpRequested && canJump)
        {
            // Anul·lem la velocitat vertical perquè el salt tingui sempre la mateixa alçada (també baixant una rampa)
            Vector3 velocity = rb.linearVelocity;
            velocity.y = 0f;
            rb.linearVelocity = velocity;

            rb.AddForce(Vector3.up * jumpForce * PlayerUpgrades.JumpMultiplier, ForceMode.Impulse);
            AudioManager.Play("jump");
            if (bodyAnimator != null) bodyAnimator.OnJump();

            isGrounded = false;
            lastJumpPressedTime = -1000f;
            lastGroundedTime = -1000f;
            groundCheckBlockedUntil = Time.time + 0.15f;
        }
    }

    private void HandleMovement()
    {
        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;

        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDirection = (camForward * movementInput.y) + (camRight * movementInput.x);

        // Acceleració i frenada suaus en lloc de canvis de velocitat instantanis
        Vector3 targetVelocity = moveDirection * currentSpeed * PlayerUpgrades.RunSpeedMultiplier;
        float acceleration = isGrounded ? (targetVelocity.sqrMagnitude > 0.01f ? groundAcceleration : groundDeceleration) : airAcceleration;
        planarVelocity = Vector3.MoveTowards(planarVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);

        // Penjats d'un fil a l'aire, mana la física del pèndol (el control el fa SpiderSwinging amb forces);
        // si aquí també moguéssim l'aranya amb MovePosition, el fil l'estiraria cap enrere i tremolaria
        bool hangingFromWeb = !isGrounded && swinging != null && swinging.IsWebAttached;
        if (hangingFromWeb) planarVelocity = Vector3.zero;
        else rb.MovePosition(rb.position + planarVelocity * Time.fixedDeltaTime);

        // Passes: més seguides quan corre
        if (isGrounded && planarVelocity.sqrMagnitude > 1f)
        {
            stepTimer -= Time.fixedDeltaTime;
            if (stepTimer <= 0f)
            {
                AudioManager.Play("step", 0.45f, 0.1f);
                stepTimer = currentSpeed > walkSpeed ? 0.12f : 0.18f;
            }
        }

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            // Calculem quina rotació hauria de tenir el personatge per mirar cap al vector de moviment
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);

            // Apliquem un "Slerp" (interpolació esfèrica suau) des de la rotació actual a la desitjada
            // Amb la interpolació activa, la rotació es fa a través del Rigidbody
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed));
        }
        else if (Vector3.Angle(rb.rotation * Vector3.up, Vector3.up) > 1f)
        {
            // Si l'aranya ha quedat inclinada (per exemple, després d'un salt de paret), es redreça suaument
            Vector3 flatForward = Vector3.ProjectOnPlane(rb.rotation * Vector3.forward, Vector3.up);

            // Si mirava just cap amunt (escalant), fem que miri en direcció contrària a la paret
            if (flatForward.sqrMagnitude < 0.001f) flatForward = Vector3.ProjectOnPlane(rb.rotation * Vector3.up, Vector3.up);
            if (flatForward.sqrMagnitude < 0.001f) flatForward = Vector3.forward;

            Quaternion uprightRotation = Quaternion.LookRotation(flatForward.normalized, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, uprightRotation, Time.fixedDeltaTime * rotationSpeed));
        }

    }

    private float CalculateKillHeight()
    {
        // El límit és una mica per sota del col·lider més baix del nivell, així no cal ajustar-lo a mà
        float lowestPoint = float.PositiveInfinity;
        foreach (Collider levelCollider in FindObjectsByType<Collider>())
        {
            if (levelCollider.attachedRigidbody == rb) continue;
            lowestPoint = Mathf.Min(lowestPoint, levelCollider.bounds.min.y);
        }

        return float.IsPositiveInfinity(lowestPoint) ? float.NegativeInfinity : lowestPoint - respawnMargin;
    }

    private void CheckFallOutOfMap()
    {
        if (rb.position.y > killHeight) return;

        // L'aranya ha caigut fora del mapa: la tornem a l'últim punt segur on estava a terra
        if (swinging != null) swinging.ReleaseWeb();
        rb.linearVelocity = Vector3.zero;
        rb.position = lastSafePosition + Vector3.up * 0.5f;
    }
}

