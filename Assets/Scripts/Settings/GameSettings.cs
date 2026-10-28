using UnityEngine;

// Opcions del jugador (volums i sensibilitat de la càmera).
// Es guarden amb PlayerPrefs perquè es mantinguin entre partides
public static class GameSettings
{
    private const string MasterKey = "opcions_volum_general";
    private const string MusicKey = "opcions_volum_musica";
    private const string EffectsKey = "opcions_volum_efectes";
    private const string MouseKey = "opcions_sensibilitat_ratoli";
    private const string GamepadKey = "opcions_sensibilitat_comandament";

    // Avisa quan canvia qualsevol opció (per exemple, perquè l'AudioManager actualitzi els volums)
    public static event System.Action Changed;

    private static bool loaded = false;
    private static float masterVolume;
    private static float musicVolume;
    private static float effectsVolume;
    private static float mouseSensitivity;
    private static float gamepadSensitivity;

    private static void Load()
    {
        if (loaded) return;
        loaded = true;

        masterVolume = PlayerPrefs.GetFloat(MasterKey, 1f);
        musicVolume = PlayerPrefs.GetFloat(MusicKey, 0.5f);
        effectsVolume = PlayerPrefs.GetFloat(EffectsKey, 0.8f);
        mouseSensitivity = PlayerPrefs.GetFloat(MouseKey, 1f);
        gamepadSensitivity = PlayerPrefs.GetFloat(GamepadKey, 1f);
    }

    public static float MasterVolume
    {
        get { Load(); return masterVolume; }
        set { Load(); masterVolume = Mathf.Clamp01(value); Store(MasterKey, masterVolume); }
    }

    public static float MusicVolume
    {
        get { Load(); return musicVolume; }
        set { Load(); musicVolume = Mathf.Clamp01(value); Store(MusicKey, musicVolume); }
    }

    public static float EffectsVolume
    {
        get { Load(); return effectsVolume; }
        set { Load(); effectsVolume = Mathf.Clamp01(value); Store(EffectsKey, effectsVolume); }
    }

    // Multiplicadors de la sensibilitat de la càmera (1 = valor de l'Inspector)
    public static float MouseSensitivity
    {
        get { Load(); return mouseSensitivity; }
        set { Load(); mouseSensitivity = Mathf.Clamp(value, 0.1f, 5f); Store(MouseKey, mouseSensitivity); }
    }

    public static float GamepadSensitivity
    {
        get { Load(); return gamepadSensitivity; }
        set { Load(); gamepadSensitivity = Mathf.Clamp(value, 0.1f, 5f); Store(GamepadKey, gamepadSensitivity); }
    }

    // Escriu els valors al disc (es crida en tancar el menú d'opcions)
    public static void Save()
    {
        PlayerPrefs.Save();
    }

    private static void Store(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        Changed?.Invoke();
    }
}
