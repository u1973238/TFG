using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Eina de l'editor que construeix tots els menús del joc amb el mateix estil:
// menú principal, pausa, controls, opcions i pantalla de victòria, ja connectats als scripts.
// Es fa servir des del menú de Unity: Tools > TFG > Construir menús (es pot tornar a executar per refer-los)
public static class MenuBuilder
{
    private const string GameTitle = "L'ARANYA"; // Canvia-ho pel nom del joc
    private const string GameSubtitle = "Un joc de plataformes amb teranyines";
    private const string RootName = "UI_Menus";

    // Paleta: el vermell és el mateix de l'aranya i de la marieta
    private static readonly Color DimColor = new Color(0f, 0f, 0f, 0.55f);
    private static readonly Color PanelColor = new Color(0.07f, 0.09f, 0.11f, 0.94f);
    private static readonly Color AccentColor = new Color32(0xF4, 0x43, 0x36, 0xFF);
    private static readonly Color ButtonColor = new Color(0.17f, 0.2f, 0.23f, 1f);
    private static readonly Color TrackColor = new Color(0.25f, 0.28f, 0.32f, 1f);
    private static readonly Color TextColor = new Color(0.96f, 0.96f, 0.96f, 1f);
    private static readonly Color SubtleTextColor = new Color(0.72f, 0.76f, 0.8f, 1f);

    private static Sprite roundedSprite;
    private static Sprite trackSprite;
    private static Sprite knobSprite;

