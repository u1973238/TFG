using System.Collections.Generic;
using UnityEngine;

// Palanca que obre una porta (o una roca). S'acciona de tres maneres:
//  - l'aranya la toca;
//  - un pont de teranyina la travessa;
//  - l'aranya hi dispara el fil de balancejar-se (també des de lluny): el fil l'estira i es deixa anar sol.
// El mànec baixa amb un petit rebot, el pom passa de vermell a verd i, la primera vegada, la càmera ensenya la porta.
// També avisa les altres peces del trencaclosques que l'escolten (IPuzzleSwitch), com un PuzzleMover
public class LeverDoorSystem : MonoBehaviour, IPuzzleSwitch, IWebTarget
{
    [Header("Configuració de la Missió")]
    [SerializeField] private string missionID = "salvar_animals";

    [Header("Referències d'Elements")]
    [SerializeField] private Transform doorObject;  // Opcional: també es pot deixar buit i fer servir un PuzzleMover
    [SerializeField] private Transform leverHandle; // El que gira (a la palanca nova, l'eix del mànec)
    [SerializeField] private Renderer stateLight; // Opcional: el pom, que passa de vermell a verd

    [Header("Paràmetres de Moviment")]
    [SerializeField] private Vector3 doorOpenOffset = new Vector3(0, -5f, 0);
    [SerializeField] private float leverTargetRotation = 45f; // Graus que gira el mànec en accionar-la
    [SerializeField] private float leverSwingTime = 0.35f; // Segons que tarda el mànec a baixar
    [SerializeField] private float doorOpenDuration = 2f; // Segons que tarda la porta a obrir-se del tot
    [SerializeField] private float doorCloseDuration = 2f; // Segons que tarda a tornar a tancar-se

    [Header("Comportament")]
    // Un cop activada (per l'aranya, per un pont o pel fil), la palanca es queda baixada i la porta oberta.
    // Desmarca-ho perquè només aguanti mentre l'aranya hi és al costat, un pont la travessa o el fil hi està enganxat
    [SerializeField] private bool stayActivated = true;
    [Tooltip("La primera vegada que s'obre, la càmera ensenya la porta uns segons (si no es veu ja de prop)")]
    [SerializeField] private bool showDoorWhenOpened = true;
    [SerializeField] private float showDoorTime = 3f; // Segons que dura, comptant l'anada i la tornada de la càmera

    [Header("Colors del pom")]
    [SerializeField] private Color offColor = new Color(0.8f, 0.12f, 0.08f);
    [SerializeField] private Color onColor = new Color(0.15f, 0.85f, 0.2f);

    private const float WebReleaseDelay = 0.3f; // El fil estira la palanca i, poc després, es deixa anar sol

    // Qui està activant la palanca
    private bool isPlayerInside = false;
    private readonly HashSet<Collider> websInside = new HashSet<Collider>();
    private SpiderSwingingSimple pullingWeb; // El fil de balancejar-se, si hi està enganxat
    private float releaseWebTime;
    private bool isDoorOpen = false;
    private bool latched = false;
    private bool doorShown = false;

    private Rigidbody doorBody;
    private Vector3 doorInitialPosition;
    private Vector3 doorBase; // On es fa la pols quan la porta es mou
    private Quaternion leverInitialRotation;
    private float leverAmount; // 0 = amunt, 1 = baixada
    private float doorAmount; // 0 = tancada, 1 = oberta
    private float doorStartTime; // La porta espera que la càmera hi arribi
    private bool doorSoundPending;
    private Material lightMaterial;
    private float shownLightAmount = -1f;

    // La palanca està activada (la poden escoltar altres elements del trencaclosques). Si la càmera va a ensenyar la porta,
    // les peces enllaçades esperen que hi arribi
    public bool IsActive => isDoorOpen && Time.time >= doorStartTime;

