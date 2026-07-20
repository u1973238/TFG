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

    [Header("Configuració de Salt")]
    public float jumpForce = 5f;
    public float raycastDistance = 1.1f;
    public LayerMask walkableLayers;

    private Rigidbody rb;
    private Vector2 movementInput;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

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

    // Canvis de velocitat asíncrons
    private void StartSprinting() => currentSpeed = walkSpeed * sprintMultiplier;
    private void StopSprinting() => currentSpeed = walkSpeed;

    private void HandleJump()
    {
        // Només permet saltar si estem a terra
        if (isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }
    }

    // Actualitzacions de física
    void FixedUpdate()
    {
        CheckGround();
        HandleMovement();
    }

    private void CheckGround()
    {
        Debug.DrawRay(transform.position, Vector3.down * raycastDistance, Color.red);
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, raycastDistance, walkableLayers))
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
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

        rb.MovePosition(rb.position + moveDirection * currentSpeed * Time.fixedDeltaTime);

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            // Calculem quina rotació hauria de tenir el personatge per mirar cap al vector de moviment
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);

            // Apliquem un "Slerp" (interpolació esfèrica suau) des de la rotació actual a la desitjada
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed);
        }

    }
}