    [MenuItem("Tools/TFG/Construir menús")]
    public static void Build()
    {
        PauseMenu pauseMenu = Object.FindAnyObjectByType<PauseMenu>(FindObjectsInactive.Include);
        MenuVictoria victoria = Object.FindAnyObjectByType<MenuVictoria>(FindObjectsInactive.Include);
        if (pauseMenu == null || victoria == null)
        {
            EditorUtility.DisplayDialog("Construir menús", "No he trobat PauseMenu o MenuVictoria a l'escena oberta. Obre Joc.unity i torna-ho a provar.", "D'acord");
            return;
        }

        bool accept = EditorUtility.DisplayDialog("Construir menús",
            "Es crearà l'objecte «" + RootName + "» amb el menú principal, la pausa, els controls, les opcions i la victòria.\n\n" +
            "Els panells antics de pausa i victòria es desactivaran (no s'esborren). Si ja existeix «" + RootName + "», es refarà.\n\n" +
            "Ho pots desfer amb Ctrl+Z.", "Construir", "Cancel·lar");
        if (!accept) return;

        roundedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        trackSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        Undo.SetCurrentGroupName("Construir menús");
        int undoGroup = Undo.GetCurrentGroup();

        // Panells antics (els que tenia el joc abans): els desactivem perquè no es vegin
        GameObject oldPausePanel = GetObject(pauseMenu, "pausePanel") as GameObject;
        GameObject oldVictoryPanel = GetObject(victoria, "panellVictoria") as GameObject;

        // Si ja hi havia menús construïts amb aquesta eina, els refem de zero
        foreach (GameObject sceneRoot in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (sceneRoot.name != RootName) continue;
            if (oldPausePanel != null && oldPausePanel.transform.IsChildOf(sceneRoot.transform)) oldPausePanel = null;
            if (oldVictoryPanel != null && oldVictoryPanel.transform.IsChildOf(sceneRoot.transform)) oldVictoryPanel = null;
            Undo.DestroyObjectImmediate(sceneRoot);
        }

        DeactivateOld(oldPausePanel);
        DeactivateOld(oldVictoryPanel);

        // Només hi pot haver un MainMenu
        foreach (MainMenu oldMainMenu in Object.FindObjectsByType<MainMenu>(FindObjectsInactive.Include))
        {
            Undo.DestroyObjectImmediate(oldMainMenu);
        }

        EnsureEventSystem();

        // --- Llenç ---
        GameObject root = new GameObject(RootName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(root, "Construir menús");
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Per sobre del HUD
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // --- Menú principal ---
        GameObject mainScreen = CreateScreen(root.transform, "MenuPrincipal", 620, out Transform mainCard);
        CreateText(mainCard, "Titol", GameTitle, 84, AccentColor, FontStyles.Bold, 100);
        CreateText(mainCard, "Subtitol", GameSubtitle, 28, SubtleTextColor, FontStyles.Italic, 40);
        CreateSpacer(mainCard, 20);
        Button playButton = CreateButton(mainCard, "Jugar");
        Button mainControlsButton = CreateButton(mainCard, "Controls");
        Button mainOptionsButton = CreateButton(mainCard, "Opcions");
        Button quitButton = CreateButton(mainCard, "Sortir");

        // --- Pausa ---
        GameObject pauseScreen = CreateScreen(root.transform, "MenuPausa", 620, out Transform pauseCard);
        CreateText(pauseCard, "Titol", "PAUSA", 72, AccentColor, FontStyles.Bold, 90);
        CreateSpacer(pauseCard, 10);
        Button resumeButton = CreateButton(pauseCard, "Continuar");
        Button pauseControlsButton = CreateButton(pauseCard, "Controls");
        Button pauseOptionsButton = CreateButton(pauseCard, "Opcions");
        Button pauseMainMenuButton = CreateButton(pauseCard, "Menú principal");

        // --- Controls ---
        GameObject controlsScreen = CreateScreen(root.transform, "PantallaControls", 1150, out Transform controlsCard);
        CreateText(controlsCard, "Titol", "CONTROLS", 64, AccentColor, FontStyles.Bold, 80);
        TextMeshProUGUI controlsText = CreateText(controlsCard, "Taula", ControlsTable(), 28, TextColor, FontStyles.Normal, 430);
        controlsText.alignment = TextAlignmentOptions.TopLeft;
        controlsText.lineSpacing = 12f;
        Button controlsBackButton = CreateButton(controlsCard, "Tornar");

        // --- Opcions ---
        GameObject optionsScreen = CreateScreen(root.transform, "PantallaOpcions", 950, out Transform optionsCard);
        CreateText(optionsCard, "Titol", "OPCIONS", 64, AccentColor, FontStyles.Bold, 80);
        Slider masterSlider = CreateSliderRow(optionsCard, "Volum general", 0f, 1f, 1f);
        Slider musicSlider = CreateSliderRow(optionsCard, "Música", 0f, 1f, 0.5f);
        Slider effectsSlider = CreateSliderRow(optionsCard, "Efectes de so", 0f, 1f, 0.8f);
        Slider mouseSlider = CreateSliderRow(optionsCard, "Sensibilitat del ratolí", 0.25f, 3f, 1f);
        Slider gamepadSlider = CreateSliderRow(optionsCard, "Sensibilitat del comandament", 0.25f, 3f, 1f);
        CreateSpacer(optionsCard, 10);
        Button optionsBackButton = CreateButton(optionsCard, "Tornar");

        // --- Victòria ---
        GameObject victoryScreen = CreateScreen(root.transform, "PantallaVictoria", 820, out Transform victoryCard);
        CreateText(victoryCard, "Titol", "HAS AJUDAT TOTS ELS ANIMALS!", 52, AccentColor, FontStyles.Bold, 80);
        CreateText(victoryCard, "Subtitol", "Has completat totes les missions de l'illa.", 30, SubtleTextColor, FontStyles.Normal, 44);
        TextMeshProUGUI timeText = CreateText(victoryCard, "Temps", "Temps: 00:00", 38, TextColor, FontStyles.Bold, 60);
        CreateSpacer(victoryCard, 10);
        Button victoryMenuButton = CreateButton(victoryCard, "Menú principal");
        Button victoryQuitButton = CreateButton(victoryCard, "Sortir");

        // --- Scripts ---
        MainMenu mainMenu = Undo.AddComponent<MainMenu>(root);
        SetObject(mainMenu, "mainMenuPanel", mainScreen);

        OverlayPanel controlsOverlay = Undo.AddComponent<OverlayPanel>(controlsScreen);
        OverlayPanel optionsOverlay = Undo.AddComponent<OverlayPanel>(optionsScreen);

        OptionsMenu optionsMenu = Undo.AddComponent<OptionsMenu>(optionsScreen);
        SetObject(optionsMenu, "masterSlider", masterSlider);
        SetObject(optionsMenu, "musicSlider", musicSlider);
        SetObject(optionsMenu, "effectsSlider", effectsSlider);
        SetObject(optionsMenu, "mouseSlider", mouseSlider);
        SetObject(optionsMenu, "gamepadSlider", gamepadSlider);

        SetObject(pauseMenu, "pausePanel", pauseScreen);
        SetObject(victoria, "panellVictoria", victoryScreen);
        SetObject(victoria, "textTemps", timeText);

        // --- Botons ---
        UnityEventTools.AddPersistentListener(playButton.onClick, mainMenu.StartGame);
        UnityEventTools.AddObjectPersistentListener<GameObject>(mainControlsButton.onClick, controlsOverlay.OpenFrom, mainScreen);
        UnityEventTools.AddObjectPersistentListener<GameObject>(mainOptionsButton.onClick, optionsOverlay.OpenFrom, mainScreen);
        UnityEventTools.AddPersistentListener(quitButton.onClick, mainMenu.QuitGame);

        UnityEventTools.AddPersistentListener(resumeButton.onClick, pauseMenu.ResumeGame);
        UnityEventTools.AddObjectPersistentListener<GameObject>(pauseControlsButton.onClick, controlsOverlay.OpenFrom, pauseScreen);
        UnityEventTools.AddObjectPersistentListener<GameObject>(pauseOptionsButton.onClick, optionsOverlay.OpenFrom, pauseScreen);
        UnityEventTools.AddPersistentListener(pauseMainMenuButton.onClick, pauseMenu.RestartLevel);

        UnityEventTools.AddPersistentListener(controlsBackButton.onClick, controlsOverlay.Close);
        UnityEventTools.AddPersistentListener(optionsBackButton.onClick, optionsOverlay.Close);

        UnityEventTools.AddPersistentListener(victoryMenuButton.onClick, victoria.ReiniciarJoc);
        UnityEventTools.AddPersistentListener(victoryQuitButton.onClick, victoria.SortirDelJoc);

        // Només el menú principal es veu en començar (els scripts ja amaguen i mostren la resta)
        mainScreen.SetActive(true);
        pauseScreen.SetActive(false);
        controlsScreen.SetActive(false);
        optionsScreen.SetActive(false);
        victoryScreen.SetActive(false);

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = root;

        EditorUtility.DisplayDialog("Construir menús", "Menús creats. Desa l'escena (Ctrl+S) i prova-ho amb Play.", "D'acord");
    }

    private static string ControlsTable()
    {
        string accent = "#" + ColorUtility.ToHtmlStringRGB(AccentColor);
        string[,] rows =
        {
            { "Moure / Escalar", "W A S D", "Joystick esquerre" },
            { "Mirar / Apuntar", "Ratolí", "Joystick dret" },
            { "Córrer (i escalar més ràpid)", "Shift", "L3" },
            { "Saltar / Salt de paret", "Espai", "A" },
            { "Swing", "Clic esquerre / F", "RT" },
            { "Grappling (recollir el fil)", "Q", "LT" },
            { "Connectar (pont)", "Clic dret / G", "RB" },
            { "Interactuar", "E", "X" },
            { "Pausa", "Esc / P", "Menú" },
        };

        string text = $"<b><color={accent}>Acció<pos=48%>Teclat i ratolí<pos=76%>Comandament</color></b>\n";
        for (int i = 0; i < rows.GetLength(0); i++)
        {
            text += $"{rows[i, 0]}<pos=48%>{rows[i, 1]}<pos=76%>{rows[i, 2]}\n";
        }
        return text.TrimEnd('\n');
    }

    // --- Peces de la interfície ---

    private static GameObject CreateUIObject(string objectName, Transform parent)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");
        return go;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // Pantalla: fons enfosquit que ocupa tota la pantalla i una targeta al mig on van els elements
    private static GameObject CreateScreen(Transform parent, string screenName, float cardWidth, out Transform card)
    {
        GameObject screen = CreateUIObject(screenName, parent);
        Stretch(screen.GetComponent<RectTransform>());
        Image dim = screen.AddComponent<Image>();
        dim.color = DimColor;

        GameObject cardObject = CreateUIObject("Targeta", screen.transform);
        RectTransform cardRect = cardObject.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(cardWidth, 0f);

        Image cardImage = cardObject.AddComponent<Image>();
        cardImage.sprite = roundedSprite;
        cardImage.type = Image.Type.Sliced;
        cardImage.color = PanelColor;

        Shadow shadow = cardObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
        shadow.effectDistance = new Vector2(0f, -8f);

        VerticalLayoutGroup layout = cardObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(56, 56, 44, 48);
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = cardObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        card = cardObject.transform;
        return screen;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string objectName, string content, float size, Color color, FontStyles style, float height)
    {
        GameObject go = CreateUIObject(objectName, parent);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = size;
        text.color = color;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        LayoutElement element = go.AddComponent<LayoutElement>();
        element.preferredHeight = height;
        return text;
    }

    private static void CreateSpacer(Transform parent, float height)
    {
        GameObject go = CreateUIObject("Espai", parent);
        LayoutElement element = go.AddComponent<LayoutElement>();
        element.preferredHeight = height;
    }

    // Botó arrodonit: gris fosc en repòs i vermell quan el ratolí hi passa per sobre o està seleccionat amb el comandament
    private static Button CreateButton(Transform parent, string label)
    {
        GameObject go = CreateUIObject("Boto_" + label, parent);
        Image image = go.AddComponent<Image>();
        image.sprite = roundedSprite;
        image.type = Image.Type.Sliced;
        image.color = Color.white;

        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = ButtonColor;
        colors.highlightedColor = AccentColor;
        colors.selectedColor = AccentColor;
        colors.pressedColor = new Color(AccentColor.r * 0.75f, AccentColor.g * 0.75f, AccentColor.b * 0.75f, 1f);
        colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        LayoutElement element = go.AddComponent<LayoutElement>();
        element.preferredHeight = 68f;

        GameObject labelObject = CreateUIObject("Text", go.transform);
        Stretch(labelObject.GetComponent<RectTransform>());
        TextMeshProUGUI text = labelObject.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 32;
        text.color = TextColor;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        return button;
    }

    // Fila d'opcions: nom a l'esquerra i control lliscant a la dreta
    private static Slider CreateSliderRow(Transform parent, string label, float min, float max, float value)
    {
        GameObject row = CreateUIObject("Fila_" + label, parent);
        HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 24f;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;
        LayoutElement rowElement = row.AddComponent<LayoutElement>();
        rowElement.preferredHeight = 56f;

        TextMeshProUGUI text = CreateText(row.transform, "Nom", label, 28, TextColor, FontStyles.Normal, 56);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        LayoutElement textElement = text.GetComponent<LayoutElement>();
        textElement.preferredWidth = 400f;

        GameObject sliderObject = CreateUIObject("Control", row.transform);
        LayoutElement sliderElement = sliderObject.AddComponent<LayoutElement>();
        sliderElement.flexibleWidth = 1f;
        sliderElement.preferredHeight = 30f;

        GameObject background = CreateUIObject("Fons", sliderObject.transform);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 0.3f);
        backgroundRect.anchorMax = new Vector2(1f, 0.7f);
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.sprite = trackSprite;
        backgroundImage.type = Image.Type.Sliced;
        backgroundImage.color = TrackColor;

        GameObject fillArea = CreateUIObject("Zona_Ple", sliderObject.transform);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.3f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.7f);
        fillAreaRect.offsetMin = new Vector2(5f, 0f);
        fillAreaRect.offsetMax = new Vector2(-15f, 0f);
        GameObject fill = CreateUIObject("Ple", fillArea.transform);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.sizeDelta = new Vector2(10f, 0f);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.sprite = trackSprite;
        fillImage.type = Image.Type.Sliced;
        fillImage.color = AccentColor;

