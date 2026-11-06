using TMPro;
using UnityEngine;

// Animació procedural dels NPCs (el model no té ossos ni animacions):
// respiren, es mouen una mica, es giren cap a l'aranya quan s'hi acosta i fan un saltet quan parlen.
// A sobre mostren un indicador: «!» si tenen una missió per donar, «...» mentre està en curs i «?» quan es pot lliurar
public class NPCAnimator : MonoBehaviour
{
    // Mesures de l'indicador, en proporció de la mida de la lletra. L'espai i el moviment no creixen de lluny, perquè no se separi del cap
    private const float MarkerHalfHeight = 0.4f; // Mig «!» (amb la vora negra)
    private const float MarkerGap = 0.25f; // Espai entre el cap i l'indicador (més que el que flota, perquè no el toqui mai)
    private const float MarkerFloat = 0.1f; // Quant puja i baixa l'indicador

    [Header("Animació en repòs")]
    public float breathAmount = 0.04f; // Quant s'infla en respirar (proporció de la mida)
    public float breathSpeed = 2.2f;
    public float bobAmount = 0.03f; // Quant puja i baixa (proporció de l'alçada)
    public float bobSpeed = 1.3f;

    [Header("Mirar l'aranya")]
    public bool lookAtPlayer = true;
    public float lookDistance = 18f;
    public float turnSpeed = 4f;
    public float modelForwardYaw = 0f; // Si el model mira cap a un altre costat, gira aquest angle (graus) per corregir-ho

    [Header("Parlar")]
    public float hopHeight = 0.25f; // Alçada del saltet (proporció de l'alçada)
    public float hopDuration = 0.35f;

