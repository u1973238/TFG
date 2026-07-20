using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SpiderSwingingSimple : MonoBehaviour
{
    [Header("Referències d'Arquitectura")]
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Transform cam;
    [SerializeField] private Transform gunTip; // Des d'on surt gràficament el fil
    [SerializeField] private LineRenderer lr; // El component de línia blanca

    [Header("Configuració del Swing")]
    public LayerMask swingableLayers;
    public float maxSwingDistance = 25f;
    public float swingThrustForce = 1500f;

    [Header("Ajustos Físics de la Corda")]
    public float springForce = 4.5f; // Esta implementat peró en el joc no es fa servir, però el deixem per si vols ajustar-lo més endavant
    public float damper = 7f;
    public float massScale = 4.5f;

    private Vector3 swingPoint;
    private SpringJoint joint;
    private Vector2 moveInput;
    private Vector3 currentGrapplePosition;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (lr == null) lr = GetComponent<LineRenderer>();
    }

    void Start()
    {
        // Enllaçem amb Input System
        inputReader.swingStartedEvent += StartSwing;
        inputReader.swingCanceledEvent += StopSwing;
        inputReader.moveEvent += HandleMoveInput;
    }

    void OnDestroy()
    {
        // Ens desubscrivim dels esdeveniments per evitar errors quan l'objecte es destrueix
        inputReader.swingStartedEvent -= StartSwing;
        inputReader.swingCanceledEvent -= StopSwing;
        inputReader.moveEvent -= HandleMoveInput;
    }

    private void HandleMoveInput(Vector2 input)
    {
        moveInput = input;
    }

    void FixedUpdate()
    {
        // Si estem penjats de la teranyina, apliquem les forces del WASD
        if (joint != null)
        {
            ApplySwingMovement();
        }
    }

    void LateUpdate()
    {
        DrawRope();
    }

    private void StartSwing()
    {
        // Fem un raycast des de la càmera cap endavant per trobar un punt de swing vàlid
        RaycastHit hit;
        Ray ray = new Ray(cam.position, cam.forward);

        if (Physics.Raycast(ray, out hit, maxSwingDistance, swingableLayers))
        {
            swingPoint = hit.point;
            
            joint = gameObject.AddComponent<SpringJoint>();
            joint.autoConfigureConnectedAnchor = false;

            if (hit.rigidbody != null) // Aquesta condició comprova si l'objecte impactat té un Rigidbody, indicant que és un objecte mòbil
            {
                // Si té Rigidbody (la pedra), connectem el cos
                joint.connectedBody = hit.rigidbody;

                // Traduïm el punt del món (hit.point) a la posició local de la pedra
                joint.connectedAnchor = hit.transform.InverseTransformPoint(hit.point);
            }
            else
            {
                // Si és un mur estàtic del mapa, l'àncora es queda en coordenades del món normals
                joint.connectedBody = null;
                joint.connectedAnchor = swingPoint;
            }

            float distanceFromPoint = Vector3.Distance(transform.position, swingPoint);

            // CONFIGURACIÓ DE CORDA FIXA (SENSE ESTIRAMENT)
            joint.maxDistance = distanceFromPoint;
            joint.minDistance = 0f;

            // Convertim la molla en un cable rígid
            joint.spring = 15000f;
            joint.damper = 100f;
            joint.massScale = 1f;

            // Activem el visual de la corda
            lr.positionCount = 2;
            currentGrapplePosition = gunTip.position;
        }
    }



    private void StopSwing()
    {
        lr.positionCount = 0;
        if (joint != null)
        {
            Destroy(joint);
        }
    }

    private void ApplySwingMovement()
    {
        // Orientem les forces del WASD respecte cap on està mirant la teva càmera
        Vector3 camForward = cam.forward;
        Vector3 camRight = cam.right;

        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        // Vector de força combinat segons si prems W, S, A o D
        Vector3 thrustDirection = (camForward * moveInput.y) + (camRight * moveInput.x);

        // Apliquem la força contínua al Rigidbody de l'aranya
        rb.AddForce(thrustDirection * swingThrustForce * Time.deltaTime);
    }

    private void DrawRope()
    {
        if (!joint) return;

        // Efecte Lerp: fa que el fil viatgi de forma elàstica fins a l'impacte
        currentGrapplePosition = Vector3.Lerp(currentGrapplePosition, swingPoint, Time.deltaTime * 12f);

        lr.SetPosition(0, gunTip.position);
        lr.SetPosition(1, currentGrapplePosition);
    }
}