        GameObject handleArea = CreateUIObject("Zona_Nansa", sliderObject.transform);
        RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(10f, 0f);
        handleAreaRect.offsetMax = new Vector2(-10f, 0f);
        GameObject handle = CreateUIObject("Nansa", handleArea.transform);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(30f, 0f);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.sprite = knobSprite;
        handleImage.color = Color.white;

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = value;
        ColorBlock colors = slider.colors;
        colors.highlightedColor = new Color(1f, 0.8f, 0.78f, 1f);
        colors.selectedColor = new Color(1f, 0.8f, 0.78f, 1f);
        slider.colors = colors;

        return slider;
    }

    // --- Ajudes ---

    private static Object GetObject(Object target, string propertyName)
    {
        SerializedProperty property = new SerializedObject(target).FindProperty(propertyName);
        return property != null ? property.objectReferenceValue : null;
    }

    private static void SetObject(Object target, string propertyName, Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogWarning($"[Construir menús] {target.GetType().Name} no té el camp «{propertyName}».");
            return;
        }
        property.objectReferenceValue = value;
        serialized.ApplyModifiedProperties();
    }

    private static void DeactivateOld(GameObject panel)
    {
        if (panel == null) return;
        Undo.RecordObject(panel, "Construir menús");
        panel.SetActive(false);
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include) != null) return;

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        Undo.RegisterCreatedObjectUndo(eventSystem, "Construir menús");
    }
}
