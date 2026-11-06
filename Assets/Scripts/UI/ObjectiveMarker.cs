using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Fletxa de l'objectiu: indica on és la cosa més propera que toca fer.
//  1. Lliurar una missió complerta (l'NPC que la va donar).
//  2. L'objectiu d'una missió en curs: un cub, una mosca, el cèrcol següent, l'animal atrapat, la meta de la cursa...
//  3. Si no hi ha cap missió en curs, el següent NPC que té una missió per donar.
// Si l'objectiu es veu, hi surt un rombe a sobre amb la distància; si no, una fletxa a la vora de la pantalla que hi apunta.
// Es crea sola en carregar una escena amb missions (no cal posar-la a l'escena)
public class ObjectiveMarker : MonoBehaviour
{
    private const float RefreshInterval = 0.25f; // Cada quant es torna a buscar l'objectiu més proper (segons)
    private const float EdgeMargin = 90f; // Marge respecte a la vora de la pantalla (en una pantalla de 1920x1080)
    private const float HideDistance = 4f; // Quan l'aranya ja hi és, el marcador s'amaga
    private const float NpcHeight = 2.2f; // Sobre els NPCs (i els animals atrapats), el marcador va per sobre del cap

    private static readonly Color MarkerColor = new Color(1f, 0.84f, 0.29f); // El groc del Quest Tracker i dels indicadors dels NPCs
    private static readonly Color LabelColor = new Color(1f, 1f, 1f, 0.9f);
    private static bool hooked = false;
    private static Sprite triangleSprite;
    private static Sprite squareSprite;

    private Canvas canvas;
    private RectTransform canvasRect;
    private CanvasGroup group;
    private RectTransform marker;
    private RectTransform arrow;
    private RectTransform diamond;
    private TextMeshProUGUI distanceText;
    private TextMeshProUGUI labelText;

    private Transform player;
    private bool hasTarget;
    private Vector3 target;
    private string label = "";
    private float nextRefresh;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        CreateIfNeeded();
        if (hooked) return;
        hooked = true;