    void Start()
    {
        if (leverHandle != null) leverInitialRotation = leverHandle.localRotation;
        if (stateLight != null)
        {
            lightMaterial = stateLight.material; // Còpia pròpia: cada palanca té el seu color
            lightMaterial.EnableKeyword("_EMISSION");
        }
        UpdateLight();

        if (doorObject != null)
        {
            doorInitialPosition = doorObject.position;
            Bounds bounds = GetBounds(doorObject);
            doorBase = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);

            // La porta es mou amb un Rigidbody cinemàtic (com el PuzzleMover): així la física sap que es mou
            // i no empeny de cop l'aranya o les roques que hi ha a prop
            doorBody = doorObject.GetComponent<Rigidbody>();
            if (doorBody == null)
            {
                doorBody = doorObject.gameObject.AddComponent<Rigidbody>();
                doorBody.isKinematic = true;
                doorBody.useGravity = false;
            }
            if (doorBody.isKinematic) doorBody.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    void OnDestroy()
    {
        if (lightMaterial != null) Destroy(lightMaterial);
    }

    void Update()
    {
        // Els ponts de teranyina es poden esborrar; traiem els que ja no existeixen
        websInside.RemoveWhere(web => web == null || !web.enabled || !web.gameObject.activeInHierarchy);

        // El fil de balancejar-se aguanta la palanca mentre hi està enganxat
        bool webHolding = pullingWeb != null && pullingWeb.IsWebAttached && Vector3.Distance(pullingWeb.AttachPoint, transform.position) < 3f;
        if (!webHolding) pullingWeb = null;

        // CONDICIÓ D'OBERTURA: l'aranya la toca, un pont la travessa o el fil l'estira (o ja s'havia activat i es queda baixada)
        bool shouldOpen = latched || isPlayerInside || websInside.Count > 0 || webHolding;
        if (shouldOpen != isDoorOpen) SetOpen(shouldOpen);

        // Si la palanca es queda baixada, el fil ja ha fet la feina: es deixa anar sol
        if (pullingWeb != null && latched && Time.time >= releaseWebTime)
        {
            pullingWeb.ReleaseWeb();
            pullingWeb = null;
        }

        // La porta comença a moure's (amb so i pols) quan la càmera ja la mira
        if (doorSoundPending && Time.time >= doorStartTime)
        {
            doorSoundPending = false;
            AudioManager.Play("door");
            if (isDoorOpen) Effects.Puff(doorBase, Vector3.up, new Color(0.75f, 0.68f, 0.55f, 0.8f), 18, 0.7f, 2.5f);
        }

        AnimateLever();
    }

    void FixedUpdate()
    {
        if (doorObject == null) return;

        // Moviment suau de la porta (a FixedUpdate, al ritme de la física)
        bool open = isDoorOpen && Time.time >= doorStartTime;
        float duration = open ? doorOpenDuration : doorCloseDuration;
        doorAmount = Mathf.MoveTowards(doorAmount, open ? 1f : 0f, Time.fixedDeltaTime / Mathf.Max(0.01f, duration));
        Vector3 position = doorInitialPosition + doorOpenOffset * Mathf.SmoothStep(0f, 1f, doorAmount);

        if (doorBody != null && doorBody.isKinematic) doorBody.MovePosition(position);
        else doorObject.position = position;
    }

    // El fil de balancejar-se ha arribat a la palanca: l'estira
    public void OnWebArrived(SpiderSwingingSimple web)
    {
        pullingWeb = web;
        releaseWebTime = Time.time + WebReleaseDelay;
        if (!isDoorOpen) SetOpen(true);
    }

    private void SetOpen(bool open)
    {
        isDoorOpen = open;
        doorStartTime = Time.time;
        AudioManager.Play("lever", open ? 1f : 0.6f);

        if (open)
        {
            if (stayActivated) latched = true;

            // La primera vegada, la càmera ensenya la porta; la porta espera que la càmera hi arribi
            if (showDoorWhenOpened && !doorShown)
            {
                doorShown = true;
                doorStartTime += ShowDoor();
            }

            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.ProgressMission(missionID, 0); // Codi antic: per si la missió es completa amb aquesta acció
            }
        }

        // La porta pròpia fa so en començar a moure's (els PuzzleMover enllaçats ja en fan pel seu compte)
        doorSoundPending = doorObject != null;
    }

