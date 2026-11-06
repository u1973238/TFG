using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Eina de l'editor que millora l'aspecte visual de l'escena oberta: postprocessat (color, bloom i vinyeta),
// un cel nou, boira a la llunyania, llum ambiental més suau i antialiàsing a la càmera.
// Es fa servir des del menú de Unity: Tools > TFG > Millorar l'aspecte visual (es pot tornar a executar).
// Els valors del postprocessat es poden retocar després a Assets/Settings/AspecteVisual.asset, i els del cel a Assets/Settings/Cel.mat
public static class VisualBuilder
{
    private const string SettingsFolder = "Assets/Settings";
    private const string ProfilePath = SettingsFolder + "/AspecteVisual.asset";
    private const string SkyPath = SettingsFolder + "/Cel.mat";
    private const string VolumeName = "AspecteVisual";
    private const string GameScenePath = "Assets/Scenes/Joc.unity";

    // Cel blau i horitzó clar. A sota de l'horitzó també és blau (abans era marró), perquè les illes semblin flotar enmig del cel.
    // La boira és del color de l'horitzó: les illes llunyanes s'hi fonen a poc a poc
    private static readonly Color SkyTint = new Color(0.46f, 0.56f, 0.72f);
    private static readonly Color SkyGround = new Color(0.6f, 0.72f, 0.86f);
    private static readonly Color FogColor = new Color(0.72f, 0.82f, 0.92f);
    private static readonly Color AmbientSky = new Color(0.62f, 0.72f, 0.86f);
    private static readonly Color AmbientEquator = new Color(0.56f, 0.62f, 0.62f);
    private static readonly Color AmbientGround = new Color(0.38f, 0.36f, 0.32f);
    private static readonly Color SunColor = new Color(1f, 0.95f, 0.85f);

    [MenuItem("Tools/TFG/Millorar l'aspecte visual")]
    public static void Build()
    {
        bool accept = EditorUtility.DisplayDialog("Millorar l'aspecte visual",
            "Es posarà a l'escena oberta:\n\n" +
            "• postprocessat: color, bloom i vinyeta\n" +
            "• un cel nou i boira a la llunyania\n" +
            "• llum ambiental més suau\n" +
            "• antialiàsing a la càmera\n\n" +
            "Després pots retocar els valors a Assets/Settings/AspecteVisual.asset i Assets/Settings/Cel.mat.", "Aplicar", "Cancel·lar");
        if (!accept) return;

        Apply();
        EditorUtility.DisplayDialog("Millorar l'aspecte visual", "Fet. Desa l'escena (Ctrl+S) i prova-ho amb Play.", "D'acord");
    }

    // La mateixa eina sense finestres, per fer-la servir des de la línia d'ordres amb Unity tancat:
    // Unity.exe -batchmode -quit -projectPath <carpeta del projecte> -executeMethod VisualBuilder.BuildFromCommandLine
    public static void BuildFromCommandLine()
    {
        EditorSceneManager.OpenScene(GameScenePath);
        Apply();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("[Aspecte visual] Aplicat i escena desada.");
    }

    public static void Apply()
    {
        if (!AssetDatabase.IsValidFolder(SettingsFolder)) AssetDatabase.CreateFolder("Assets", "Settings");

        VolumeProfile profile = CreateProfile();
        CreateGlobalVolume(profile);
        SetupSky();
        SetupFogAndAmbient();
        SetupSun();
        SetupCamera();

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    // --- Postprocessat ---

    private static VolumeProfile CreateProfile()
    {
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
        }

        // Neutral manté els colors del joc (ACES els torna més foscos i ataronjats)
        Tonemapping tonemapping = GetOrAdd<Tonemapping>(profile);
        tonemapping.mode.Override(TonemappingMode.Neutral);

        // Una mica més de contrast i de color: l'estil low poly llueix més amb colors vius
        ColorAdjustments color = GetOrAdd<ColorAdjustments>(profile);
        color.postExposure.Override(0.15f);
        color.contrast.Override(12f);
        color.saturation.Override(15f);

        // Un punt més càlid, com una tarda de sol
        WhiteBalance whiteBalance = GetOrAdd<WhiteBalance>(profile);
        whiteBalance.temperature.Override(6f);

        // Resplendor suau a les zones molt clares (el cel, el sol, els fils de teranyina a contrallum)
        Bloom bloom = GetOrAdd<Bloom>(profile);
        bloom.threshold.Override(0.95f);
        bloom.intensity.Override(0.35f);
        bloom.scatter.Override(0.65f);

        // Vores de la pantalla una mica més fosques, per centrar la mirada en l'aranya
        Vignette vignette = GetOrAdd<Vignette>(profile);
        vignette.intensity.Override(0.22f);
        vignette.smoothness.Override(0.45f);

        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (profile.TryGet(out T component)) return component;

        // Cada efecte es guarda dins de l'asset del perfil (com fa l'Inspector)
        component = profile.Add<T>(false);
        component.name = typeof(T).Name;
        component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
        AssetDatabase.AddObjectToAsset(component, profile);
        return component;
    }

