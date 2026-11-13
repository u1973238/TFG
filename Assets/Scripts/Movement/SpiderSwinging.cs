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
    public float swingAcceleration = 10f; // m/s² que afegeixen les tecles WASD mentre es balanceja (impulsar el pèndol)
    public float aimAssistRadius = 1.2f; // Si el centre de la mira no toca res, es busca una superfície a prop (0 = sense ajuda)

    [Header("Deixar anar el fil")]
    public float releaseBoost = 1.5f; // Impuls extra cap endavant en deixar anar el fil a l'aire (m/s)
    public float releaseLift = 2f; // Impuls extra cap amunt en deixar anar el fil (m/s)

    [Header("Ajustos Físics de la Corda")]
    public float springForce = 4.5f; // Esta implementat peró en el joc no es fa servir, però el deixem per si vols ajustar-lo més endavant
    public float damper = 7f;
    public float massScale = 4.5f;

    [Header("Grappling (recollir el fil)")]
    public float grappleReelSpeed = 12f; // Metres per segon que s'escurça el fil mentre es manté el botó de grappling
    public float minRopeLength = 1.5f; // Longitud mínima a la qual es pot recollir el fil

    [Header("Aspecte del Fil")]
    public float ropeWidth = 0.04f; // Gruix del fil (abans era d'1 metre i semblava un rectangle)
    public Color ropeColor = new Color(0.95f, 0.95f, 0.95f, 0.9f);
    public int ropeSegments = 32; // Punts de la línia: en cal uns quants perquè es vegi l'ona en disparar
    public float waveAmplitude = 0.5f; // Mida de l'ona quan el fil surt disparat
    public float waveCount = 3f;
    public float waveDuration = 0.35f; // Temps que tarda l'ona a desaparèixer

    private Vector3 swingPoint;
    private float shootTime;
    private bool ropeArrived;
    private SpringJoint joint;
    private Vector2 moveInput;
    private Vector3 currentGrapplePosition;
    private Rigidbody rb;
    private bool swingHeld = false;
    private bool grappleHeld = false;
    private IWebTarget webTarget; // Si el fil ha tocat una cosa que hi reacciona (una palanca), se l'avisa quan hi arriba

    // Permet que l'escalada sàpiga si hi ha un fil enganxat (llavors no força l'aranya contra la paret)
    public bool IsWebAttached => joint != null;

    // On està enganxat el fil ara mateix (si l'objecte es mou, el punt el segueix)
    public Vector3 AttachPoint => joint != null && joint.connectedBody != null
        ? joint.connectedBody.transform.TransformPoint(joint.connectedAnchor)
        : swingPoint;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (lr == null) lr = GetComponent<LineRenderer>();

        // Configurem la línia perquè sembli un fil prim de teranyina
        lr.useWorldSpace = true;
        lr.widthMultiplier = ropeWidth;
        lr.numCapVertices = 2;
        lr.startColor = ropeColor;
        lr.endColor = ropeColor;
        if (lr.sharedMaterial == null) lr.sharedMaterial = WebConnector.GetWebMaterial();
        lr.positionCount = 0;
    }

    void Start()
    {
        // Enllaçem amb Input System
        inputReader.swingStartedEvent += StartSwing;
        inputReader.swingCanceledEvent += StopSwing;
        inputReader.grappleStartedEvent += StartGrapple;
        inputReader.grappleCanceledEvent += StopGrapple;
        inputReader.moveEvent += HandleMoveInput;
    }

    void OnDestroy()
    {
        // Ens desubscrivim dels esdeveniments per evitar errors quan l'objecte es destrueix
        inputReader.swingStartedEvent -= StartSwing;
        inputReader.swingCanceledEvent -= StopSwing;
        inputReader.grappleStartedEvent -= StartGrapple;
        inputReader.grappleCanceledEvent -= StopGrapple;
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

            // Mentre es manté el grappling, el fil es va recollint
            if (grappleHeld) ReelInWeb();
        }
    }

    void LateUpdate()
    {
        DrawRope();
    }

    private void StartSwing()
    {
        swingHeld = true;
        if (joint == null) ShootWeb();
    }

    private void StopSwing()
    {
        swingHeld = false;

        // Si encara es manté el botó de grappling, el fil continua enganxat
        if (!grappleHeld) ReleaseWebWithBoost();
    }

    private void StartGrapple()
    {
        // Si una missió ensenya el Grappling, fins llavors no es pot fer servir
        if (!PlayerUpgrades.GrappleUnlocked)
        {
            AudioManager.Play("fail", 0.5f);
            if (MissionManager.Instance != null) MissionManager.Instance.ShowNotification("Encara no saps recollir el fil. Ajuda els animals per aprendre'n!", NotificationKind.Warning);
            return;
        }

        grappleHeld = true;

        // Si encara no estàvem enganxats, primer disparem el fil
        if (joint == null) ShootWeb();
    }

    private void StopGrapple()
    {
        grappleHeld = false;
        if (!swingHeld) ReleaseWebWithBoost();
    }

    private void ReelInWeb()
    {
        // Grappling: escurcem el fil i la junta estira l'aranya cap al punt d'ancoratge (o la pedra cap a l'aranya)
        joint.maxDistance = Mathf.Max(minRopeLength, joint.maxDistance - grappleReelSpeed * PlayerUpgrades.GrappleMultiplier * Time.fixedDeltaTime);
    }

    private void ShootWeb()
    {
        // Fem un raycast des de la càmera cap endavant per trobar un punt de swing vàlid
        // (si falla per poc, l'ajuda d'apuntat busca una superfície a prop del centre de la mira)
        if (FindSwingPoint(cam, maxSwingDistance * PlayerUpgrades.WebRangeMultiplier, swingableLayers, aimAssistRadius, rb.position, out RaycastHit hit))
        {
            swingPoint = hit.point;
            webTarget = hit.collider.GetComponentInParent<IWebTarget>();

            joint = gameObject.AddComponent<SpringJoint>();
            joint.autoConfigureConnectedAnchor = false;

            if (hit.rigidbody != null) // Aquesta condició comprova si l'objecte impactat té un Rigidbody, indicant que és un objecte mòbil
            {
                // Si té Rigidbody (la pedra), connectem el cos
                joint.connectedBody = hit.rigidbody;

                // Traduïm el punt del món (hit.point) a la posició local de la pedra
                joint.connectedAnchor = hit.rigidbody.transform.InverseTransformPoint(hit.point);
            }
            else
            {
                // Si és un mur estàtic del mapa, l'àncora es queda en coordenades del món normals
                joint.connectedBody = null;
                joint.connectedAnchor = swingPoint;
            }

            float distanceFromPoint = Vector3.Distance(rb.position, swingPoint);

            // CONFIGURACIÓ DE CORDA FIXA (SENSE ESTIRAMENT)
            joint.maxDistance = distanceFromPoint;
            joint.minDistance = 0f;

            // Convertim la molla en un cable rígid
            joint.spring = 15000f;
            joint.damper = 100f;
            joint.massScale = 1f;

            // Activem el visual de la corda
            lr.positionCount = Mathf.Max(2, ropeSegments);
            currentGrapplePosition = gunTip.position;
            shootTime = Time.time;
            ropeArrived = false;

            AudioManager.Play("web_shoot");
        }
    }

    // Busca on es pot enganxar el fil: primer exactament al centre de la mira i, si no hi ha res, amb una esfera
    // (només punts per sobre de l'aranya, perquè l'ajuda no enganxi el fil al terra). També la fa servir el punt de mira.
    // El fil només s'enganxa a superfícies sòlides: s'ignoren els Triggers (zones invisibles com la d'una palanca o d'un NPC)
    public static bool FindSwingPoint(Transform camera, float maxDistance, LayerMask layers, float assistRadius, Vector3 playerPosition, out RaycastHit hit)
    {
        Ray ray = new Ray(camera.position, camera.forward);
        if (Physics.Raycast(ray, out hit, maxDistance, layers, QueryTriggerInteraction.Ignore)) return true;
        if (assistRadius <= 0f) return false;

        // L'esfera comença a l'altura de l'aranya (no darrere, on és la càmera) perquè no toqui res entre la càmera i l'aranya
        float skip = Mathf.Max(0f, Vector3.Dot(playerPosition - camera.position, camera.forward));
        Ray assistRay = new Ray(camera.position + camera.forward * skip, camera.forward);
        if (Physics.SphereCast(assistRay, assistRadius, out hit, maxDistance - skip, layers, QueryTriggerInteraction.Ignore) && hit.point.y > playerPosition.y + 1f)
        {
            return true;
        }
        return false;
    }

    // Deixar anar el fil a l'aire dona un petit impuls endavant i amunt, per encadenar balancejos amb més fluïdesa
    private void ReleaseWebWithBoost()
    {
        bool wasAttached = joint != null;
        ReleaseWeb();

        Vector3 velocity = rb.linearVelocity;
        if (wasAttached && velocity.sqrMagnitude > 16f)
        {
            Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
            Vector3 boost = (horizontal.sqrMagnitude > 0.01f ? horizontal.normalized * releaseBoost : Vector3.zero) + Vector3.up * releaseLift;
            rb.AddForce(boost, ForceMode.VelocityChange);
        }
    }



    public void ReleaseWeb()
    {
        webTarget = null;
        lr.positionCount = 0;
        if (joint != null)
        {
            AudioManager.Play("web_release", 0.6f);
            Destroy(joint);

            // Buidem la referència de seguida: Destroy no elimina el component fins al final del fotograma,
            // i mentrestant DrawRope intentaria dibuixar una línia de 0 punts
            joint = null;
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

        // Apliquem una acceleració contínua al Rigidbody de l'aranya (independent de la massa)
        rb.AddForce(thrustDirection * swingAcceleration * PlayerUpgrades.SwingMultiplier, ForceMode.Acceleration);
    }

    private void DrawRope()
    {
        if (!joint) return;

        // Si el fil està enganxat a un objecte mòbil (la pedra), el punt final segueix l'objecte
        Vector3 ropeEnd = joint.connectedBody != null
            ? joint.connectedBody.transform.TransformPoint(joint.connectedAnchor)
            : swingPoint;

        // Efecte Lerp: fa que el fil viatgi de forma elàstica fins a l'impacte
        currentGrapplePosition = Vector3.Lerp(currentGrapplePosition, ropeEnd, Time.deltaTime * 12f);

        // So quan la punta del fil arriba al punt d'ancoratge
        if (!ropeArrived && Vector3.Distance(currentGrapplePosition, ropeEnd) < 0.3f)
        {
            ropeArrived = true;
            AudioManager.Play("web_attach", 0.7f);
            Effects.Puff(ropeEnd, (gunTip.position - ropeEnd).normalized, new Color(1f, 1f, 1f, 0.8f), 8, 0.25f, 1.5f);

            // Si el fil ha tocat una cosa que hi reacciona (una palanca), l'avisem ara que hi ha arribat
            IWebTarget target = webTarget;
            webTarget = null;
            if (target is UnityEngine.Object targetObject && targetObject != null) target.OnWebArrived(this);
            if (!joint) return; // La palanca pot haver deixat anar el fil
        }

        // Dibuixem el fil com una línia de molts punts amb una ona que s'esvaeix (com un fil que surt disparat)
        Vector3 start = gunTip.position;
        Vector3 end = currentGrapplePosition;
        Vector3 direction = end - start;
        Vector3 waveAxis = Vector3.Cross(direction, Vector3.up);
        if (waveAxis.sqrMagnitude < 0.0001f) waveAxis = Vector3.right;
        waveAxis = Vector3.Cross(waveAxis.normalized, direction.normalized);

        float amplitude = waveAmplitude * Mathf.Clamp01(1f - (Time.time - shootTime) / waveDuration);
        int count = lr.positionCount;
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / (count - 1);

            // L'ona és zero als dos extrems i màxima al mig
            float wave = Mathf.Sin(t * waveCount * 2f * Mathf.PI) * Mathf.Sin(t * Mathf.PI) * amplitude;
            lr.SetPosition(i, start + direction * t + waveAxis * wave);
        }
    }
}