    // Ensenya la porta amb la càmera, si no es veu ja de prop. Retorna quants segons s'ha d'esperar perquè la càmera hi arribi
    private float ShowDoor()
    {
        Transform door = doorObject != null ? doorObject : FindLinkedMover();
        ThirdPersonCamera followCamera = FindAnyObjectByType<ThirdPersonCamera>();
        if (door == null || followCamera == null) return 0f;

        Bounds bounds = GetBounds(door);
        Camera cam = followCamera.GetComponent<Camera>();
        if (cam != null)
        {
            Vector3 viewport = cam.WorldToViewportPoint(bounds.center);
            bool alreadyInView = viewport.z > 0f && viewport.z < 15f && viewport.x > 0.15f && viewport.x < 0.85f && viewport.y > 0.15f && viewport.y < 0.85f;
            if (alreadyInView) return 0f;
        }

        // Es mira una mica per sota del centre, per veure com s'enfonsa o es mou
        Vector3 focus = bounds.center - Vector3.up * bounds.size.y * 0.2f;
        float distance = Mathf.Clamp(bounds.size.magnitude * 1.4f, 8f, 25f);
        followCamera.ShowPoint(focus, showDoorTime, distance);
        return followCamera.showBlendTime;
    }

    // Si la palanca no té porta pròpia, busca la que mou un PuzzleMover que l'escolta
    private Transform FindLinkedMover()
    {
        foreach (PuzzleMover mover in FindObjectsByType<PuzzleMover>())
        {
            if (mover.HasSwitch(this)) return mover.MovedObject;
        }
        return null;
    }

    private void AnimateLever()
    {
        float target = isDoorOpen ? 1f : 0f;
        bool pulling = target > leverAmount;
        leverAmount = Mathf.MoveTowards(leverAmount, target, Time.deltaTime / Mathf.Max(0.01f, pulling ? leverSwingTime : leverSwingTime * 2f));

        // En baixar, el mànec arriba amb un petit rebot (clac!); en pujar, torna suau
        float eased = pulling ? EaseOutBack(leverAmount) : Mathf.SmoothStep(0f, 1f, leverAmount);
        if (leverHandle != null) leverHandle.localRotation = leverInitialRotation * Quaternion.Euler(leverTargetRotation * eased, 0f, 0f);
        UpdateLight();
    }

    // El pom passa de vermell (tancada) a verd (oberta) i brilla una mica més quan està oberta
    // (si brilla massa, amb el postprocessat el color es torna blanc)
    private void UpdateLight()
    {
        if (lightMaterial == null || Mathf.Approximately(shownLightAmount, leverAmount)) return;
        shownLightAmount = leverAmount;

        Color color = Color.Lerp(offColor, onColor, leverAmount);
        if (lightMaterial.HasProperty("_BaseColor")) lightMaterial.SetColor("_BaseColor", color);
        if (lightMaterial.HasProperty("_EmissionColor")) lightMaterial.SetColor("_EmissionColor", color * Mathf.Lerp(0.25f, 0.5f, leverAmount));
    }

    private static float EaseOutBack(float t)
    {
        const float overshoot = 1.70158f;
        float u = t - 1f;
        return 1f + (overshoot + 1f) * u * u * u + overshoot * u * u;
    }

    private static Bounds GetBounds(Transform target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(target.position, Vector3.one * 2f);
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);
        return bounds;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Cas A: És el jugador (l'aranya)
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
        }

        // Cas B: És una teranyina (objecte generat a la capa Climbing)
        if (other.gameObject.layer == LayerMask.NameToLayer("Climbing"))
        {
            websInside.Add(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Cas A: L'aranya marxa
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
        }

        // Cas B: La teranyina marxa o desapareix
        if (other.gameObject.layer == LayerMask.NameToLayer("Climbing"))
        {
            websInside.Remove(other);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // A l'editor es veu quina porta obre la palanca i on anirà
        if (doorObject == null) return;
        Gizmos.color = new Color(0.3f, 0.95f, 0.35f, 0.8f);
        Gizmos.DrawLine(transform.position, doorObject.position);
        Vector3 from = Application.isPlaying ? doorInitialPosition : doorObject.position;
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.8f);
        Gizmos.DrawLine(from, from + doorOpenOffset);
    }
}