    [Header("Indicador de missió")]
    public bool showMarker = true;
    [Tooltip("Mida de l'indicador: 1 = la mida d'abans, 2 = el doble. Es pot provar mentre es juga")]
    [Range(0.5f, 4f)] public float markerSize = 2f;
    [Tooltip("Més lluny d'aquesta distància de la càmera (metres), l'indicador ja no es veu més petit: creix perquè es trobi de lluny. 0 = es fa petit amb la distància, com qualsevol objecte")]
    public float markerKeepSizeDistance = 12f;
    public Color markerAvailableColor = new Color(1f, 0.84f, 0.29f);
    public Color markerActiveColor = new Color(1f, 1f, 1f, 0.7f);
    public Color markerDeliverColor = new Color(1f, 0.84f, 0.29f);
    public Color markerLockedColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);

    private Transform visual;
    private Vector3 baseLocalPosition;
    private Vector3 baseLocalScale;
    private Quaternion baseWorldRotation;
    private float visualHeight = 1f;
    private float headTop = 1f; // Metres des del punt del model fins a dalt de tot del cap
    private float currentYaw = 0f;
    private float hopStartTime = -10f;
    private float timeOffset;

    private Transform player;
    private MissionNPC missionNPC;
    private TextMeshPro marker;

    void Start()
    {
        missionNPC = GetComponent<MissionNPC>();
        timeOffset = Random.Range(0f, 10f); // Perquè no respirin tots alhora

        visual = FindVisual();
        if (visual != null)
        {
            baseLocalPosition = visual.localPosition;
            baseLocalScale = visual.localScale;
            baseWorldRotation = visual.rotation;

            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);
                visualHeight = Mathf.Max(bounds.size.y, 0.1f);
                headTop = bounds.max.y - visual.position.y;
            }
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) player = playerObject.transform;

        if (showMarker && missionNPC != null && missionNPC.missionToGive != null) CreateMarker();
    }

    // El cridem quan el jugador parla amb l'NPC
    public void Talk()
    {
        hopStartTime = Time.time;
    }

    // Agafa la posició i el gir actuals com a punt de repòs (per exemple, després que l'NPC hagi caminat fins a un altre lloc)
    public void RecaptureBase()
    {
        if (visual == null) return;
        baseLocalPosition = visual.localPosition;
        baseWorldRotation = visual.rotation;
        currentYaw = 0f;
    }

    void Update()
    {
        if (visual == null) return;

        float t = Time.time + timeOffset;

        // Respiració: s'infla una mica cap amunt i s'aprima pels costats
        float breath = Mathf.Sin(t * breathSpeed) * breathAmount;
        Vector3 scale = baseLocalScale;
        scale.y *= 1f + breath;
        scale.x *= 1f - breath * 0.5f;
        scale.z *= 1f - breath * 0.5f;

        // Saltet en parlar: puja i baixa amb una corba suau i s'aixafa una mica en tocar terra
        float hopProgress = (Time.time - hopStartTime) / hopDuration;
        float hop = 0f;
        if (hopProgress >= 0f && hopProgress <= 1f)
        {
            hop = Mathf.Sin(hopProgress * Mathf.PI) * hopHeight;
            float squash = Mathf.Sin(hopProgress * Mathf.PI * 2f) * 0.08f;
            scale.y *= 1f + squash;
        }
        visual.localScale = scale;

        // Moviment amunt i avall (en metres del món, convertits a l'espai del pare)
        float worldOffset = (Mathf.Sin(t * bobSpeed) * bobAmount + hop) * visualHeight;
        Vector3 localOffset = visual.parent != null ? visual.parent.InverseTransformVector(Vector3.up * worldOffset) : Vector3.up * worldOffset;
        visual.localPosition = baseLocalPosition + localOffset;

        // Es gira cap a l'aranya quan és a prop; si no, torna a la posició inicial
        float targetYaw = 0f;
        if (lookAtPlayer && player != null)
        {
            Vector3 toPlayer = player.position - visual.position;
            toPlayer.y = 0f;
            if (toPlayer.magnitude < lookDistance && toPlayer.sqrMagnitude > 0.01f)
            {
                Vector3 baseForward = Quaternion.Euler(0f, modelForwardYaw, 0f) * (baseWorldRotation * Vector3.forward);
                baseForward.y = 0f;
                if (baseForward.sqrMagnitude > 0.001f) targetYaw = Vector3.SignedAngle(baseForward, toPlayer, Vector3.up);
            }
        }
        currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, Time.deltaTime * turnSpeed);
        visual.rotation = Quaternion.AngleAxis(currentYaw, Vector3.up) * baseWorldRotation;
    }

    void LateUpdate()
    {
        if (marker == null || visual == null) return;

        // L'indicador flota per sobre del cap, sense tocar-lo. La mida es llegeix cada fotograma perquè es pugui ajustar mentre es juga
        float baseLetter = BaseLetterSize();
        float letter = LetterSize(baseLetter);
        marker.fontSize = letter * 10f;
        float floating = Mathf.Sin((Time.time + timeOffset) * 3f) * MarkerFloat * baseLetter;
        marker.transform.position = MarkerRestPosition(baseLetter, letter) + Vector3.up * floating;

        MissionData mission = missionNPC.missionToGive;
        if (!mission.isActive && !mission.isCompleted)
        {
            bool locked = MissionManager.Instance != null && !MissionManager.Instance.CanAccept(mission);
            marker.text = "!";
            marker.color = locked ? markerLockedColor : markerAvailableColor;
            marker.enabled = true;
        }
        else if (mission.IsReadyToDeliver)
        {
            marker.text = "?";
            marker.color = markerDeliverColor;
            marker.enabled = true;
        }
        else if (mission.isActive && !mission.isCompleted)
        {
            marker.text = "...";
            marker.color = markerActiveColor;
            marker.enabled = true;
        }
        else
        {
            // Missió acabada: ja no cal indicador
            marker.enabled = false;
        }
    }

    void OnDestroy()
    {
        if (marker != null) Destroy(marker.gameObject);
    }

    // Per a la fletxa de l'objectiu: si l'indicador es veu, la fletxa posa els seus textos just a sobre en lloc de tapar-lo
    public bool IsMarkerVisible => marker != null && marker.enabled && visual != null;
    public Vector3 MarkerTop
    {
        get
        {
            float baseLetter = BaseLetterSize();
            float letter = LetterSize(baseLetter);
            return MarkerRestPosition(baseLetter, letter) + Vector3.up * (letter * MarkerHalfHeight + baseLetter * MarkerFloat);
        }
    }

    // Mida de la lletra de l'indicador de prop, en metres (en un TextMeshPro 3D, una mida de lletra de 10 fa 1 metre)
    private float BaseLetterSize() => Mathf.Clamp(visualHeight * 0.4f, 0.4f, 6f) * markerSize;

    // De lluny, la lletra creix amb la distància perquè a la pantalla no es vegi més petita
    private float LetterSize(float baseLetter)
    {
        Camera cam = Camera.main;
        if (cam == null || markerKeepSizeDistance <= 0f) return baseLetter;
        float distance = Vector3.Distance(cam.transform.position, visual.position);
        return baseLetter * Mathf.Max(1f, distance / markerKeepSizeDistance);
    }

    // El centre de l'indicador quan no flota: just per sobre del cap
    private Vector3 MarkerRestPosition(float baseLetter, float letter) => visual.position + Vector3.up * (headTop + baseLetter * MarkerGap + letter * MarkerHalfHeight);

    private void CreateMarker()
    {
        // És un objecte a part (sense pare) perquè no respiri ni salti amb el model
        GameObject markerObject = new GameObject(name + "_Indicador");
        marker = markerObject.AddComponent<TextMeshPro>();
        marker.alignment = TextAlignmentOptions.Midline; // Centrat amb la forma de la lletra: el «!» queda just al punt calculat
        marker.fontStyle = FontStyles.Bold;
        marker.fontSize = BaseLetterSize() * 10f;
        marker.rectTransform.sizeDelta = new Vector2(visualHeight * 2f, visualHeight);
        marker.textWrappingMode = TextWrappingModes.NoWrap;
        marker.overflowMode = TextOverflowModes.Overflow;
        marker.outlineWidth = 0.2f;
        marker.outlineColor = new Color(0f, 0f, 0f, 0.8f);
        markerObject.AddComponent<Billboard>();
    }

    private Transform FindVisual()
    {
        // El model és el primer fill que té un Renderer i no forma part del cartell (Canvas)
        foreach (Transform child in transform)
        {
            if (child.GetComponentInChildren<Canvas>(true) != null) continue;
            if (child.GetComponentInChildren<MeshRenderer>(true) != null) return child;
        }
        return null;
    }
}