    private static void CreateGlobalVolume(VolumeProfile profile)
    {
        GameObject volumeObject = null;
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == VolumeName) volumeObject = root;
        }
        if (volumeObject == null)
        {
            volumeObject = new GameObject(VolumeName);
            Undo.RegisterCreatedObjectUndo(volumeObject, "Millorar l'aspecte visual");
        }

        Volume volume = volumeObject.GetComponent<Volume>();
        if (volume == null) volume = Undo.AddComponent<Volume>(volumeObject);
        volume.isGlobal = true;
        volume.priority = 1f;
        volume.sharedProfile = profile;
        EditorUtility.SetDirty(volume);
    }

    // --- Cel, boira i llum ---

    private static void SetupSky()
    {
        Material sky = AssetDatabase.LoadAssetAtPath<Material>(SkyPath);
        if (sky == null)
        {
            sky = new Material(Shader.Find("Skybox/Procedural"));
            AssetDatabase.CreateAsset(sky, SkyPath);
        }

        sky.SetFloat("_SunDisk", 2f); // Sol de qualitat alta
        sky.SetFloat("_SunSize", 0.035f);
        sky.SetFloat("_SunSizeConvergence", 6f);
        sky.SetFloat("_AtmosphereThickness", 0.85f);
        sky.SetColor("_SkyTint", SkyTint);
        sky.SetColor("_GroundColor", SkyGround);
        sky.SetFloat("_Exposure", 1.25f);
        EditorUtility.SetDirty(sky);

        RenderSettings.skybox = sky;
    }

    private static void SetupFogAndAmbient()
    {
        // Boira lineal: res fins a 70 m i del tot a 400 m
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = FogColor;
        RenderSettings.fogStartDistance = 70f;
        RenderSettings.fogEndDistance = 400f;

        // Llum ambiental amb tres colors (cel, horitzó i terra): les ombres queden blavoses i no negres
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = AmbientSky;
        RenderSettings.ambientEquatorColor = AmbientEquator;
        RenderSettings.ambientGroundColor = AmbientGround;

        DynamicGI.UpdateEnvironment();
    }

    private static void SetupSun()
    {
        Light sun = RenderSettings.sun;
        if (sun == null)
        {
            foreach (Light light in Object.FindObjectsByType<Light>())
            {
                if (light.type == LightType.Directional) { sun = light; break; }
            }
        }
        if (sun == null) return;

        // No es toca la direcció del sol (la que ja tenia l'escena): només el color i la força
        Undo.RecordObject(sun, "Millorar l'aspecte visual");
        sun.color = SunColor;
        sun.intensity = 1.25f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.85f;
        RenderSettings.sun = sun;
        EditorUtility.SetDirty(sun);
    }

    private static void SetupCamera()
    {
        Camera camera = Camera.main;
        if (camera == null) return;

        UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
        Undo.RecordObject(data, "Millorar l'aspecte visual");
        data.renderPostProcessing = true; // Sense això, el postprocessat no es veu
        data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing; // Vores més suaus (SMAA)
        data.antialiasingQuality = AntialiasingQuality.High;
        EditorUtility.SetDirty(data);
    }
}
