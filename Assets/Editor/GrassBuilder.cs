using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Posa herba a les illes: crea l'objecte «Herba» amb el component GrassField i el material del shader TFG/Herba.
// L'herba es genera sola cada vegada (no es desa a l'escena). Menú: Tools > TFG > Posar herba
public static class GrassBuilder
{
    private const string MaterialPath = "Assets/Materials/Herba/Herba.mat";

    [MenuItem("Tools/TFG/Posar herba")]
    public static void AddFromMenu()
    {
        GrassField field = Add();
        if (field == null)
        {
            EditorUtility.DisplayDialog("Herba", "No s'ha trobat el shader TFG/Herba (Assets/Shaders/Herba.shader).", "D'acord");
            return;
        }

        Selection.activeGameObject = field.gameObject;
        string grounds = field.grassMaterials.Count > 0 ? string.Join(", ", field.grassMaterials.ConvertAll(m => m != null ? m.name : "?")) : "qualsevol material verd";
        EditorUtility.DisplayDialog("Herba",
            $"S'han posat {field.TuftCount} mates d'herba ({field.BladeCount} fulles), damunt de: {grounds}.\n\n" +
            "Per canviar la quantitat, l'alçada o els colors, selecciona l'objecte «Herba» (el vent es canvia al material Herba). " +
            "Desa l'escena (Ctrl+S).", "D'acord");
    }

    // Per fer-ho des de la línia d'ordres: Unity -batchmode -quit -executeMethod GrassBuilder.BuildFromCommandLine
    public static void BuildFromCommandLine()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Joc.unity");
        GrassField field = Add();
        Debug.Log(field != null ? $"[Herba] {field.TuftCount} mates, {field.BladeCount} fulles, {field.ChunkCount} trossos" : "[Herba] No s'ha trobat el shader");
        if (field != null) EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }

    public static GrassField Add()
    {
        Material material = GetMaterial();
        if (material == null) return null;

        GrassField field = Object.FindAnyObjectByType<GrassField>(FindObjectsInactive.Include);
        if (field == null)
        {
            GameObject grass = new GameObject("Herba");
            Undo.RegisterCreatedObjectUndo(grass, "Posar herba");
            field = grass.AddComponent<GrassField>();
        }

        field.material = material;
        if (field.grassMaterials.Count == 0) field.grassMaterials.AddRange(FindIslandGrassMaterials(field.groundLayers));
        field.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        field.transform.localScale = Vector3.one;
        field.Generate();

        EditorUtility.SetDirty(field);
        EditorSceneManager.MarkSceneDirty(field.gameObject.scene);
        return field;
    }

    // Els materials verds de les illes (el model Island): la gespa. Així no creix herba a les fulles de l'arbre, que també són verdes
    private static System.Collections.Generic.List<Material> FindIslandGrassMaterials(LayerMask groundLayers)
    {
        System.Collections.Generic.List<Material> found = new System.Collections.Generic.List<Material>();
        foreach (Collider ground in Object.FindObjectsByType<Collider>())
        {
            if (ground.isTrigger || (groundLayers.value & (1 << ground.gameObject.layer)) == 0) continue;
            if (!ground.name.StartsWith("Island")) continue;

            Renderer groundRenderer = ground.GetComponent<Renderer>();
            if (groundRenderer == null) continue;
            foreach (Material candidate in groundRenderer.sharedMaterials)
            {
                if (candidate == null || found.Contains(candidate)) continue;
                Color color = candidate.HasProperty("_BaseColor") ? candidate.GetColor("_BaseColor") : candidate.color;
                if (color.g > 0.12f && color.g > color.r * 1.1f && color.g > color.b * 1.1f) found.Add(candidate);
            }
        }
        return found;
    }

    private static Material GetMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material != null) return material;

        Shader shader = Shader.Find("TFG/Herba");
        if (shader == null) return null;

        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Materials/Herba")) AssetDatabase.CreateFolder("Assets/Materials", "Herba");
        material = new Material(shader) { name = "Herba" };
        AssetDatabase.CreateAsset(material, MaterialPath);
        return material;
    }
}
