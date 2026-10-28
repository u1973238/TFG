using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Eina de l'editor per muntar trencaclosques ràpidament: cada opció crea una peça ja preparada
// al punt on mira la vista de l'escena (Scene view). Menú: Tools > TFG > Peces de trencaclosques
public static class PuzzleBuilder
{
    private const string MenuRoot = "Tools/TFG/Peces de trencaclosques/";
    private const string MaterialFolder = "Assets/Materials/Puzle";

    [MenuItem(MenuRoot + "Porta amb condicions")]
    public static void CreateDoor()
    {
        GameObject door = CreateBlock("PortaPuzle", PrimitiveType.Cube, SpawnPoint() + Vector3.up * 2.5f, new Vector3(4f, 5f, 0.6f), "Porta", new Color(0.55f, 0.36f, 0.2f));
        PuzzleMover mover = door.AddComponent<PuzzleMover>();
        SetVector(mover, "openOffset", new Vector3(0f, -5.2f, 0f));
        Finish(door, "Arrossega palanques o plaques a la llista «Switches» del PuzzleMover.");
    }

    [MenuItem(MenuRoot + "Ascensor (plataforma que puja)")]
    public static void CreateElevator()
    {
        GameObject platform = CreateBlock("AscensorPuzle", PrimitiveType.Cube, SpawnPoint() + Vector3.up * 0.25f, new Vector3(4f, 0.5f, 4f), "Plataforma", new Color(0.45f, 0.5f, 0.55f));
        platform.layer = LayerMask.NameToLayer("Walkable");
        PuzzleMover mover = platform.AddComponent<PuzzleMover>();
        SetVector(mover, "openOffset", new Vector3(0f, 6f, 0f));
        Finish(platform, "Puja quan s'activen els interruptors de la llista «Switches».");
    }

    [MenuItem(MenuRoot + "Placa de pressió")]
    public static void CreatePressurePlate()
    {
        Vector3 position = SpawnPoint();
        GameObject root = new GameObject("PlacaDePressio");
        root.transform.position = position;

        // Zona que detecta el pes (la roca o l'aranya)
        BoxCollider trigger = root.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, 0.7f, 0f);
        trigger.size = new Vector3(2.4f, 1.4f, 2.4f);

        GameObject baseBlock = CreateBlock("Base", PrimitiveType.Cylinder, Vector3.zero, new Vector3(2.8f, 0.05f, 2.8f), "PlacaBase", new Color(0.3f, 0.3f, 0.3f));
        baseBlock.transform.SetParent(root.transform, false);
        GameObject plate = CreateBlock("Placa", PrimitiveType.Cylinder, new Vector3(0f, 0.12f, 0f), new Vector3(2.2f, 0.08f, 2.2f), "Placa", new Color(0.95f, 0.75f, 0.2f));
        plate.transform.SetParent(root.transform, false);
        plate.layer = LayerMask.NameToLayer("Walkable");

