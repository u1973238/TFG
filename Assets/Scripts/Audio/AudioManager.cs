using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Gestor de so del joc. Es crea sol en començar (no cal posar-lo a l'escena) i es manté entre escenes.
// Cada so es demana pel nom, per exemple AudioManager.Play("jump").
// Si hi ha un arxiu a Assets/Resources/Audio/ amb aquest nom (jump.wav, jump.ogg...), es fa servir;
// si n'hi ha diversos numerats (step_1, step_2...), se'n tria un a l'atzar cada vegada perquè no soni repetitiu;
// si no, es genera un so senzill per codi (ProceduralSounds) perquè el joc no quedi mut
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private const int EffectVoices = 10; // Quants efectes poden sonar alhora

    private readonly List<AudioSource> effectSources = new List<AudioSource>();
    private readonly Dictionary<string, AudioClip[]> clips = new Dictionary<string, AudioClip[]>();
    private AudioSource musicSource;
    private AudioSource ambienceSource;
    private int nextVoice = 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateInstance()
    {
        if (Instance != null) return;

        GameObject audioObject = new GameObject("AudioManager");
        DontDestroyOnLoad(audioObject);
        audioObject.AddComponent<AudioManager>();
    }

    void Awake()
    {
        Instance = this;

        for (int i = 0; i < EffectVoices; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            effectSources.Add(source);
        }

        musicSource = CreateLoopSource(LoadClip("music"));
        ambienceSource = CreateLoopSource(LoadClip("ambience"));

        ApplyVolumes();
        GameSettings.Changed += ApplyVolumes;
        SceneManager.sceneLoaded += OnSceneLoaded;

        HookButtons();
    }

    void OnDestroy()
    {
        GameSettings.Changed -= ApplyVolumes;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    // Reprodueix un efecte de so pel nom. El to varia una mica cada vegada perquè no soni repetitiu
    public static void Play(string clipName, float volume = 1f, float pitchVariation = 0.06f, float basePitch = 1f)
    {
        if (Instance == null) return;

        AudioClip clip = Instance.LoadClip(clipName);
        if (clip == null) return;

        AudioSource source = Instance.effectSources[Instance.nextVoice];
        Instance.nextVoice = (Instance.nextVoice + 1) % Instance.effectSources.Count;

        source.clip = clip;
        source.volume = volume * GameSettings.EffectsVolume;
        source.pitch = basePitch + Random.Range(-pitchVariation, pitchVariation);
        source.Play();
    }

    // Per als objectes que necessiten el seu propi AudioSource (per exemple, el brunzit 3D de les mosques)
    public static AudioClip GetClip(string clipName)
    {
        return Instance != null ? Instance.LoadClip(clipName) : null;
    }

    private AudioClip LoadClip(string clipName)
    {
        if (!clips.TryGetValue(clipName, out AudioClip[] variations))
        {
            // Primer busquem arxius reals (el nom tal qual o variacions numerades); si no n'hi ha, en generem un de provisional
            List<AudioClip> found = new List<AudioClip>();
            AudioClip single = Resources.Load<AudioClip>("Audio/" + clipName);
            if (single != null) found.Add(single);
            for (int i = 1; i <= 9; i++)
            {
                AudioClip variation = Resources.Load<AudioClip>("Audio/" + clipName + "_" + i);
                if (variation != null) found.Add(variation);
            }
            if (found.Count == 0) found.AddRange(ProceduralSounds.CreateVariations(clipName));

            variations = found.ToArray();
            clips[clipName] = variations;
        }

        if (variations.Length == 0) return null;
        return variations[Random.Range(0, variations.Length)];
    }

    private AudioSource CreateLoopSource(AudioClip clip)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        if (clip != null) source.Play();
        return source;
    }

    private void ApplyVolumes()
    {
        AudioListener.volume = GameSettings.MasterVolume;
        musicSource.volume = 0.6f * GameSettings.MusicVolume;
        ambienceSource.volume = 0.5f * GameSettings.EffectsVolume;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        HookButtons();
    }

    // Tots els botons de l'escena fan so en clicar-los
    private void HookButtons()
    {
        foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include))
        {
            button.onClick.RemoveListener(PlayClick);
            button.onClick.AddListener(PlayClick);
        }
    }

    private void PlayClick()
    {
        Play("ui_click", 0.8f, 0f);
    }
}
