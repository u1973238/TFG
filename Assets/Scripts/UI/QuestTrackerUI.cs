using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Quest Tracker: el panell de les missions (a dalt a la dreta) i els avisos (a dalt al mig).
//  - Cada missió en curs té una fila amb una icona d'estat, el títol i el progrés: una barra per trossos («2/4»),
//    el temps que queda de la cursa o una frase curta amb el que s'ha de fer (MissionData.objective).
//  - Quan s'assoleix l'objectiu, la fila es torna groga amb un «?» (cal tornar a l'NPC); quan es lliura, verda amb un «✓»,
//    i al cap d'uns segons desapareix. A dalt hi ha el recompte de missions lliurades.
//  - Els avisos (missió nova, objectiu complert, recompensa, «encara no pots...») surten a dalt al mig, un darrere l'altre,
//    amb un color segons el tipus (NotificationKind).
// Es crea sol en carregar una escena amb missions, com la fletxa de l'objectiu, i amaga el text antic del HUD
public class QuestTrackerUI : MonoBehaviour
{
    // Mides en una pantalla de 1920x1080 (el llenç s'escala amb la pantalla)
    private const float PanelWidth = 430f;
    private const float ScreenMargin = 28f;  // Distància a les vores de la pantalla
    private const float Padding = 18f;       // Marge interior del panell
    private const float HeaderHeight = 44f;  // «MISSIONS» i el recompte, amb la ratlla de sota
    private const float RowGap = 16f;
    private const float IconSize = 30f;
    private const float TextIndent = 44f;    // Els textos de cada fila comencen a la dreta de la icona
    private const float BarHeight = 7f;
    private const float CountWidth = 66f;    // Espai per al «2/4» o el temps, a la dreta de la barra
    private const float EmptyHeight = 26f;
    private const float DeliveredHold = 4f;  // Segons que es veu una missió lliurada abans de desaparèixer
    private const float RaceWarningTime = 10f; // Per sota d'aquests segons, el temps de la cursa es posa vermell
    private const float ToastIconSize = 38f;
    private const float ToastTextX = 70f;
    private const float ToastMaxWidth = 860f;
    private const float AppearTime = 0.35f;
    private const float ToastFadeIn = 0.25f;
    private const float ToastFadeOut = 0.3f;

    private static readonly Color PanelColor = new Color(0.07f, 0.09f, 0.11f, 0.88f); // El color de les targetes dels menús, una mica transparent
    private static readonly Color ToastColor = new Color(0.07f, 0.09f, 0.11f, 0.93f);
    private static readonly Color TextColor = new Color(0.96f, 0.96f, 0.96f, 1f);
    private static readonly Color SubtleColor = new Color(0.72f, 0.76f, 0.8f, 1f);
    private static readonly Color LineColor = new Color(1f, 1f, 1f, 0.12f);
    private static readonly Color TrackColor = new Color(1f, 1f, 1f, 0.16f);
    private static readonly Color Gold = new Color(1f, 0.84f, 0.29f, 1f); // El groc dels indicadors dels NPCs i de la fletxa de l'objectiu
    private static readonly Color Green = new Color(0.42f, 0.86f, 0.46f, 1f);
    private static readonly Color Red = new Color(0.96f, 0.36f, 0.3f, 1f);
    private static readonly Color RedText = new Color(1f, 0.6f, 0.55f, 1f); // Més clar, perquè es llegeixi bé sobre el panell fosc
    private static readonly Color Blue = new Color(0.55f, 0.75f, 1f, 1f);
    private static readonly Color DarkGlyph = new Color(0.08f, 0.09f, 0.11f, 1f);

    private enum RowState { Active, Ready, Delivered }

    // Una fila del panell: una missió
    private class Row
    {
        public MissionData Mission;
        public TimeTrialChallenge Race; // Només a la missió de la cursa
        public RectTransform Rect;
        public CanvasGroup Group;
        public Image Background;
        public RectTransform Icon;
        public Image IconImage;
        public Image IconCheck;
        public TextMeshProUGUI IconText;
        public TextMeshProUGUI Title;
        public TextMeshProUGUI Detail;
        public RectTransform Bar;
        public readonly List<Image> Segments = new List<Image>(); // L'emplenat de cada tros de la barra
        public TextMeshProUGUI Count;