        // Quan es torna a carregar el nivell (per exemple, en tornar al menú principal), es crea de nou
        SceneManager.sceneLoaded += (scene, mode) => CreateIfNeeded();
    }

    private static void CreateIfNeeded()
    {
        if (FindAnyObjectByType<MissionManager>() == null || FindAnyObjectByType<ObjectiveMarker>() != null) return;
        new GameObject("MarcadorObjectiu").AddComponent<ObjectiveMarker>();
    }

    void Awake()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5; // Per sobre del HUD i per sota dels menús
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasRect = (RectTransform)transform;

        group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        marker = CreateRect("Marcador", transform, Vector2.zero);
        diamond = CreateImage("Rombe", marker, SquareSprite(), new Vector2(22f, 22f));
        diamond.localRotation = Quaternion.Euler(0f, 0f, 45f);
        arrow = CreateImage("Fletxa", marker, TriangleSprite(), new Vector2(40f, 36f));
        distanceText = CreateText("Distancia", marker, 26f, FontStyles.Bold, MarkerColor);
        labelText = CreateText("Missio", marker, 22f, FontStyles.Normal, LabelColor);
    }

    void Start()
    {
        SpiderLocomotion spider = FindAnyObjectByType<SpiderLocomotion>();
        if (spider != null) player = spider.transform;
    }

    void LateUpdate()
    {
        Camera cam = Camera.main;
        // Amb el joc aturat (menú, pausa o victòria) no es mostra
        bool playing = Time.timeScale > 0f && cam != null && player != null;

        if (playing && Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + RefreshInterval;
            FindTarget();
        }

        bool show = playing && hasTarget && Vector3.Distance(player.position, target) > HideDistance;
        group.alpha = Mathf.MoveTowards(group.alpha, show ? 1f : 0f, Time.unscaledDeltaTime * 4f);
        if (hasTarget && cam != null && group.alpha > 0f) Place(cam);
    }

    // --- Quin és l'objectiu ---

    private void FindTarget()
    {
        hasTarget = false;
        label = "";
        if (MissionManager.Instance == null) return;

        float best = float.MaxValue;
        MissionNPC[] npcs = FindObjectsByType<MissionNPC>();
        MissionCollectible[] items = FindObjectsByType<MissionCollectible>();
        RingCircuit[] circuits = FindObjectsByType<RingCircuit>();
        Fly[] flies = FindObjectsByType<Fly>();
        PlatformLinkTrigger[] rescues = FindObjectsByType<PlatformLinkTrigger>();
        PuzzleMover[] movers = FindObjectsByType<PuzzleMover>();
        TimeTrialChallenge[] races = FindObjectsByType<TimeTrialChallenge>();

        // 1. Missions complertes: cal tornar a l'NPC per lliurar-les
        foreach (MissionNPC npc in npcs)
        {
            MissionData mission = npc.missionToGive;
            if (mission != null && mission.IsReadyToDeliver) Consider(npc.transform.position + Vector3.up * NpcHeight, "Lliura: " + mission.title, ref best);
        }

        // 2. Missions en curs: el seu objectiu més proper (o, si no se'n troba cap a l'escena, l'NPC que la va donar)
        foreach (MissionData mission in MissionManager.Instance.allMissions)
        {
            if (mission == null || !mission.isActive || mission.isCompleted) continue;

            bool found = false;
            string id = mission.missionID;
            foreach (MissionCollectible item in items)
                if (item.TargetMissionID == id && !item.IsCollected && item.Circuit == null) found |= Consider(item.transform.position, mission.title, ref best);
            foreach (RingCircuit circuit in circuits)
                if (circuit.Mission == mission && circuit.NextRing != null) found |= Consider(circuit.NextRing.position, mission.title, ref best);
            foreach (Fly fly in flies)
                if (fly.Mission == mission && !fly.IsCaught) found |= Consider(fly.transform.position, mission.title, ref best);
            foreach (PlatformLinkTrigger rescue in rescues)
                if (rescue.MissionID == id && !rescue.IsRescued)
                    found |= Consider(rescue.RescuedAnimal != null ? rescue.RescuedAnimal.position + Vector3.up * NpcHeight : rescue.transform.position, mission.title, ref best);
            foreach (PuzzleMover mover in movers)
                if (mover.MissionOnOpen == mission && !mover.IsOpen) found |= Consider(mover.transform.position, mission.title, ref best);
            foreach (TimeTrialChallenge race in races)
                if (race.MissionID == id) found |= Consider(race.transform.position, mission.title, ref best);

            if (found) continue;
            foreach (MissionNPC npc in npcs)
                if (npc.missionToGive == mission) Consider(npc.transform.position + Vector3.up * NpcHeight, mission.title, ref best);
        }
        if (hasTarget) return;

        // 3. Cap missió en curs: el següent NPC que té una missió per donar
        foreach (MissionNPC npc in npcs)
        {
            MissionData mission = npc.missionToGive;
            if (mission != null && !mission.isActive && !mission.isCompleted && MissionManager.Instance.CanAccept(mission))
                Consider(npc.transform.position + Vector3.up * NpcHeight, "Nova missió", ref best);
        }
    }

    // Es queda el punt si és el més proper fins ara. Retorna cert sempre: vol dir que la missió té un objectiu a l'escena
    private bool Consider(Vector3 point, string text, ref float best)
    {
        float distance = Vector3.Distance(player.position, point);
        if (distance < best)
        {
            best = distance;
            target = point;
            label = text;
            hasTarget = true;
        }
        return true;
    }

    // --- On es dibuixa ---

    private void Place(Camera cam)
    {
        Vector3 screen = cam.WorldToScreenPoint(target);
        Rect pixels = cam.pixelRect;
        float margin = EdgeMargin * canvas.scaleFactor;
        bool behind = screen.z < 0f;
        bool onScreen = !behind && screen.x > pixels.xMin + margin && screen.x < pixels.xMax - margin
                        && screen.y > pixels.yMin + margin && screen.y < pixels.yMax - margin;

        Vector2 point = screen;
        Vector2 direction = Vector2.down;
        if (!onScreen)
        {
            // Fora de la pantalla: la fletxa va a la vora, en la direcció de l'objectiu.
            // Si és darrere de la càmera, va a baix (cal girar-se), inclinada cap al costat pel qual és més ràpid girar
            Vector2 center = pixels.center;
            if (behind)
            {
                Vector3 fromCamera = cam.transform.InverseTransformPoint(target);
                direction = new Vector2(fromCamera.x, -Mathf.Abs(fromCamera.z));
            }
            else
            {
                direction = (Vector2)screen - center;
            }
            if (direction.sqrMagnitude < 0.01f) direction = Vector2.down;
            direction.Normalize();

            float halfWidth = pixels.width / 2f - margin;
            float halfHeight = pixels.height / 2f - margin;
            float reach = Mathf.Min(halfWidth / Mathf.Max(Mathf.Abs(direction.x), 0.0001f), halfHeight / Mathf.Max(Mathf.Abs(direction.y), 0.0001f));
            point = center + direction * reach;
            arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
        }

        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, point, uiCamera, out Vector2 local);
        marker.anchoredPosition = local;

        diamond.gameObject.SetActive(onScreen);
        arrow.gameObject.SetActive(!onScreen);
        diamond.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 4f) * 0.1f);

        // La distància va a sota del rombe, o cap a dins de la pantalla des de la fletxa; el nom de la missió, només si l'objectiu es veu
        distanceText.rectTransform.anchoredPosition = onScreen ? new Vector2(0f, -34f) : -direction * 54f;
        distanceText.text = Mathf.RoundToInt(Vector3.Distance(player.position, target)) + " m";
        labelText.gameObject.SetActive(onScreen && label != "");
        labelText.rectTransform.anchoredPosition = new Vector2(0f, -62f);
        labelText.text = label;
    }

    // --- Peces de la interfície ---

    private static RectTransform CreateRect(string objectName, Transform parent, Vector2 size)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        return rect;
    }

    private static RectTransform CreateImage(string objectName, Transform parent, Sprite sprite, Vector2 size)
    {
        RectTransform rect = CreateRect(objectName, parent, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = MarkerColor;
        image.raycastTarget = false;

        // Una vora fosca perquè es vegi sobre el cel i sobre l'herba
        Outline outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.55f);
        outline.effectDistance = new Vector2(2f, -2f);
        return rect;
    }

    private static TextMeshProUGUI CreateText(string objectName, Transform parent, float size, FontStyles style, Color color)
    {
        RectTransform rect = CreateRect(objectName, parent, new Vector2(420f, 40f));
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        text.outlineWidth = 0.25f;
        text.outlineColor = new Color32(0, 0, 0, 200);
        return text;
    }

    private static Sprite SquareSprite()
    {
        if (squareSprite == null)
        {
            Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
            texture.SetPixels32(pixels);
            texture.Apply();
            squareSprite = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 100f);
        }
        return squareSprite;
    }

    // Triangle que apunta amunt, amb les vores suavitzades (es genera per codi: no cal cap imatge)
    private static Sprite TriangleSprite()
    {
        if (triangleSprite == null)
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            Vector2 a = new Vector2(32f, 60f), b = new Vector2(5f, 8f), c = new Vector2(59f, 8f);

            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float inside = Mathf.Min(EdgeDistance(p, a, b), Mathf.Min(EdgeDistance(p, b, c), EdgeDistance(p, c, a)));
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(inside + 0.5f) * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            triangleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
        return triangleSprite;
    }

    // Distància amb signe d'un punt a la vora a→b: positiva cap a dins del triangle (els vèrtexs van en sentit antihorari)
    private static float EdgeDistance(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 edge = b - a;
        return (edge.x * (p.y - a.y) - edge.y * (p.x - a.x)) / edge.magnitude;
    }
}
