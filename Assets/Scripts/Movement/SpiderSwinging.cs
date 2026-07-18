using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SpiderSwingingSimple : MonoBehaviour
{
    [Header("Referències d'Arquitectura")]
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Transform cam; // La teva Main Camera
    [SerializeField] private Transform gunTip; // Des d'on surt gràficament el fil
    [SerializeField] private LineRenderer lr; // El component de línia blanca

    [Header("Configuració del Swing")]
    public LayerMask swingableLayers; // Capa "Walkable" o on et vulguis agafar
    public float maxSwingDistance = 25f;
    public float swingThrustForce = 1500f; // Força d'impuls del WASD a l'aire

    [Header("Ajustos Físics de la Corda")]
    public float springForce = 4.5f;
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
        // Enllaçem amb el teu Input System existent
        inputReader.swingStartedEvent += StartSwing;
        inputReader.swingCanceledEvent += StopSwing;
        inputReader.moveEvent += HandleMoveInput;
    }

    void OnDestroy()
    {
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
        RaycastHit hit;
        Ray ray = new Ray(cam.position, cam.forward);

        if (Physics.Raycast(ray, out hit, maxSwingDistance, swingableLayers))
        {
            swingPoint = hit.point;

            joint = gameObject.AddComponent<SpringJoint>();
            joint.autoConfigureConnectedAnchor = false;

            // ======================================================================
            // CORREGIT: CONTROL D'ESPAI LOCAL VS MÓN (SISTEMA DE SEGURETAT)
            // ======================================================================
            if (hit.rigidbody != null)
            {
                // Si té Rigidbody (la pedra), connectem el cos
                joint.connectedBody = hit.rigidbody;

                // TRUC CLAU: Traduïm el punt del món (hit.point) a la posició local de la pedra
                joint.connectedAnchor = hit.transform.InverseTransformPoint(hit.point);
            }
            else
            {
                // Si és un mur estàtic del mapa, l'àncora es queda en coordenades del món normals
                joint.connectedBody = null;
                joint.connectedAnchor = swingPoint;
            }
            // ======================================================================

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