        public RowState State;
        public int ShownAmount;
        public bool RaceRunning, RaceFailed;
        public int ShownTenths = -1;
        public string LayoutKey = "";
        public float Height;
        public float Y;
        public bool Placed;
        public float Appear;          // 0 → 1 en entrar
        public float Vanish;          // 0 → 1 en sortir
        public float Hold;            // Temps que falta perquè surti una missió lliurada
        public float Flash;           // Llampada de color quan canvia alguna cosa
        public Color FlashColor;
        public float IconPop = 1f;    // 0 → 1: la icona fa un bot quan canvia
        public float CountPop = 1f;   // 0 → 1: el comptador fa un bot quan avança
    }

    private struct Notice
    {
        public string Text;
        public NotificationKind Kind;
    }

    private static bool hooked;
    private static Sprite roundedSprite, circleSprite, ringSprite, checkSprite;

    private MissionManager manager;
    private Canvas hudCanvas; // El llenç del HUD antic: el menú d'inici l'amaga, i llavors el Quest Tracker també s'amaga
    private CanvasGroup group;
    private RectTransform panel;
    private RectTransform content;
    private TextMeshProUGUI headerCount;
    private CanvasGroup emptyGroup;
    private float panelHeight = -1f;
    private bool dirty = true;
    private readonly List<Row> rows = new List<Row>();
    private readonly Dictionary<string, TimeTrialChallenge> races = new Dictionary<string, TimeTrialChallenge>();

    private RectTransform toast;
    private CanvasGroup toastGroup;
    private Image toastIcon;
    private Image toastCheck;
    private TextMeshProUGUI toastGlyph;
    private TextMeshProUGUI toastCaption;
    private TextMeshProUGUI toastMessage;
    private readonly List<Notice> queue = new List<Notice>();
    private bool hasNotice;
    private Notice current;
    private float toastAge;
    private float toastDuration;

