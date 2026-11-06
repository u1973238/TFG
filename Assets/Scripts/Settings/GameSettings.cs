using UnityEngine;

// Opcions del jugador (volums, càmera i pantalla).
// Es guarden amb PlayerPrefs perquè es mantinguin entre partides
public static class GameSettings
{
    private const string MasterKey = "opcions_volum_general";
    private const string MusicKey = "opcions_volum_musica";
    private const string EffectsKey = "opcions_volum_efectes";
    private const string MouseKey = "opcions_sensibilitat_ratoli";
    private const string GamepadKey = "opcions_sensibilitat_comandament";
    private const string InvertYKey = "opcions_invertir_y";
    private const string QualityKey = "opcions_qualitat";
    private const string VSyncKey = "opcions_vsync";

    // Mida de la finestra quan es treu la pantalla completa
    private const int WindowedWidth = 1280;
    private const int WindowedHeight = 720;

    // Avisa quan canvia qualsevol opció (per exemple, perquè l'AudioManager actualitzi els volums)
    public static event System.Action Changed;

    private static bool loaded = false;
    private static float masterVolume;
    private static float musicVolume;
    private static float effectsVolume;
    private static float mouseSensitivity;
    private static float gamepadSensitivity;
    private static bool invertY;
    private static int qualityLevel;
    private static bool vSync;

    private static void Load()
    {
        if (loaded) return;
        loaded = true;

        masterVolume = PlayerPrefs.GetFloat(MasterKey, 1f);
        musicVolume = PlayerPrefs.GetFloat(MusicKey, 0.5f);
        effectsVolume = PlayerPrefs.GetFloat(EffectsKey, 0.8f);
        mouseSensitivity = PlayerPrefs.GetFloat(MouseKey, 1f);
        gamepadSensitivity = PlayerPrefs.GetFloat(GamepadKey, 1f);
        invertY = PlayerPrefs.GetInt(InvertYKey, 0) == 1;
        // Si el jugador encara no l'ha canviat, es fa servir el nivell de Project Settings > Quality
        qualityLevel = Mathf.Clamp(PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel()), 0, QualitySettings.names.Length - 1);
        vSync = PlayerPrefs.GetInt(VSyncKey, 1) == 1;
    }

    // En arrencar el joc apliquem la qualitat i la sincronització vertical que el jugador va triar l'última vegada
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplyOnStart()
    {
        ApplyGraphics();
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

    // Inverteix el moviment vertical de la càmera (moure el ratolí amunt fa mirar avall)
    public static bool InvertY
    {
        get { Load(); return invertY; }
        set { Load(); invertY = value; Store(InvertYKey, value ? 1 : 0); }
    }

    // Nivell de qualitat gràfica (els nivells de Project Settings > Quality, de més baix a més alt)
    public static int QualityLevel
    {
        get { Load(); return qualityLevel; }
        set
        {
            Load();
            qualityLevel = Mathf.Clamp(value, 0, QualitySettings.names.Length - 1);
            Store(QualityKey, qualityLevel);
            ApplyGraphics();
        }
    }

    // Sincronització vertical: evita que la imatge es talli, però limita els FPS als de la pantalla
    public static bool VSync
    {
        get { Load(); return vSync; }
        set { Load(); vSync = value; Store(VSyncKey, value ? 1 : 0); ApplyGraphics(); }
    }

    // Pantalla completa o finestra. Aquesta opció ja la recorda Unity entre partides
    public static bool Fullscreen
    {
        get { return Screen.fullScreenMode != FullScreenMode.Windowed; }
        set
        {
            if (value) Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, FullScreenMode.FullScreenWindow);
            else Screen.SetResolution(WindowedWidth, WindowedHeight, FullScreenMode.Windowed);
            Changed?.Invoke();
        }
    }

    // Escriu els valors al disc (es crida en tancar el menú d'opcions)
    public static void Save()
    {
        PlayerPrefs.Save();
    }

    private static void ApplyGraphics()
    {
        Load();
        if (QualitySettings.GetQualityLevel() != qualityLevel) QualitySettings.SetQualityLevel(qualityLevel, true);

        // Canviar de nivell de qualitat també canvia la sincronització vertical, per això l'apliquem després
        QualitySettings.vSyncCount = vSync ? 1 : 0;
    }

    private static void Store(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        Changed?.Invoke();
    }

    private static void Store(string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
        Changed?.Invoke();
    }
}