        PressurePlate pressurePlate = root.AddComponent<PressurePlate>();
        SetObject(pressurePlate, "plateVisual", plate.transform);
        Finish(root, "Desmarca «Player Counts» si vols que només hi compti una roca.");
    }

    [MenuItem(MenuRoot + "Roca mòbil")]
    public static void CreateRock()
    {
        InstantiatePrefab("Assets/Prefabs/Roca_Activation.prefab", SpawnPoint() + Vector3.up, "La roca es mou amb el Swing o el Grappling.");
    }

    [MenuItem(MenuRoot + "Palanca")]
    public static void CreateLever()
    {
        InstantiatePrefab("Assets/Prefabs/Palanca.prefab", SpawnPoint(), "Pots deixar «Door Object» buit i connectar la palanca a un PuzzleMover.");
    }

    [MenuItem(MenuRoot + "Mosca amb ruta")]
    public static void CreateFly()
    {
        Vector3 center = SpawnPoint() + Vector3.up * 3f;
        GameObject route = new GameObject("RutaMosca");
        route.transform.position = center;

        // Ruta quadrada de 4 punts: mou-los per decidir per on passa la mosca
        Vector3[] corners = { new Vector3(-3f, 0f, -3f), new Vector3(3f, 0.5f, -3f), new Vector3(3f, 0f, 3f), new Vector3(-3f, 0.5f, 3f) };
        Transform[] points = new Transform[corners.Length];
        for (int i = 0; i < corners.Length; i++)
        {
            GameObject point = new GameObject("Punt_" + (i + 1));
            point.transform.SetParent(route.transform, false);
            point.transform.localPosition = corners[i];
            points[i] = point.transform;
        }

        GameObject fly = new GameObject("Mosca");
        fly.transform.SetParent(route.transform, false);
        fly.transform.localPosition = corners[0];
        Fly flyComponent = fly.AddComponent<Fly>();

        SerializedObject serialized = new SerializedObject(flyComponent);
        SerializedProperty waypoints = serialized.FindProperty("waypoints");
        waypoints.arraySize = points.Length;
        for (int i = 0; i < points.Length; i++) waypoints.GetArrayElementAtIndex(i).objectReferenceValue = points[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();

        Finish(route, "Assigna una missió a la mosca (camp «Mission») perquè compti en atrapar-la.");
    }

    [MenuItem(MenuRoot + "Pals per al llit elàstic")]
    public static void CreateTrampolinePosts()
    {
        Vector3 position = SpawnPoint();
        GameObject root = new GameObject("PalsLlitElastic");
        root.transform.position = position;

        // Dos pals escalables: un pont de teranyina entre ells fa de llit elàstic si l'aranya hi cau des de prou amunt
        for (int i = 0; i < 2; i++)
        {
            GameObject post = CreateBlock("Pal_" + (i + 1), PrimitiveType.Cylinder, new Vector3(i == 0 ? -4f : 4f, 2f, 0f), new Vector3(0.6f, 2f, 0.6f), "Pal", new Color(0.6f, 0.45f, 0.3f));
            post.transform.SetParent(root.transform, false);
            post.layer = LayerMask.NameToLayer("Climbing");
        }
        Finish(root, "Fes un pont entre els dos pals i deixa't caure a sobre des d'una mica amunt.");
    }

    [MenuItem(MenuRoot + "Zona de rescat (animal atrapat)")]
    public static void CreateRescueZone()
    {
        Vector3 position = SpawnPoint();
        GameObject zone = new GameObject("ZonaRescat");
        zone.transform.position = position;

        // Volum invisible que ha de travessar el pont (el Rigidbody cinemàtic cal perquè detecti ponts estàtics)
        BoxCollider trigger = zone.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(6f, 3f, 2f);
        Rigidbody body = zone.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        GameObject safePoint = new GameObject("PuntSegur");
        safePoint.transform.SetParent(zone.transform, false);
        safePoint.transform.localPosition = new Vector3(0f, 0f, 6f);

        PlatformLinkTrigger link = zone.AddComponent<PlatformLinkTrigger>();
        SetObject(link, "safePoint", safePoint.transform);
        Finish(zone, "Posa la zona al buit entre les plataformes, arrossega l'animal a «Rescued Animal» i mou «PuntSegur» a l'altra banda. Suma 1 a «Required Amount» de la missió per cada zona.");
    }

    [MenuItem("Tools/TFG/Configurar circuit de cèrcols")]
    public static void SetupRingCircuit()
    {
        const string ringMissionID = "Cursa_aeria";

        // Busquem els cèrcols de l'escena (els col·leccionables de la missió dels cèrcols) i els ordenem pel nom
        System.Collections.Generic.List<MissionCollectible> rings = new System.Collections.Generic.List<MissionCollectible>();
        foreach (MissionCollectible collectible in Object.FindObjectsByType<MissionCollectible>(FindObjectsInactive.Include))
        {
            if (collectible.TargetMissionID == ringMissionID) rings.Add(collectible);
        }
        rings.Sort((a, b) => EditorUtility.NaturalCompare(a.name, b.name));

        MissionData ringMission = null;
        foreach (string guid in AssetDatabase.FindAssets("t:MissionData"))
        {
            MissionData candidate = AssetDatabase.LoadAssetAtPath<MissionData>(AssetDatabase.GUIDToAssetPath(guid));
            if (candidate != null && candidate.missionID == ringMissionID) ringMission = candidate;
        }

        if (rings.Count == 0 || ringMission == null)
        {
            EditorUtility.DisplayDialog("Circuit de cèrcols", $"No he trobat cèrcols ni la missió «{ringMissionID}» a l'escena oberta.", "D'acord");
            return;
        }

        RingCircuit circuit = Object.FindAnyObjectByType<RingCircuit>(FindObjectsInactive.Include);
        if (circuit == null)
        {
            GameObject circuitObject = new GameObject("CircuitDeCercols");
            Undo.RegisterCreatedObjectUndo(circuitObject, "Configurar circuit de cèrcols");
            circuit = circuitObject.AddComponent<RingCircuit>();
        }

        SerializedObject serialized = new SerializedObject(circuit);
        serialized.FindProperty("mission").objectReferenceValue = ringMission;
        SerializedProperty list = serialized.FindProperty("rings");
        list.arraySize = rings.Count;
        for (int i = 0; i < rings.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = rings[i];
        serialized.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(circuit.gameObject.scene);
        Selection.activeGameObject = circuit.gameObject;

        string order = string.Join("\n", rings.ConvertAll(r => "  " + (rings.IndexOf(r) + 1) + ". " + r.name));
        EditorUtility.DisplayDialog("Circuit de cèrcols",
            "Circuit configurat amb aquest ordre:\n" + order + "\n\nSi l'ordre no és el que vols, canvia'l a la llista «Rings» del RingCircuit. Desa l'escena (Ctrl+S).", "D'acord");
    }

    // --- Ajudes ---

    private static Vector3 SpawnPoint()
    {
        SceneView view = SceneView.lastActiveSceneView;
        return view != null ? view.pivot : Vector3.zero;
    }

    private static GameObject CreateBlock(string objectName, PrimitiveType type, Vector3 position, Vector3 scale, string materialName, Color color)
    {
        GameObject block = GameObject.CreatePrimitive(type);
        block.name = objectName;
        block.transform.position = position;
        block.transform.localScale = scale;
        block.GetComponent<Renderer>().sharedMaterial = GetMaterial(materialName, color);
        return block;
    }

    private static void InstantiatePrefab(string path, Vector3 position, string message)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            EditorUtility.DisplayDialog("Peces de trencaclosques", "No s'ha trobat " + path, "D'acord");
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, SceneManager.GetActiveScene());
        instance.transform.position = position;
        Finish(instance, message);
    }

    private static void Finish(GameObject created, string message)
    {
        Undo.RegisterCreatedObjectUndo(created, "Crear peça de trencaclosques");
        Selection.activeGameObject = created;
        EditorSceneManager.MarkSceneDirty(created.scene);
        Debug.Log($"[Trencaclosques] Creat «{created.name}». {message}", created);
    }

    // Materials de colors senzills per a les peces (es creen una vegada a Assets/Materials/Puzle)
    private static Material GetMaterial(string materialName, Color color)
    {
        string path = MaterialFolder + "/" + materialName + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;

        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets/Materials", "Puzle");

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        material = new Material(shader);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        material.color = color;
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void SetObject(Object target, string propertyName, Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(propertyName).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetVector(Object target, string propertyName, Vector3 value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(propertyName).vector3Value = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