    // Per a les proves: quantes files i avisos hi ha
    public int RowCount => rows.Count;
    public bool ToastVisible => hasNotice;
    public string ToastText => hasNotice ? current.Text : "";
    public float Alpha => group != null ? group.alpha : 0f;

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
        if (FindAnyObjectByType<MissionManager>() == null || FindAnyObjectByType<QuestTrackerUI>() != null) return;
        new GameObject("QuestTracker").AddComponent<QuestTrackerUI>();
    }

    void Awake()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 4; // Per sobre del HUD i per sota de la fletxa de l'objectiu i dels menús
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        BuildPanel();
        BuildToast();

        manager = MissionManager.Instance != null ? MissionManager.Instance : FindAnyObjectByType<MissionManager>();
        if (manager == null) return;
        manager.Changed += MarkDirty;
        manager.NotificationShown += OnNotification;

        // El text antic del HUD ja no cal: aquest panell el substitueix
        TextMeshProUGUI legacy = manager.LegacyTrackerText;
        if (legacy != null)
        {
            Canvas legacyCanvas = legacy.GetComponentInParent<Canvas>(true);
            hudCanvas = legacyCanvas != null ? legacyCanvas.rootCanvas : null;
            legacy.gameObject.SetActive(false);
        }
    }

    void Start()
    {
        foreach (TimeTrialChallenge race in FindObjectsByType<TimeTrialChallenge>())
        {
            if (!string.IsNullOrEmpty(race.MissionID)) races[race.MissionID] = race;
        }
    }

    void OnDestroy()
    {
        if (manager == null) return;
        manager.Changed -= MarkDirty;
        manager.NotificationShown -= OnNotification;
    }

    private void MarkDirty()
    {
        dirty = true;
    }

    void LateUpdate()
    {
        if (manager == null) return;

        // Amb el joc aturat (menú d'inici, pausa o victòria) no es mostra
        bool show = Time.timeScale > 0f && (hudCanvas == null || (hudCanvas.enabled && hudCanvas.gameObject.activeInHierarchy));
        group.alpha = Mathf.MoveTowards(group.alpha, show ? 1f : 0f, Time.unscaledDeltaTime * 5f);

        float deltaTime = Time.deltaTime; // Amb el joc aturat, les animacions també s'aturen
        if (dirty)
        {
            dirty = false;
            Sync();
        }
        UpdateRaces();
        AnimateRows(deltaTime);
        UpdateToast(deltaTime);
    }

    // --- Panell de missions ---

    private void BuildPanel()
    {
        panel = NewRect("Panell", transform);
        panel.anchorMin = panel.anchorMax = new Vector2(1f, 1f);
        panel.pivot = new Vector2(1f, 1f);
        panel.anchoredPosition = new Vector2(-ScreenMargin, -ScreenMargin);
        panel.sizeDelta = new Vector2(PanelWidth, 100f);
        Image background = panel.gameObject.AddComponent<Image>();
        SetRounded(background, PanelColor, 14f);

        TextMeshProUGUI header = NewText("Titol", panel, 15f, FontStyles.Bold, SubtleColor);
        header.text = "MISSIONS";
        header.characterSpacing = 8f;
        Place(header.rectTransform, Padding, -Padding, 200f, 22f);

        headerCount = NewText("Recompte", panel, 18f, FontStyles.Normal, SubtleColor, TextAlignmentOptions.TopRight);
        Place(headerCount.rectTransform, PanelWidth - Padding - 160f, -Padding + 2f, 160f, 24f);

        Image line = NewImage("Ratlla", panel, SquareSprite(), LineColor);
        Place(line.rectTransform, Padding, -(Padding + 30f), PanelWidth - 2f * Padding, 1f);

        content = NewRect("Missions", panel);
        Place(content, Padding, -(Padding + HeaderHeight), PanelWidth - 2f * Padding, 10f);

        TextMeshProUGUI empty = NewText("CapMissio", panel, 18f, FontStyles.Italic, SubtleColor);
        empty.text = "Explora l'illa i ajuda els animals.";
        Place(empty.rectTransform, Padding, -(Padding + HeaderHeight), PanelWidth - 2f * Padding, EmptyHeight);
        emptyGroup = empty.gameObject.AddComponent<CanvasGroup>();
        emptyGroup.alpha = 0f;
    }

    // Posa les files d'acord amb l'estat de les missions
    private void Sync()
    {
        int delivered = 0;
        foreach (MissionData mission in manager.allMissions)
        {
            if (mission == null) continue;
            if (mission.isDelivered) delivered++;

            Row row = rows.Find(r => r.Mission == mission);
            if (row == null)
            {
                if (!mission.isActive || mission.isDelivered) continue;
                row = CreateRow(mission);
                rows.Add(row);
            }
            Refresh(row);
        }

        // Les files segueixen l'ordre de la llista de missions (no salten de lloc)
        rows.Sort((a, b) => manager.allMissions.IndexOf(a.Mission).CompareTo(manager.allMissions.IndexOf(b.Mission)));
        headerCount.text = $"<color=#{Hex(Gold)}><b>{delivered}</b></color> / {manager.allMissions.Count} lliurades";
    }

    private Row CreateRow(MissionData mission)
    {
        Row row = new Row { Mission = mission };
        races.TryGetValue(mission.missionID, out row.Race);

        row.Rect = NewRect("Missio_" + mission.missionID, content);
        row.Group = row.Rect.gameObject.AddComponent<CanvasGroup>();
        row.Group.alpha = 0f;

        row.Background = NewImage("Fons", row.Rect, null, Color.clear);
        SetRounded(row.Background, Color.clear, 10f);

        row.Icon = NewRect("Icona", row.Rect);
        Place(row.Icon, 0f, 0f, IconSize, IconSize);
        row.Icon.pivot = new Vector2(0.5f, 0.5f);
        row.Icon.anchoredPosition = new Vector2(IconSize / 2f, -IconSize / 2f);
        row.IconImage = row.Icon.gameObject.AddComponent<Image>();
        row.IconImage.raycastTarget = false;
        row.IconCheck = NewImage("Marca", row.Icon, CheckSprite(), Color.white);
        Place(row.IconCheck.rectTransform, 0f, 0f, IconSize, IconSize);
        row.IconText = NewText("Signe", row.Icon, 21f, FontStyles.Bold, DarkGlyph, TextAlignmentOptions.Center);
        row.IconText.textWrappingMode = TextWrappingModes.NoWrap;
        Place(row.IconText.rectTransform, 0f, 0f, IconSize, IconSize);

        row.Title = NewText("Titol", row.Rect, 22f, FontStyles.Bold, TextColor);
        row.Detail = NewText("Detall", row.Rect, 17f, FontStyles.Normal, SubtleColor);

        row.Bar = NewRect("Barra", row.Rect);
        row.Count = NewText("Comptador", row.Rect, 18f, FontStyles.Bold, Gold, TextAlignmentOptions.Right);
        row.Count.textWrappingMode = TextWrappingModes.NoWrap;
        row.Count.rectTransform.pivot = new Vector2(1f, 0.5f);

        // Comença en l'estat en què ja és (sense llampades)
        row.State = mission.isDelivered ? RowState.Delivered : mission.isCompleted ? RowState.Ready : RowState.Active;
        row.ShownAmount = mission.currentAmount;
        if (row.Race != null)
        {
            row.RaceRunning = row.Race.IsRunning;
            row.RaceFailed = row.Race.Failed;
        }
        if (row.State == RowState.Delivered) row.Hold = DeliveredHold;
        return row;
    }

    private void Refresh(Row row)
    {
        MissionData mission = row.Mission;
        RowState state = mission.isDelivered ? RowState.Delivered : mission.isCompleted ? RowState.Ready : RowState.Active;
        if (state != row.State)
        {
            row.State = state;
            row.IconPop = 0f;
            row.Flash = 1f;
            row.FlashColor = state == RowState.Delivered ? Green : Gold;
            if (state == RowState.Delivered) row.Hold = DeliveredHold;
        }

        if (mission.currentAmount != row.ShownAmount)
        {
            if (mission.currentAmount > row.ShownAmount)
            {
                row.CountPop = 0f;
                if (state == RowState.Active)
                {
                    row.Flash = Mathf.Max(row.Flash, 0.6f);
                    row.FlashColor = Gold;
                }
            }
            else
            {
                // Ha tornat a zero (per exemple, en tocar terra al circuit de cèrcols)
                row.Flash = 1f;
                row.FlashColor = Red;
                row.CountPop = 0f;
            }
            row.ShownAmount = mission.currentAmount;
        }

        ApplyLook(row);
    }

    // Icona, colors i textos de la fila segons l'estat
    private void ApplyLook(Row row)
    {
        MissionData mission = row.Mission;
        bool counted = row.State == RowState.Active && mission.requiredAmount > 1;
        bool racing = row.State == RowState.Active && row.RaceRunning;
        string detail = "";
        Color detailColor = SubtleColor;

        switch (row.State)
        {
            case RowState.Ready:
                detail = "Torna a parlar amb qui te l'ha demanat";
                detailColor = Gold;
                break;
            case RowState.Delivered:
                detail = "Lliurada!";
                detailColor = Green;
                break;
            default:
                if (row.RaceFailed && !racing)
                {
                    detail = "S'ha acabat el temps: torna-hi a parlar";
                    detailColor = RedText;
                }
                else if (!counted && !racing && !string.IsNullOrEmpty(mission.objective))
                {
                    detail = mission.objective;
                }
                break;
        }

        // La icona: un cercle buit (en curs), groc amb «?» (cal lliurar-la) o verd amb «✓» (lliurada)
        switch (row.State)
        {
            case RowState.Ready:
                row.IconImage.sprite = CircleSprite();
                row.IconImage.color = Gold;
                row.IconText.text = "?";
                break;
            case RowState.Delivered:
                row.IconImage.sprite = CircleSprite();
                row.IconImage.color = Green;
                row.IconText.text = "";
                break;
            default:
                row.IconImage.sprite = RingSprite();
                row.IconImage.color = row.RaceFailed && !racing ? Red : new Color(1f, 1f, 1f, 0.85f);
                row.IconText.text = "";
                break;
        }
        row.IconCheck.enabled = row.State == RowState.Delivered;
        row.Title.color = row.State == RowState.Delivered ? SubtleColor : TextColor;
        row.Detail.color = detailColor;

        bool showBar = counted || racing;
        if (counted) row.Count.text = $"{mission.currentAmount}/{mission.requiredAmount}";
        else if (!racing) row.Count.text = "";
        if (racing) row.ShownTenths = -1; // Es tornarà a escriure el temps

        string layoutKey = $"{mission.title}|{detail}|{showBar}|{(racing ? -1 : mission.requiredAmount)}";
        if (layoutKey != row.LayoutKey)
        {
            row.LayoutKey = layoutKey;
            LayoutRow(row, detail, showBar, racing ? 1 : Mathf.Clamp(mission.requiredAmount, 1, 12));
        }
    }

    private void LayoutRow(Row row, string detail, bool showBar, int segmentCount)
    {
        float width = PanelWidth - 2f * Padding;
        float textWidth = width - TextIndent;
        float y = 0f;

        row.Title.text = row.Mission.title;
        float titleHeight = row.Title.GetPreferredValues(row.Mission.title, textWidth, 0f).y;
        Place(row.Title.rectTransform, TextIndent, y + 2f, textWidth, titleHeight);
        y -= titleHeight;

        row.Detail.gameObject.SetActive(detail != "");
        if (detail != "")
        {
            row.Detail.text = detail;
            float detailHeight = row.Detail.GetPreferredValues(detail, textWidth, 0f).y;
            y -= 3f;
            Place(row.Detail.rectTransform, TextIndent, y, textWidth, detailHeight);
            y -= detailHeight;
        }

        row.Bar.gameObject.SetActive(showBar);
        row.Count.gameObject.SetActive(showBar);
        if (showBar)
        {
            float lineHeight = 22f;
            float barWidth = textWidth - CountWidth - 10f;
            y -= 8f;
            Place(row.Bar, TextIndent, y - (lineHeight - BarHeight) / 2f, barWidth, BarHeight);
            row.Count.rectTransform.anchoredPosition = new Vector2(TextIndent + textWidth, y - lineHeight / 2f);
            row.Count.rectTransform.sizeDelta = new Vector2(CountWidth, lineHeight);
            BuildSegments(row, segmentCount, barWidth);
            y -= lineHeight;
        }

        row.Height = Mathf.Max(IconSize, -y);
        Place(row.Background.rectTransform, -10f, 8f, width + 20f, row.Height + 16f);
        row.Rect.sizeDelta = new Vector2(width, row.Height);
    }

    // Barra feta de trossos (un per objecte que demana la missió); la de la cursa és d'un sol tros
    private static void BuildSegments(Row row, int count, float barWidth)
    {
        foreach (Image segment in row.Segments) Destroy(segment.transform.parent.gameObject);
        row.Segments.Clear();

        const float gap = 4f;
        float segmentWidth = (barWidth - gap * (count - 1)) / count;
        for (int i = 0; i < count; i++)
        {
            Image track = NewImage("Tros", row.Bar, null, TrackColor);
            SetRounded(track, TrackColor, 3f);
            Place(track.rectTransform, i * (segmentWidth + gap), 0f, segmentWidth, BarHeight);

            Image fill = NewImage("Ple", track.transform, null, Gold);
            SetRounded(fill, Gold, 3f);
            Place(fill.rectTransform, 0f, 0f, segmentWidth, BarHeight);
            fill.rectTransform.localScale = new Vector3(i < row.Mission.currentAmount ? 1f : 0f, 1f, 1f);
            row.Segments.Add(fill);
        }
    }

    // El temps de la cursa: la barra es buida i el comptador compta enrere
    private void UpdateRaces()
    {
        foreach (Row row in rows)
        {
            if (row.Race == null) continue;
            bool running = row.Race.IsRunning;
            bool failed = row.Race.Failed;
            if (running != row.RaceRunning || failed != row.RaceFailed)
            {
                row.RaceRunning = running;
                row.RaceFailed = failed;
                ApplyLook(row);
            }
            if (!running || row.State != RowState.Active || row.Segments.Count == 0) continue;

            float left = Mathf.Max(0f, row.Race.TimeLeft);
            int tenths = Mathf.CeilToInt(left * 10f);
            if (tenths != row.ShownTenths)
            {
                row.ShownTenths = tenths;
                row.Count.text = FormatTime(tenths);
            }

            Color color = left <= RaceWarningTime ? Red : Gold;
            row.Count.color = color;
            Image fill = row.Segments[0];
            fill.color = color;
            float ratio = row.Race.TimeLimit > 0f ? left / row.Race.TimeLimit : 0f;
            fill.rectTransform.localScale = new Vector3(Mathf.Clamp01(ratio), 1f, 1f);
        }
    }

    private void AnimateRows(float deltaTime)
    {
        float smooth = 1f - Mathf.Exp(-14f * deltaTime);
        float y = 0f;

        foreach (Row row in rows)
        {
            // Entrar i, si ja s'ha lliurat (o ja no és activa), sortir al cap d'una estona
            row.Appear = Mathf.MoveTowards(row.Appear, 1f, deltaTime / AppearTime);
            bool leaving = row.State == RowState.Delivered || (!row.Mission.isActive && !row.Mission.isDelivered);
            if (leaving)
            {
                row.Hold -= deltaTime;
                if (row.Hold <= 0f) row.Vanish = Mathf.MoveTowards(row.Vanish, 1f, deltaTime / AppearTime);
            }

            float appear = EaseOut(row.Appear);
            float keep = 1f - EaseOut(row.Vanish);
            if (!row.Placed)
            {
                row.Y = y;
                row.Placed = true;
            }
            row.Y = Mathf.Lerp(row.Y, y, smooth);
            row.Rect.anchoredPosition = new Vector2((1f - appear) * 48f + (1f - keep) * 32f, row.Y);
            row.Group.alpha = appear * keep;
            y -= (row.Height + RowGap) * keep;

            // Llampada de color i bots
            row.Flash = Mathf.MoveTowards(row.Flash, 0f, deltaTime / 0.9f);
            float baseAlpha = row.State == RowState.Ready ? 0.1f : 0f;
            Color tint = row.Flash > 0f ? row.FlashColor : Gold;
            row.Background.color = new Color(tint.r, tint.g, tint.b, Mathf.Max(baseAlpha, 0.28f * row.Flash));
            row.IconPop = Mathf.MoveTowards(row.IconPop, 1f, deltaTime / 0.4f);
            row.Icon.localScale = Vector3.one * Pop(row.IconPop);
            row.CountPop = Mathf.MoveTowards(row.CountPop, 1f, deltaTime / 0.35f);
            row.Count.rectTransform.localScale = Vector3.one * Pop(row.CountPop);

            // Els trossos de la barra s'omplen (o es buiden) a poc a poc
            if (!row.RaceRunning)
            {
                for (int i = 0; i < row.Segments.Count; i++)
                {
                    RectTransform fill = row.Segments[i].rectTransform;
                    float target = i < row.Mission.currentAmount ? 1f : 0f;
                    fill.localScale = new Vector3(Mathf.MoveTowards(fill.localScale.x, target, deltaTime * 5f), 1f, 1f);
                }
            }
        }

        rows.RemoveAll(row =>
        {
            if (row.Vanish < 1f) return false;
            Destroy(row.Rect.gameObject);
            return true;
        });

        bool empty = rows.Count == 0;
        emptyGroup.alpha = Mathf.MoveTowards(emptyGroup.alpha, empty ? 1f : 0f, deltaTime * 4f);
        float contentHeight = empty ? EmptyHeight : Mathf.Max(EmptyHeight, -y - RowGap);
        float targetHeight = Padding + HeaderHeight + contentHeight + Padding;
        panelHeight = panelHeight < 0f ? targetHeight : Mathf.Lerp(panelHeight, targetHeight, smooth);
        panel.sizeDelta = new Vector2(PanelWidth, panelHeight);
    }

    // --- Avisos ---

    private void BuildToast()
    {
        toast = NewRect("Avis", transform);
        toast.anchorMin = toast.anchorMax = new Vector2(0.5f, 1f);
        toast.pivot = new Vector2(0.5f, 1f);
        toast.anchoredPosition = new Vector2(0f, -ScreenMargin);
        toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
        toastGroup.alpha = 0f;
        Image background = toast.gameObject.AddComponent<Image>();
        SetRounded(background, ToastColor, 16f);

        toastIcon = NewImage("Icona", toast, CircleSprite(), Gold);
        toastIcon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        toastCheck = NewImage("Marca", toastIcon.transform, CheckSprite(), Color.white);
        Place(toastCheck.rectTransform, 0f, 0f, ToastIconSize, ToastIconSize);
        toastGlyph = NewText("Signe", toastIcon.transform, 26f, FontStyles.Bold, DarkGlyph, TextAlignmentOptions.Center);
        toastGlyph.textWrappingMode = TextWrappingModes.NoWrap;
        Place(toastGlyph.rectTransform, 0f, 0f, ToastIconSize, ToastIconSize);

        toastCaption = NewText("Tipus", toast, 15f, FontStyles.Bold, Gold);
        toastCaption.characterSpacing = 6f;
        toastCaption.textWrappingMode = TextWrappingModes.NoWrap;
        toastMessage = NewText("Text", toast, 23f, FontStyles.Normal, TextColor);
    }

    private void OnNotification(string text, NotificationKind kind)
    {
        if (string.IsNullOrEmpty(text)) return;

        // Si ja es veu el mateix avís, s'allarga; si ja és a la cua, no es repeteix
        if (hasNotice && current.Text == text)
        {
            toastAge = Mathf.Min(toastAge, ToastFadeIn);
            return;
        }
        if (queue.Exists(notice => notice.Text == text)) return;

        queue.Add(new Notice { Text = text, Kind = kind });
        if (hasNotice) toastDuration = Mathf.Min(toastDuration, Mathf.Max(toastAge + 0.5f, 1.4f)); // N'hi ha més esperant: aquest s'escurça
    }

    private void UpdateToast(float deltaTime)
    {
        if (!hasNotice)
        {
            if (queue.Count == 0)
            {
                toastGroup.alpha = 0f;
                return;
            }
            current = queue[0];
            queue.RemoveAt(0);
            hasNotice = true;
            toastAge = 0f;
            toastDuration = queue.Count > 0 ? 1.8f : Mathf.Max(1.5f, manager.NotificationDuration);
            LayoutToast(current);
        }

        toastAge += deltaTime;
        float alpha = toastAge < ToastFadeIn ? toastAge / ToastFadeIn
                    : toastAge > toastDuration ? 1f - (toastAge - toastDuration) / ToastFadeOut
                    : 1f;
        toastGroup.alpha = Mathf.Clamp01(alpha);

        // Entra baixant una mica i la icona fa un bot
        float slide = toastAge < ToastFadeIn ? (1f - EaseOut(toastAge / ToastFadeIn)) * 16f : 0f;
        toast.anchoredPosition = new Vector2(0f, -ScreenMargin + slide);
        toastIcon.rectTransform.localScale = Vector3.one * Pop(Mathf.Clamp01(toastAge / 0.45f));

        if (toastAge >= toastDuration + ToastFadeOut) hasNotice = false;
    }

    private void LayoutToast(Notice notice)
    {
        Color color = KindColor(notice.Kind);
        string caption = MissionManager.NotificationCaption(notice.Kind).ToUpperInvariant();

        // La icona: «!» (missió nova o avís), «?» (cal tornar a l'NPC), «✓» (lliurada o fet) o «i» (informació)
        toastIcon.color = color;
        bool check = notice.Kind == NotificationKind.Delivered || notice.Kind == NotificationKind.Success;
        toastCheck.enabled = check;
        toastGlyph.text = check ? "" : notice.Kind == NotificationKind.ObjectiveDone ? "?" : notice.Kind == NotificationKind.Info ? "i" : "!";
        toastGlyph.color = notice.Kind == NotificationKind.Warning ? Color.white : DarkGlyph;

        // La primera línia, en negreta; la resta, més petita
        string message = notice.Text;
        int newLine = message.IndexOf('\n');
        message = newLine < 0
            ? $"<b>{message}</b>"
            : $"<b>{message.Substring(0, newLine)}</b>\n<size=82%><color=#{Hex(SubtleColor)}>{message.Substring(newLine + 1)}</color></size>";
        toastMessage.text = message;

        float maxTextWidth = ToastMaxWidth - ToastTextX - 26f;
        float captionWidth = caption != "" ? toastCaption.GetPreferredValues(caption, 0f, 0f).x : 0f;
        float textWidth = Mathf.Min(maxTextWidth, Mathf.Max(toastMessage.GetPreferredValues(message, maxTextWidth, 0f).x, captionWidth)) + 2f;
        float messageHeight = toastMessage.GetPreferredValues(message, textWidth, 0f).y;

        float y = -15f;
        toastCaption.gameObject.SetActive(caption != "");
        if (caption != "")
        {
            toastCaption.text = caption;
            toastCaption.color = color;
            Place(toastCaption.rectTransform, ToastTextX, y, textWidth, 20f);
            y -= 22f;
        }
        Place(toastMessage.rectTransform, ToastTextX, y, textWidth, messageHeight);
        y -= messageHeight;

        float height = Mathf.Max(68f, -y + 15f);
        toast.sizeDelta = new Vector2(ToastTextX + textWidth + 26f, height);
        toastIcon.rectTransform.anchoredPosition = new Vector2(18f + ToastIconSize / 2f, -height / 2f);
        toastIcon.rectTransform.sizeDelta = new Vector2(ToastIconSize, ToastIconSize);
    }

    private static Color KindColor(NotificationKind kind)
    {
        switch (kind)
        {
            case NotificationKind.Warning: return Red;
            case NotificationKind.Success:
            case NotificationKind.Delivered: return Green;
            case NotificationKind.Info: return Blue;
            default: return Gold;
        }
    }

    // --- Ajudes ---

    private static float EaseOut(float t)
    {
        float u = 1f - Mathf.Clamp01(t);
        return 1f - u * u * u;
    }

    // Un bot que creix i torna a la mida normal (t va de 0 a 1)
    private static float Pop(float t)
    {
        return 1f + 0.4f * Mathf.Sin(Mathf.PI * t) * (1f - t);
    }

    // El temps amb una xifra decimal i coma (23,4 s); a partir d'un minut, en minuts i segons (4:05)
    private static string FormatTime(int tenths)
    {
        if (tenths >= 600)
        {
            int seconds = Mathf.CeilToInt(tenths / 10f);
            return $"{seconds / 60}:{seconds % 60:00}";
        }
        return (tenths / 10f).ToString("0.0", CultureInfo.InvariantCulture).Replace('.', ',') + " s";
    }

    private static string Hex(Color color)
    {
        return ColorUtility.ToHtmlStringRGB(color);
    }

    private static RectTransform NewRect(string objectName, Transform parent)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform));
        go.layer = 5; // UI
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        return rect;
    }

    // Col·loca un element des de la cantonada de dalt a l'esquerra del seu pare
    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static Image NewImage(string objectName, Transform parent, Sprite sprite, Color color)
    {
        RectTransform rect = NewRect(objectName, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    // Rectangle amb les cantonades arrodonides (la imatge es divideix en 9 trossos i les cantonades no es deformen)
    private static void SetRounded(Image image, Color color, float radius)
    {
        image.sprite = RoundedSprite();
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 20f / radius; // La textura té cantonades de 20 píxels
        image.color = color;
        image.raycastTarget = false;
    }

    private static TextMeshProUGUI NewText(string objectName, Transform parent, float size, FontStyles style, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft)
    {
        RectTransform rect = NewRect(objectName, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        text.richText = true;
        return text;
    }

    // --- Formes generades per codi (no cal cap imatge): rectangle arrodonit, cercle, anella i «✓» ---

    private static Sprite RoundedSprite()
    {
        if (roundedSprite == null)
        {
            roundedSprite = MakeSprite(64, 22f, (x, y) =>
            {
                // Distància amb signe a un rectangle de 64x64 amb cantonades de radi 20
                Vector2 q = new Vector2(Mathf.Abs(x - 32f) - 12f, Mathf.Abs(y - 32f) - 12f);
                return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - 20f;
            });
        }
        return roundedSprite;
    }

    private static Sprite CircleSprite()
    {
        if (circleSprite == null) circleSprite = MakeSprite(64, 0f, (x, y) => Vector2.Distance(new Vector2(x, y), new Vector2(32f, 32f)) - 30.5f);
        return circleSprite;
    }

    private static Sprite RingSprite()
    {
        if (ringSprite == null) ringSprite = MakeSprite(64, 0f, (x, y) => Mathf.Abs(Vector2.Distance(new Vector2(x, y), new Vector2(32f, 32f)) - 27f) - 3.5f);
        return ringSprite;
    }

    private static Sprite CheckSprite()
    {
        if (checkSprite == null)
        {
            Vector2 a = new Vector2(18f, 33f), b = new Vector2(28f, 22f), c = new Vector2(47f, 43f);
            checkSprite = MakeSprite(64, 0f, (x, y) =>
            {
                Vector2 p = new Vector2(x, y);
                return Mathf.Min(SegmentDistance(p, a, b), SegmentDistance(p, b, c)) - 4.5f;
            });
        }
        return checkSprite;
    }

    private static Sprite squareSprite;

    private static Sprite SquareSprite()
    {
        if (squareSprite == null) squareSprite = MakeSprite(4, 0f, (x, y) => -1f);
        return squareSprite;
    }

    private static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + ab * t);
    }

    // Cada píxel és opac a dins de la forma (distància negativa) i es fon a la vora, en un píxel (vores suaus)
    private static Sprite MakeSprite(int size, float border, System.Func<float, float, float> distance)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = distance(x + 0.5f, y + 0.5f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(0.5f - d) * 255f));
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
    }
}
