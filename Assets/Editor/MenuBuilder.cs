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
// menú principal, pausa, controls, opcions (amb pestanyes) i pantalla de victòria, ja connectats als scripts.
// Es fa servir des del menú de Unity: Tools > TFG > Construir menús (es pot tornar a executar per refer-los)
public static class MenuBuilder
{
    private const string GameTitle = "L'ARANYA"; // Canvia-ho pel nom del joc
    private const string GameSubtitle = "Un joc de plataformes amb teranyines";
    private const string RootName = "UI_Menus";
    private const string GameScenePath = "Assets/Scenes/Joc.unity";

    // Paleta: el vermell és el mateix de l'aranya i de la marieta
    private static readonly Color DimColor = new Color(0f, 0f, 0f, 0.55f);
    private static readonly Color PanelColor = new Color(0.07f, 0.09f, 0.11f, 0.94f);
    private static readonly Color AccentColor = new Color32(0xF4, 0x43, 0x36, 0xFF);
    private static readonly Color ButtonColor = new Color(0.17f, 0.2f, 0.23f, 1f);
    private static readonly Color HoverColor = new Color(0.3f, 0.34f, 0.39f, 1f); // Pestanyes i caselles quan el ratolí hi passa per sobre
    private static readonly Color TrackColor = new Color(0.25f, 0.28f, 0.32f, 1f);
    private static readonly Color TextColor = new Color(0.96f, 0.96f, 0.96f, 1f);
    private static readonly Color SubtleTextColor = new Color(0.72f, 0.76f, 0.8f, 1f);
    private static readonly Color InactiveTabTextColor = new Color(0.55f, 0.6f, 0.65f, 1f);

    private const float RowLabelWidth = 440f; // Amplada dels noms de les opcions: els controls queden alineats a la mateixa columna

    private static Sprite roundedSprite;
    private static Sprite trackSprite;
    private static Sprite knobSprite;

    [MenuItem("Tools/TFG/Construir menús")]
    public static void Build()
    {
        if (!FindMenuScripts(out PauseMenu pauseMenu, out MenuVictoria victoria))
        {
            EditorUtility.DisplayDialog("Construir menús", "No he trobat PauseMenu o MenuVictoria a l'escena oberta. Obre Joc.unity i torna-ho a provar.", "D'acord");
            return;
        }

        bool accept = EditorUtility.DisplayDialog("Construir menús",
            "Es crearà l'objecte «" + RootName + "» amb el menú principal, la pausa, els controls, les opcions i la victòria.\n\n" +
            "Els panells antics de pausa i victòria es desactivaran (no s'esborren). Si ja existeix «" + RootName + "», es refarà.\n\n" +
            "Ho pots desfer amb Ctrl+Z.", "Construir", "Cancel·lar");
        if (!accept) return;

        BuildMenus(pauseMenu, victoria);
        Selection.activeGameObject = GameObject.Find(RootName);

        EditorUtility.DisplayDialog("Construir menús", "Menús creats. Desa l'escena (Ctrl+S) i prova-ho amb Play.", "D'acord");
    }

    // La mateixa eina sense finestres, per fer-la servir des de la línia d'ordres amb Unity tancat:
    // Unity.exe -batchmode -quit -projectPath <carpeta del projecte> -executeMethod MenuBuilder.BuildFromCommandLine
    // Obre l'escena del joc, construeix els menús i desa l'escena
    public static void BuildFromCommandLine()
    {
        EditorSceneManager.OpenScene(GameScenePath);
        if (!FindMenuScripts(out PauseMenu pauseMenu, out MenuVictoria victoria))
        {
            throw new System.Exception("[Construir menús] No he trobat PauseMenu o MenuVictoria a " + GameScenePath);
        }

        BuildMenus(pauseMenu, victoria);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("[Construir menús] Menús creats i escena desada.");
    }

    private static bool FindMenuScripts(out PauseMenu pauseMenu, out MenuVictoria victoria)
    {
        pauseMenu = Object.FindAnyObjectByType<PauseMenu>(FindObjectsInactive.Include);
        victoria = Object.FindAnyObjectByType<MenuVictoria>(FindObjectsInactive.Include);
        return pauseMenu != null && victoria != null;
    }

