using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Pantalla d'opcions amb pestanyes (So, Càmera i Pantalla). Els valors es guarden a GameSettings
public class OptionsMenu : MonoBehaviour
{
    [System.Serializable]
    public class Tab
    {
        public Button button;
        public GameObject page; // El contingut que es veu quan la pestanya està triada
        public GameObject indicator; // La ratlla de color de sota la pestanya triada
        public TextMeshProUGUI label;
    }

    [Header("Pestanyes")]
    [SerializeField] private Tab[] tabs;
    [SerializeField] private Color activeTabTextColor = new Color(0.96f, 0.96f, 0.96f, 1f);
    [SerializeField] private Color inactiveTabTextColor = new Color(0.55f, 0.6f, 0.65f, 1f);

    [Header("So")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider effectsSlider;

    [Header("Càmera")]
    [SerializeField] private Slider mouseSlider;
    [SerializeField] private Slider gamepadSlider;
    [SerializeField] private Toggle invertYToggle;

    [Header("Pantalla")]
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TextMeshProUGUI qualityText;
    [SerializeField] private Toggle vSyncToggle;

    private int currentTab = 0;
    private float lastPreviewTime = -1f;

    void Awake()
    {
        if (masterSlider != null) masterSlider.onValueChanged.AddListener(value => GameSettings.MasterVolume = value);
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(value => GameSettings.MusicVolume = value);
        if (effectsSlider != null) effectsSlider.onValueChanged.AddListener(OnEffectsChanged);
        if (mouseSlider != null) mouseSlider.onValueChanged.AddListener(value => GameSettings.MouseSensitivity = value);
        if (gamepadSlider != null) gamepadSlider.onValueChanged.AddListener(value => GameSettings.GamepadSensitivity = value);
        if (invertYToggle != null) invertYToggle.onValueChanged.AddListener(value => GameSettings.InvertY = value);
        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.AddListener(value => GameSettings.Fullscreen = value);
        if (vSyncToggle != null) vSyncToggle.onValueChanged.AddListener(value => GameSettings.VSync = value);
    }

    void OnEnable()
    {
        // Cada vegada que s'obre la pantalla mostrem els valors actuals
        if (masterSlider != null) masterSlider.SetValueWithoutNotify(GameSettings.MasterVolume);
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(GameSettings.MusicVolume);
        if (effectsSlider != null) effectsSlider.SetValueWithoutNotify(GameSettings.EffectsVolume);
        if (mouseSlider != null) mouseSlider.SetValueWithoutNotify(GameSettings.MouseSensitivity);
        if (gamepadSlider != null) gamepadSlider.SetValueWithoutNotify(GameSettings.GamepadSensitivity);
        if (invertYToggle != null) invertYToggle.SetIsOnWithoutNotify(GameSettings.InvertY);
        if (fullscreenToggle != null) fullscreenToggle.SetIsOnWithoutNotify(GameSettings.Fullscreen);
        if (vSyncToggle != null) vSyncToggle.SetIsOnWithoutNotify(GameSettings.VSync);
        UpdateQualityText();

        // Si aquesta plataforma només té un nivell de qualitat, no hi ha res a triar: amaguem la fila sencera
        if (qualityText != null) qualityText.transform.parent.gameObject.SetActive(QualitySettings.names.Length > 1);

        // Es torna a obrir per la pestanya que es va fer servir l'última vegada
        ShowTab(currentTab);
    }

    void OnDisable()
    {
        GameSettings.Save();
    }

    void Update()
    {
        // Amb el comandament, LB i RB canvien de pestanya
        Gamepad gamepad = Gamepad.current;
        if (gamepad == null || tabs == null || tabs.Length == 0) return;

        int direction = 0;
        if (gamepad.leftShoulder.wasPressedThisFrame) direction = -1;
        else if (gamepad.rightShoulder.wasPressedThisFrame) direction = 1;
        if (direction == 0) return;

        ShowTab(currentTab + direction);
        AudioManager.Play("ui_click", 0.8f, 0f);

        // El que estava seleccionat pot haver quedat amagat: seleccionem la pestanya nova
        if (EventSystem.current != null && tabs[currentTab].button != null)
        {
            EventSystem.current.SetSelectedGameObject(tabs[currentTab].button.gameObject);
        }
    }

    // Mostra el contingut d'una pestanya i marca-la com a triada (es crida des dels botons de les pestanyes)
    public void ShowTab(int index)
    {
        if (tabs == null || tabs.Length == 0) return;

        // Passant de l'última pestanya es torna a la primera (i al revés)
        currentTab = (index % tabs.Length + tabs.Length) % tabs.Length;

        for (int i = 0; i < tabs.Length; i++)
        {
            bool active = i == currentTab;
            if (tabs[i].page != null) tabs[i].page.SetActive(active);
            if (tabs[i].indicator != null) tabs[i].indicator.SetActive(active);
            if (tabs[i].label != null) tabs[i].label.color = active ? activeTabTextColor : inactiveTabTextColor;
        }
    }

    // Botons «<» i «>» de la qualitat gràfica
    public void NextQuality()
    {
        ChangeQuality(1);
    }

    public void PreviousQuality()
    {
        ChangeQuality(-1);
    }

    private void ChangeQuality(int direction)
    {
        int count = QualitySettings.names.Length;
        GameSettings.QualityLevel = (GameSettings.QualityLevel + direction + count) % count;
        UpdateQualityText();
    }

    private void UpdateQualityText()
    {
        if (qualityText != null) qualityText.text = QualityName(GameSettings.QualityLevel);
    }

    // Els nivells de Project Settings > Quality tenen noms en anglès (Mobile, PC): al menú els mostrem en català
    private static string QualityName(int level)
    {
        string[] names = QualitySettings.names;
        if (names.Length == 2) return level == 0 ? "Baixa" : "Alta";
        if (names.Length == 3) return new[] { "Baixa", "Mitjana", "Alta" }[level];
        return names[level];
    }

    private void OnEffectsChanged(float value)
    {
        GameSettings.EffectsVolume = value;

        // Un clic perquè el jugador senti com sona el volum nou (no a cada petit moviment del control)
        if (Time.unscaledTime - lastPreviewTime > 0.12f)
        {
            lastPreviewTime = Time.unscaledTime;
            AudioManager.Play("ui_click", 0.8f, 0f);
        }
    }
}
