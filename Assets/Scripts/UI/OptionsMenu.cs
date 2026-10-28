using UnityEngine;
using UnityEngine.UI;

// Pantalla d'opcions: volums i sensibilitat de la càmera. Els valors es guarden a GameSettings
public class OptionsMenu : MonoBehaviour
{
    [Header("Controls lliscants")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider effectsSlider;
    [SerializeField] private Slider mouseSlider;
    [SerializeField] private Slider gamepadSlider;

    void Awake()
    {
        if (masterSlider != null) masterSlider.onValueChanged.AddListener(value => GameSettings.MasterVolume = value);
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(value => GameSettings.MusicVolume = value);
        if (effectsSlider != null) effectsSlider.onValueChanged.AddListener(OnEffectsChanged);
        if (mouseSlider != null) mouseSlider.onValueChanged.AddListener(value => GameSettings.MouseSensitivity = value);
        if (gamepadSlider != null) gamepadSlider.onValueChanged.AddListener(value => GameSettings.GamepadSensitivity = value);
    }

    void OnEnable()
    {
        // Cada vegada que s'obre la pantalla mostrem els valors actuals
        if (masterSlider != null) masterSlider.SetValueWithoutNotify(GameSettings.MasterVolume);
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(GameSettings.MusicVolume);
        if (effectsSlider != null) effectsSlider.SetValueWithoutNotify(GameSettings.EffectsVolume);
        if (mouseSlider != null) mouseSlider.SetValueWithoutNotify(GameSettings.MouseSensitivity);
        if (gamepadSlider != null) gamepadSlider.SetValueWithoutNotify(GameSettings.GamepadSensitivity);
    }

    void OnDisable()
    {
        GameSettings.Save();
    }

    private float lastPreviewTime = -1f;

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
