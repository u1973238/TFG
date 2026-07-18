using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SpiderClimbing2 : MonoBehaviour
{
    [Header("Configuració d'Escalada")]
    public LayerMask climbableLayers;
    public float detectionRange = 0.8f;
    public float climbSpeed = 4f;
    public float rotationSpeed = 10f;

    [Header("References Visuals")]
    [SerializeField] private Transform characterMesh;

    [Header("Ajustos del Model Visual a la Paret")]
    public Vector3 climbingRotationOffset = new Vector3(0, 90, 0);

    private Rigidbody rb;
    private SpiderLocomotion locomotion;
    private bool isClimbing = false;
    private Vector3 wallNormal;

    // NOU: Variable per recordar la rotació perfecta que tu li hagis donat a terra
    private Quaternion groundInitialLocalRotation;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        locomotion = GetComponent<SpiderLocomotion>();

        // NOU: Guardem de forma automàtica la teva rotació de l'inspector abans que passi res
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
        Debug.DrawRay(transform.position, transform.forward * detectionRange, Color.blue);
        bool hitWall = Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, detectionRange, climbableLayers);

        if (hitWall)
        {
            wallNormal = hit.normal;
            if (!isClimbing)
            {
                StartClimbing();
            }
        }
        else
        {
            if (isClimbing)
            {
                StopClimbing();
            }
        }
    }

    private void StartClimbing()
    {
        isClimbing = true;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;

        if (locomotion != null)
        {
            locomotion.enabled = false;
        }
    }

    private void StopClimbing()
    {
        isClimbing = false;
        rb.useGravity = true;

        if (locomotion != null)
        {
            locomotion.enabled = true;
        }
    }

    private void ExecuteClimbBehavior()
    {
        if (characterMesh != null)
        {
            Quaternion targetGlobalRotation = Quaternion.FromToRotation(transform.up, wallNormal) * transform.rotation;

            // Apliquem l'offset de la paret respecte a la teva rotació inicial de terra
            targetGlobalRotation *= Quaternion.Euler(climbingRotationOffset);

            characterMesh.rotation = Quaternion.Slerp(characterMesh.rotation, targetGlobalRotation, Time.fixedDeltaTime * rotationSpeed);
        }

        float verticalInput = Input.GetAxisRaw("Vertical");
        float horizontalInput = Input.GetAxisRaw("Horizontal");

        Vector3 climbDirection = (Vector3.up * verticalInput + transform.right * horizontalInput).normalized;
        rb.MovePosition(rb.position + climbDirection * climbSpeed * Time.fixedDeltaTime);
    }

    private void ResetMeshRotation()
    {
        // CORREGIT: En lloc d'anar a Quaternion.identity (0,0,0), tornem de forma suau a la teva configuració de terra
        if (characterMesh != null && characterMesh.localRotation != groundInitialLocalRotation)
        {
            characterMesh.localRotation = Quaternion.Slerp(characterMesh.localRotation, groundInitialLocalRotation, Time.fixedDeltaTime * rotationSpeed);
        }
    }
}
