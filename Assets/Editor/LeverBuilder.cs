using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Eina de l'editor: converteix la palanca (abans un cilindre que s'inclinava, mig enterrat) en una palanca de veritat:
// una base de pedra, un mànec de fusta que gira per l'eix i un pom que passa de vermell a verd.
// Canvia el prefab Palanca (i, per tant, totes les palanques) i posa les palanques de l'escena oberta damunt del terra.
// Menú: Tools > TFG > Millorar les palanques
public static class LeverBuilder
{
    private const string PrefabPath = "Assets/Prefabs/Palanca.prefab";
    private const string MaterialFolder = "Assets/Materials/Palanca";
    private const int WalkableLayer = 3; // El fil de balancejar-se s'hi pot enganxar (per estirar la palanca des de lluny)

    [MenuItem("Tools/TFG/Millorar les palanques")]
    public static void BuildFromMenu()
    {
        bool ok = Build(out string summary);
        EditorUtility.DisplayDialog("Palanques", ok ? summary + "\n\nDesa l'escena (Ctrl+S)." : summary, "D'acord");
    }

    // Per fer-ho des de la línia d'ordres: Unity -batchmode -quit -executeMethod LeverBuilder.BuildFromCommandLine
    public static void BuildFromCommandLine()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Joc.unity");
        bool ok = Build(out string summary);
        Debug.Log("[Palanques] " + summary);
        if (ok) EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }

    public static bool Build(out string summary)
    {
        if (!UpgradePrefab(out summary)) return false;
        int placed = PlaceLeversOnGround(out int total);
        summary = $"Prefab «Palanca» actualitzat. Palanques de l'escena posades a terra: {placed} de {total}.";
        return true;
    }

    private static bool UpgradePrefab(out string error)
    {
        error = "";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
        {
            error = "No s'ha trobat " + PrefabPath;
            return false;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            LeverDoorSystem lever = root.GetComponent<LeverDoorSystem>();
            if (lever == null)
            {
                error = "El prefab Palanca no té el component LeverDoorSystem.";
                return false;
            }

            // L'arrel ja no es veu: només fa de zona (trigger) per tocar-la amb l'aranya o amb un pont de teranyina
            MeshRenderer oldRenderer = root.GetComponent<MeshRenderer>();
            if (oldRenderer != null) Object.DestroyImmediate(oldRenderer);
            MeshFilter oldFilter = root.GetComponent<MeshFilter>();
            if (oldFilter != null) Object.DestroyImmediate(oldFilter);

            CapsuleCollider zone = root.GetComponent<CapsuleCollider>();
            if (zone == null) zone = root.AddComponent<CapsuleCollider>();
            zone.isTrigger = true;
            zone.direction = 1;
            zone.center = new Vector3(0f, 1f, 0f);
            zone.height = 2.2f;
            zone.radius = 0.9f;

            Rigidbody body = root.GetComponent<Rigidbody>();
            if (body == null) body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            // Si ja s'havia millorat abans, es torna a fer de nou
            foreach (string oldPart in new[] { "Base", "Eix" })
            {
                Transform existing = root.transform.Find(oldPart);
                if (existing != null) Object.DestroyImmediate(existing.gameObject);
            }

            Material stone = GetMaterial("Palanca_Pedra", new Color(0.52f, 0.5f, 0.48f), false);
            Material wood = GetMaterial("Palanca_Fusta", new Color(0.45f, 0.29f, 0.16f), false);
            Material knobMaterial = GetMaterial("Palanca_Pom", new Color(0.8f, 0.12f, 0.08f), true);

            Part("Base", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.18f, 0f), new Vector3(0.9f, 0.36f, 0.7f), stone);

            // L'eix és el que gira: comença inclinat cap enrere i, en accionar-la, baixa cap endavant
            GameObject pivot = new GameObject("Eix");
            SceneManager.MoveGameObjectToScene(pivot, root.scene);
            pivot.layer = WalkableLayer;
            pivot.transform.SetParent(root.transform, false);
            pivot.transform.localPosition = new Vector3(0f, 0.36f, 0f);
            pivot.transform.localRotation = Quaternion.Euler(-35f, 0f, 0f);

            Part("Mànec", PrimitiveType.Cylinder, pivot.transform, new Vector3(0f, 0.65f, 0f), new Vector3(0.12f, 0.65f, 0.12f), wood);
            GameObject knob = Part("Pom", PrimitiveType.Sphere, pivot.transform, new Vector3(0f, 1.35f, 0f), Vector3.one * 0.4f, knobMaterial);

            // El col·lisionador del pom és una mica més gran que la bola: és on s'apunta per estirar-la amb el fil
            knob.GetComponent<SphereCollider>().radius = 0.8f;

            SerializedObject serialized = new SerializedObject(lever);
            serialized.FindProperty("leverHandle").objectReferenceValue = pivot.transform;
            serialized.FindProperty("stateLight").objectReferenceValue = knob.GetComponent<Renderer>();
            serialized.FindProperty("leverTargetRotation").floatValue = 70f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // Abans el cilindre s'estirava i quedava mig enterrat: ara cada palanca va a escala 1 i damunt del terra que té a sota
    private static int PlaceLeversOnGround(out int total)
    {
        int placed = 0;
        LeverDoorSystem[] levers = Object.FindObjectsByType<LeverDoorSystem>(FindObjectsInactive.Include);
        total = levers.Length;
        Physics.SyncTransforms();

        foreach (LeverDoorSystem lever in levers)
        {
            Transform t = lever.transform;
            Undo.RecordObject(t, "Millorar les palanques");

            if (PrefabUtility.IsPartOfPrefabInstance(t))
            {
                SerializedProperty scale = new SerializedObject(t).FindProperty("m_LocalScale");
                PrefabUtility.RevertPropertyOverride(scale, InteractionMode.AutomatedAction);
            }
            t.localScale = Vector3.one;

            // El terra més proper per sota (sense comptar la mateixa palanca)
            RaycastHit[] hits = Physics.RaycastAll(t.position + Vector3.up * 3f, Vector3.down, 20f, 1 << WalkableLayer, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            Vector3 ground = t.position;
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.transform.IsChildOf(t)) continue;
                if (hit.distance < best)
                {
                    best = hit.distance;
                    ground = hit.point;
                }
            }

            if (best < float.MaxValue)
            {
                t.position = ground;
                placed++;
            }
            EditorUtility.SetDirty(t);
        }

        if (total > 0) EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        return placed;
    }

    private static GameObject Part(string partName, PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        SceneManager.MoveGameObjectToScene(part, parent.gameObject.scene);
        part.name = partName;
        part.layer = WalkableLayer;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = localScale;
        part.GetComponent<Renderer>().sharedMaterial = material;
        return part;
    }

    // Materials de colors senzills (es creen una vegada a Assets/Materials/Palanca)
    private static Material GetMaterial(string materialName, Color color, bool emissive)
    {
        string path = MaterialFolder + "/" + materialName + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
            if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets/Materials", "Palanca");

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        material.color = color;
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.25f);
        if (emissive)
        {
            // El pom brilla una mica, perquè es vegi de lluny (el LeverDoorSystem el canvia de vermell a verd)
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            material.SetColor("_EmissionColor", color * 0.25f);
        }
        EditorUtility.SetDirty(material);
        return material;
    }
}