    private static void BuildMenus(PauseMenu pauseMenu, MenuVictoria victoria)
    {
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

        // --- Opcions: una pestanya per a cada grup d'opcions ---
        GameObject optionsScreen = CreateScreen(root.transform, "PantallaOpcions", 1060, out Transform optionsCard);
        CreateText(optionsCard, "Titol", "OPCIONS", 64, AccentColor, FontStyles.Bold, 80);

        Transform tabBar = CreateTabBar(optionsCard);
        string[] tabNames = { "So", "Càmera", "Pantalla" };
        Button[] tabButtons = new Button[tabNames.Length];
        GameObject[] tabIndicators = new GameObject[tabNames.Length];
        TextMeshProUGUI[] tabLabels = new TextMeshProUGUI[tabNames.Length];
        for (int i = 0; i < tabNames.Length; i++)
        {
            tabButtons[i] = CreateTabButton(tabBar, tabNames[i], out tabIndicators[i], out tabLabels[i]);
        }

        Transform soundPage = CreatePage(optionsCard, "Pagina_So");
        Slider masterSlider = CreateSliderRow(soundPage, "Volum general", 0f, 1f, 1f, true);
        Slider musicSlider = CreateSliderRow(soundPage, "Música", 0f, 1f, 0.5f, true);
        Slider effectsSlider = CreateSliderRow(soundPage, "Efectes de so", 0f, 1f, 0.8f, true);

        Transform cameraPage = CreatePage(optionsCard, "Pagina_Camera");
        Slider mouseSlider = CreateSliderRow(cameraPage, "Sensibilitat del ratolí", 0.25f, 3f, 1f, false);
        Slider gamepadSlider = CreateSliderRow(cameraPage, "Sensibilitat del comandament", 0.25f, 3f, 1f, false);
        Toggle invertYToggle = CreateToggleRow(cameraPage, "Invertir l'eix vertical", false);

        Transform screenPage = CreatePage(optionsCard, "Pagina_Pantalla");
        Toggle fullscreenToggle = CreateToggleRow(screenPage, "Pantalla completa", true);
        TextMeshProUGUI qualityText = CreateSelectorRow(screenPage, "Qualitat gràfica", "Alta", out Button qualityPreviousButton, out Button qualityNextButton);
        Toggle vSyncToggle = CreateToggleRow(screenPage, "Sincronització vertical", true);

        GameObject[] tabPages = { soundPage.gameObject, cameraPage.gameObject, screenPage.gameObject };

        CreateSpacer(optionsCard, 6);
        Button optionsBackButton = CreateButton(optionsCard, "Tornar");

        // --- Victòria ---
        GameObject victoryScreen = CreateScreen(root.transform, "PantallaVictoria", 1000, out Transform victoryCard);
        CreateText(victoryCard, "Titol", "HAS AJUDAT TOTS ELS ANIMALS!", 46, AccentColor, FontStyles.Bold, 80);
        CreateText(victoryCard, "Subtitol", "Has completat totes les missions de l'illa.", 30, SubtleTextColor, FontStyles.Normal, 44);
        TextMeshProUGUI timeText = CreateText(victoryCard, "Temps", "Temps: 00:00", 38, TextColor, FontStyles.Bold, 60);
        CreateSpacer(victoryCard, 10);
        Button victoryMenuButton = CreateButton(victoryCard, "Menú principal");
        Button victoryQuitButton = CreateButton(victoryCard, "Sortir");

        // --- Scripts ---
        MainMenu mainMenu = Undo.AddComponent<MainMenu>(root);
        SetObject(mainMenu, "mainMenuPanel", mainScreen);
        SetObject(mainMenu, "hudCanvas", FindHudCanvas(canvas));

        OverlayPanel controlsOverlay = Undo.AddComponent<OverlayPanel>(controlsScreen);
        OverlayPanel optionsOverlay = Undo.AddComponent<OverlayPanel>(optionsScreen);

        OptionsMenu optionsMenu = Undo.AddComponent<OptionsMenu>(optionsScreen);
        SetObject(optionsMenu, "masterSlider", masterSlider);
        SetObject(optionsMenu, "musicSlider", musicSlider);
        SetObject(optionsMenu, "effectsSlider", effectsSlider);
        SetObject(optionsMenu, "mouseSlider", mouseSlider);
        SetObject(optionsMenu, "gamepadSlider", gamepadSlider);
        SetObject(optionsMenu, "invertYToggle", invertYToggle);
        SetObject(optionsMenu, "fullscreenToggle", fullscreenToggle);
        SetObject(optionsMenu, "qualityText", qualityText);
        SetObject(optionsMenu, "vSyncToggle", vSyncToggle);
        SetTabs(optionsMenu, tabButtons, tabPages, tabIndicators, tabLabels);

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

        for (int i = 0; i < tabButtons.Length; i++)
        {
            UnityEventTools.AddIntPersistentListener(tabButtons[i].onClick, optionsMenu.ShowTab, i);
        }
        UnityEventTools.AddPersistentListener(qualityPreviousButton.onClick, optionsMenu.PreviousQuality);
        UnityEventTools.AddPersistentListener(qualityNextButton.onClick, optionsMenu.NextQuality);

        UnityEventTools.AddPersistentListener(victoryMenuButton.onClick, victoria.ReiniciarJoc);
        UnityEventTools.AddPersistentListener(victoryQuitButton.onClick, victoria.SortirDelJoc);

        // Només el menú principal es veu en començar (els scripts ja amaguen i mostren la resta)
        mainScreen.SetActive(true);
        pauseScreen.SetActive(false);
        controlsScreen.SetActive(false);
        optionsScreen.SetActive(false);
        victoryScreen.SetActive(false);

        // A Opcions comença triada la primera pestanya
        for (int i = 0; i < tabPages.Length; i++)
        {
            tabPages[i].SetActive(i == 0);
            tabIndicators[i].SetActive(i == 0);
            tabLabels[i].color = i == 0 ? TextColor : InactiveTabTextColor;
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    // El HUD del joc és el llenç del text de les missions. El menú d'inici l'amaga mentre es veu
    private static Canvas FindHudCanvas(Canvas menuCanvas)
    {
        MissionManager missionManager = Object.FindAnyObjectByType<MissionManager>(FindObjectsInactive.Include);
        TextMeshProUGUI questText = missionManager != null ? GetObject(missionManager, "questTrackerText") as TextMeshProUGUI : null;
        Canvas hud = questText != null ? questText.GetComponentInParent<Canvas>(true) : null;
        if (hud != null) hud = hud.rootCanvas;

        if (hud == null || hud == menuCanvas)
        {
            Debug.LogWarning("[Construir menús] No he trobat el llenç del HUD: el menú d'inici no l'amagarà.");
            return null;
        }
        return hud;
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

    // --- Pestanyes ---

    private static Transform CreateTabBar(Transform parent)
    {
        GameObject bar = CreateUIObject("Pestanyes", parent);
        HorizontalLayoutGroup layout = bar.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 12f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        LayoutElement element = bar.AddComponent<LayoutElement>();
        element.preferredHeight = 60f;
        return bar.transform;
    }

    // Pestanya: un botó amb una ratlla vermella a sota que indica quina està triada
    private static Button CreateTabButton(Transform parent, string label, out GameObject indicator, out TextMeshProUGUI text)
    {
        Button button = CreateButton(parent, label);
        button.gameObject.name = "Pestanya_" + label;
        LayoutElement element = button.GetComponent<LayoutElement>();
        element.preferredHeight = 60f;
        element.flexibleWidth = 1f;

        ColorBlock colors = button.colors;
        colors.highlightedColor = HoverColor;
        colors.selectedColor = HoverColor;
        button.colors = colors;

        text = button.GetComponentInChildren<TextMeshProUGUI>();
        text.fontSize = 30;

        indicator = CreateUIObject("Indicador", button.transform);
        RectTransform rect = indicator.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.offsetMin = new Vector2(20f, 6f);
        rect.offsetMax = new Vector2(-20f, 12f);
        Image image = indicator.AddComponent<Image>();
        image.color = AccentColor;
        image.raycastTarget = false;

        return button;
    }

    // Contingut d'una pestanya: les files d'opcions una sota l'altra
    private static Transform CreatePage(Transform parent, string objectName)
    {
        GameObject page = CreateUIObject(objectName, parent);
        VerticalLayoutGroup layout = page.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 8, 0);
        layout.spacing = 16f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return page.transform;
    }

    // --- Files d'opcions: nom a l'esquerra i control a la dreta ---

    private static Transform CreateRow(Transform parent, string label)
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
        text.textWrappingMode = TextWrappingModes.NoWrap;
        LayoutElement textElement = text.GetComponent<LayoutElement>();
        textElement.preferredWidth = RowLabelWidth;

        return row.transform;
    }

    // Fila amb una casella per activar o desactivar una opció
    private static Toggle CreateToggleRow(Transform parent, string label, bool value)
    {
        Transform row = CreateRow(parent, label);

        GameObject box = CreateUIObject("Control", row);
        LayoutElement boxElement = box.AddComponent<LayoutElement>();
        boxElement.preferredWidth = 44f;
        boxElement.preferredHeight = 44f;
        Image boxImage = box.AddComponent<Image>();
        boxImage.sprite = roundedSprite;
        boxImage.type = Image.Type.Sliced;
        boxImage.color = Color.white;

        GameObject mark = CreateUIObject("Marca", box.transform);
        RectTransform markRect = mark.GetComponent<RectTransform>();
        Stretch(markRect);
        markRect.offsetMin = new Vector2(9f, 9f);
        markRect.offsetMax = new Vector2(-9f, -9f);
        // Quan l'opció està activada, la casella s'omple amb un quadrat vermell
        Image markImage = mark.AddComponent<Image>();
        markImage.sprite = roundedSprite;
        markImage.type = Image.Type.Sliced;
        markImage.color = AccentColor;
        markImage.raycastTarget = false;

        Toggle toggle = box.AddComponent<Toggle>();
        toggle.targetGraphic = boxImage;
        toggle.graphic = markImage;
        toggle.isOn = value;
        ColorBlock colors = toggle.colors;
        colors.normalColor = TrackColor;
        colors.highlightedColor = HoverColor;
        colors.selectedColor = HoverColor;
        colors.pressedColor = ButtonColor;
        colors.fadeDuration = 0.08f;
        toggle.colors = colors;

        // Espai buit a la dreta perquè la casella quedi a la mateixa columna que els controls lliscants
        GameObject filler = CreateUIObject("Espai", row);
        filler.AddComponent<LayoutElement>().flexibleWidth = 1f;

        return toggle;
    }

    // Fila amb un valor i dos botons («<» i «>») per canviar-lo
    private static TextMeshProUGUI CreateSelectorRow(Transform parent, string label, string value, out Button previous, out Button next)
    {
        Transform row = CreateRow(parent, label);

        previous = CreateArrowButton(row, "<", "Boto_Anterior");
        TextMeshProUGUI valueText = CreateText(row, "Valor", value, 28, TextColor, FontStyles.Bold, 56);
        valueText.GetComponent<LayoutElement>().flexibleWidth = 1f;
        next = CreateArrowButton(row, ">", "Boto_Seguent");

        return valueText;
    }

    private static Button CreateArrowButton(Transform parent, string arrow, string objectName)
    {
        Button button = CreateButton(parent, arrow);
        button.gameObject.name = objectName;
        LayoutElement element = button.GetComponent<LayoutElement>();
        element.preferredWidth = 64f;
        element.preferredHeight = 48f;
        return button;
    }

    // Fila amb un control lliscant i el seu valor al costat
    private static Slider CreateSliderRow(Transform parent, string label, float min, float max, float value, bool asPercentage)
    {
        Transform row = CreateRow(parent, label);

        GameObject sliderObject = CreateUIObject("Control", row);
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

        TextMeshProUGUI valueText = CreateText(row, "Valor", "", 26, SubtleTextColor, FontStyles.Normal, 56);
        valueText.alignment = TextAlignmentOptions.MidlineRight;
        valueText.textWrappingMode = TextWrappingModes.NoWrap;
        valueText.GetComponent<LayoutElement>().preferredWidth = 100f;

        SliderValueText display = valueText.gameObject.AddComponent<SliderValueText>();
        SetObject(display, "slider", slider);
        SetObject(display, "text", valueText);
        SetBool(display, "asPercentage", asPercentage);
        display.Refresh();

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

    private static void SetBool(Object target, string propertyName, bool value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogWarning($"[Construir menús] {target.GetType().Name} no té el camp «{propertyName}».");
            return;
        }
        property.boolValue = value;
        serialized.ApplyModifiedProperties();
    }

    // Omple la llista de pestanyes de l'OptionsMenu (cada pestanya té el botó, el contingut, la ratlla i el text)
    private static void SetTabs(OptionsMenu optionsMenu, Button[] buttons, GameObject[] pages, GameObject[] indicators, TextMeshProUGUI[] labels)
    {
        SerializedObject serialized = new SerializedObject(optionsMenu);
        SerializedProperty tabs = serialized.FindProperty("tabs");
        tabs.arraySize = buttons.Length;
        for (int i = 0; i < buttons.Length; i++)
        {
            SerializedProperty tab = tabs.GetArrayElementAtIndex(i);
            tab.FindPropertyRelative("button").objectReferenceValue = buttons[i];
            tab.FindPropertyRelative("page").objectReferenceValue = pages[i];
            tab.FindPropertyRelative("indicator").objectReferenceValue = indicators[i];
            tab.FindPropertyRelative("label").objectReferenceValue = labels[i];
        }
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
