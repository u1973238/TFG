using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Herba procedural: mates de fulles que creixen damunt de les illes, només a les parts verdes i planes.
// Es genera sola en obrir l'escena i en començar el joc, i no es desa a l'escena (així no ocupa espai al projecte).
// Amb el shader TFG/Herba es mou amb el vent, s'aparta quan hi passa l'aranya i de lluny s'amaga suaument.
// Per posar-la: Tools > TFG > Posar herba
[ExecuteAlways]
public class GrassField : MonoBehaviour
{
    private const float ChunkSize = 24f; // L'herba es divideix en trossos d'aquesta mida (els que no es veuen no es dibuixen)
    private const float BuriedDepth = 0.06f; // La base de cada fulla queda una mica enterrada, perquè no floti als pendents

    [Header("On creix")]
    [Tooltip("Capes del terra on pot créixer")]
    public LayerMask groundLayers = (1 << 3) | (1 << 6); // Walkable i Climbing
    [Tooltip("Només creix damunt dels materials de la llista (la gespa de les illes), no a la terra, les roques ni l'arbre")]
    public bool onlyOnGreen = true;
    [Tooltip("Materials del terra on creix l'herba. Si la llista és buida, creix a qualsevol material verd (compte: les fulles dels arbres també ho són)")]
    public List<Material> grassMaterials = new List<Material>();
    [Range(0f, 1f)] public float minUpward = 0.8f; // Com de pla ha de ser el terra (1 = del tot pla)
    [Tooltip("Al voltant dels NPCs, cubs, palanques i plaques no hi creix, perquè es vegin bé")]
    public float clearRadius = 2.5f;

    [Header("Quantitat")]
    [Tooltip("Distància entre mates (metres). Més petit = més herba, però l'ordinador treballa més")]
    public float spacing = 1f;
    [Range(0f, 1f)] public float coverage = 0.72f; // Quina part del terra té herba (la resta són clarianes)
    public float patchSize = 14f; // Mida de les clapes d'herba (metres)
    public int seed = 7;

    [Header("Mates")]
    public Vector2 bladeHeight = new Vector2(0.45f, 1.1f); // Alçada de les fulles: mínima i màxima (metres)
    [Range(0f, 1f)] public float tallChance = 0.08f; // Algunes mates són més altes que la resta
    public float tallMultiplier = 1.8f;
    public Vector2Int bladesPerTuft = new Vector2Int(4, 7);
    public float bladeWidth = 0.13f;
    public float tuftRadius = 0.25f;

    [Header("Colors (la base agafa el verd del terra)")]
    public Color tipColor = new Color(0.48f, 0.74f, 0.2f);
    public Color dryTipColor = new Color(0.74f, 0.7f, 0.34f);
    [Range(0f, 1f)] public float dryChance = 0.12f;
    public Color fallbackGroundColor = new Color(0.3f, 0.55f, 0.15f); // Si no es pot saber el color del terra

    [Header("Distància")]
    public float fadeStart = 50f; // A partir d'aquí l'herba s'encongeix...
    public float fadeEnd = 75f;   // ...i aquí ja no es veu (la boira comença als 70 m)

    [Header("Aranya")]
    public float pushRadius = 1.6f; // L'herba s'aparta en aquest radi al voltant de l'aranya

    public Material material;

    private readonly List<GameObject> chunks = new List<GameObject>();
    private readonly List<Mesh> meshes = new List<Mesh>();
    private readonly List<Renderer> chunkRenderers = new List<Renderer>();
    private readonly Dictionary<Collider, GroundInfo> groundInfo = new Dictionary<Collider, GroundInfo>();
    private Transform spider;
    private bool started;
    private bool generated;

    public int TuftCount { get; private set; }
    public int BladeCount { get; private set; }
    public int ChunkCount => chunks.Count;
    public float LastGenerationMs { get; private set; }

    void OnEnable()
    {
        SetGlobals(false);
        if (Application.isPlaying)
        {
            if (started) Generate();
        }
#if UNITY_EDITOR
        else
        {
            ScheduleGenerate();
        }
#endif
    }

    void Start()
    {
        // Al joc, es genera quan tota l'escena ja està carregada (les illes ja tenen els col·lisionadors a punt)
        started = true;
        if (Application.isPlaying) Generate();
    }

    void OnDisable()
    {
        Clear();
        Shader.SetGlobalVector("_HerbaPusher", Vector4.zero);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // En canviar algun valor a l'inspector, es torna a generar
        if (generated && isActiveAndEnabled) ScheduleGenerate();
    }

    private bool generatePending;

    private void ScheduleGenerate()
    {
        if (generatePending) return;
        generatePending = true;
        UnityEditor.EditorApplication.delayCall += () =>
        {
            generatePending = false;
            if (this != null && isActiveAndEnabled) Generate();
        };
    }
#endif

    void Update()
    {
        SetGlobals(Application.isPlaying);
        if (!Application.isPlaying) return;

        // Els trossos massa lluny de la càmera no es dibuixen (l'herba ja s'hi ha amagat del tot)
        Camera cam = Camera.main;
        if (cam == null) return;
        Vector3 cameraPosition = cam.transform.position;
        float maxDistance = (fadeEnd + 5f) * (fadeEnd + 5f);
        foreach (Renderer chunkRenderer in chunkRenderers)
        {
            if (chunkRenderer != null) chunkRenderer.enabled = chunkRenderer.bounds.SqrDistance(cameraPosition) < maxDistance;
        }
    }

    // Valors que llegeix el shader: fins on es veu l'herba i on és l'aranya (perquè l'aparti)
    private void SetGlobals(bool withSpider)
    {
        Shader.SetGlobalVector("_HerbaFade", new Vector4(fadeStart, fadeEnd, 0f, 0f));

        Vector4 pusher = Vector4.zero;
        if (withSpider)
        {
            if (spider == null)
            {
                SpiderLocomotion locomotion = FindAnyObjectByType<SpiderLocomotion>();
                if (locomotion != null) spider = locomotion.transform;
            }
            if (spider != null) pusher = new Vector4(spider.position.x, spider.position.y, spider.position.z, pushRadius);
        }
        Shader.SetGlobalVector("_HerbaPusher", pusher);
    }

    // --- Generar l'herba ---

    // A l'editor, si has mogut un NPC, un cub o una palanca, fes clic dret al component > «Tornar a generar»
    // (al joc es torna a generar sempre en començar)
    [ContextMenu("Tornar a generar")]
    public void Generate()
    {
        Clear();
        generated = true;
        if (material == null) return;

        System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
        Physics.SyncTransforms();
        List<Vector3> clearPoints = FindClearPoints();
        Dictionary<Vector2Int, GrassMeshBuilder> builders = new Dictionary<Vector2Int, GrassMeshBuilder>();
        RaycastHit[] hits = new RaycastHit[16];
        TuftCount = 0;
        BladeCount = 0;

        foreach (Collider ground in FindGrounds())
        {
            Bounds bounds = ground.bounds;
            int minX = Mathf.FloorToInt(bounds.min.x / spacing), maxX = Mathf.CeilToInt(bounds.max.x / spacing);
            int minZ = Mathf.FloorToInt(bounds.min.z / spacing), maxZ = Mathf.CeilToInt(bounds.max.z / spacing);

            for (int ix = minX; ix <= maxX; ix++)
            {
                for (int iz = minZ; iz <= maxZ; iz++)
                {
                    // Cada punt de la quadrícula té els seus números aleatoris (sempre els mateixos): l'herba surt igual cada vegada
                    TuftRandom random = new TuftRandom(ix, iz, seed);
                    float x = (ix + random.Next() - 0.5f) * spacing;
                    float z = (iz + random.Next() - 0.5f) * spacing;

                    // Clapes: on el soroll és baix hi ha clarianes, amb una vora gradual
                    float noise = Mathf.PerlinNoise(x / patchSize + seed * 0.37f, z / patchSize + seed * 0.71f);
                    float chance = Mathf.InverseLerp(1f - coverage - 0.12f, 1f - coverage + 0.12f, noise);
                    if (random.Next() > chance) continue;

                    if (!FindGround(ground, x, z, bounds, hits, out RaycastHit hit)) continue;
                    if (hit.normal.y < minUpward) continue;
                    if (!GroundColor(hit, out Color groundColor) && onlyOnGreen) continue;
                    if (IsNearClearPoint(hit.point, clearPoints)) continue;

                    Vector2Int cell = new Vector2Int(Mathf.FloorToInt(hit.point.x / ChunkSize), Mathf.FloorToInt(hit.point.z / ChunkSize));
                    if (!builders.TryGetValue(cell, out GrassMeshBuilder builder)) builders[cell] = builder = new GrassMeshBuilder();
                    AddTuft(builder, hit.point, hit.normal, groundColor, ref random);
                }
            }
        }

        foreach (KeyValuePair<Vector2Int, GrassMeshBuilder> pair in builders) CreateChunk(pair.Key, pair.Value);
        groundInfo.Clear();
        LastGenerationMs = (float)watch.Elapsed.TotalMilliseconds;
    }

    private void Clear()
    {
        foreach (GameObject chunk in chunks) if (chunk != null) DestroySafe(chunk);
        foreach (Mesh mesh in meshes) if (mesh != null) DestroySafe(mesh);
        chunks.Clear();
        meshes.Clear();
        chunkRenderers.Clear();

        // Restes d'una generació anterior (per exemple, després de recompilar els scripts)
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if ((child.hideFlags & HideFlags.DontSave) != 0) DestroySafe(child);
        }
    }

    private static void DestroySafe(Object target)
    {
        if (Application.isPlaying) Destroy(target);
        else DestroyImmediate(target);
    }

    // Terres candidats: col·lisionadors sòlids de les capes del terra que no es mouen
    private List<Collider> FindGrounds()
    {
        List<Collider> grounds = new List<Collider>();
        foreach (Collider candidate in FindObjectsByType<Collider>())
        {
            if (!candidate.enabled || candidate.isTrigger) continue;
            if ((groundLayers.value & (1 << candidate.gameObject.layer)) == 0) continue;
            if (candidate.attachedRigidbody != null && !candidate.attachedRigidbody.isKinematic) continue;
            grounds.Add(candidate);
        }
        return grounds;
    }

    // Llança un raig des de dalt: el primer que toca ha de ser aquest terra (si hi ha una roca o l'arbre a sobre, aquí no hi creix)
    private static bool FindGround(Collider ground, float x, float z, Bounds bounds, RaycastHit[] hits, out RaycastHit result)
    {
        result = default;
        Vector3 from = new Vector3(x, bounds.max.y + 1f, z);
        int count = Physics.RaycastNonAlloc(from, Vector3.down, hits, bounds.size.y + 2f, ~0, QueryTriggerInteraction.Ignore);

        float best = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            Collider other = hits[i].collider;
            bool isSpider = other.CompareTag("Player") || (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player"));
            if (isSpider || hits[i].distance >= best) continue;
            best = hits[i].distance;
            result = hits[i];
        }
        return best < float.MaxValue && result.collider == ground;
    }

    // Mira el material del terra just en aquell punt (les illes en tenen un per a la gespa i un altre per a la terra):
    // diu si hi pot créixer herba i de quin verd ha de ser la base de les fulles
    private bool GroundColor(RaycastHit hit, out Color color)
    {
        color = fallbackGroundColor;
        if (!groundInfo.TryGetValue(hit.collider, out GroundInfo info))
        {
            info = new GroundInfo(hit.collider);
            groundInfo[hit.collider] = info;
        }

        Material groundMaterial = info.MaterialAt(hit.triangleIndex);
        if (groundMaterial == null) return false;

        Color materialColor = groundMaterial.HasProperty("_BaseColor") ? groundMaterial.GetColor("_BaseColor") : groundMaterial.color;
        bool isGrass = grassMaterials.Count > 0
            ? grassMaterials.Contains(groundMaterial)
            : materialColor.g > 0.12f && materialColor.g > materialColor.r * 1.1f && materialColor.g > materialColor.b * 1.1f;
        if (isGrass) color = materialColor;
        return isGrass;
    }

    // Llocs on no hi ha d'haver herba alta: on hi ha coses que el jugador ha de veure
    private static List<Vector3> FindClearPoints()
    {
        List<Vector3> points = new List<Vector3>();
        foreach (MissionNPC npc in FindObjectsByType<MissionNPC>()) points.Add(npc.transform.position);
        foreach (MissionCollectible item in FindObjectsByType<MissionCollectible>()) points.Add(item.transform.position);
        foreach (LeverDoorSystem lever in FindObjectsByType<LeverDoorSystem>()) points.Add(lever.transform.position);
        foreach (PressurePlate plate in FindObjectsByType<PressurePlate>()) points.Add(plate.transform.position);
        foreach (PlatformLinkTrigger rescue in FindObjectsByType<PlatformLinkTrigger>())
        {
            if (rescue.RescuedAnimal != null) points.Add(rescue.RescuedAnimal.position);
        }
        return points;
    }

    private bool IsNearClearPoint(Vector3 point, List<Vector3> clearPoints)
    {
        foreach (Vector3 clear in clearPoints)
        {
            float dx = point.x - clear.x, dz = point.z - clear.z;
            if (dx * dx + dz * dz < clearRadius * clearRadius && Mathf.Abs(point.y - clear.y) < 4f) return true;
        }
        return false;
    }

    // Una mata: unes quantes fulles (triangles) que surten del mateix lloc cap a direccions diferents
    private void AddTuft(GrassMeshBuilder builder, Vector3 root, Vector3 normal, Color groundColor, ref TuftRandom random)
    {
        float height = Mathf.Lerp(bladeHeight.x, bladeHeight.y, random.Next());
        if (random.Next() < tallChance) height *= tallMultiplier;
        int blades = Mathf.RoundToInt(Mathf.Lerp(bladesPerTuft.x, bladesPerTuft.y, random.Next()));

        // La base és del verd del terra (una mica més fosc) i la punta més clara; algunes mates són més seques
        float shade = Mathf.Lerp(0.88f, 1.08f, random.Next());
        Color tip = random.Next() < dryChance
            ? Color.Lerp(tipColor, dryTipColor, 0.75f)
            : Color.Lerp(tipColor, groundColor, random.Next() * 0.35f);
        tip *= shade;
        tip.a = 1f;
        Color baseColor = groundColor * (0.82f * shade);
        baseColor.a = 1f;

        float firstYaw = random.Next() * 360f;
        for (int i = 0; i < blades; i++)
        {
            float yaw = firstYaw + i * 360f / blades + (random.Next() - 0.5f) * 40f;
            Vector3 direction = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward; // Cap on s'inclina la fulla
            Vector3 side = new Vector3(direction.z, 0f, -direction.x);
            Vector3 start = root + direction * (random.Next() * tuftRadius) - Vector3.up * BuriedDepth;
            float bladeHeightNow = height * Mathf.Lerp(0.6f, 1f, random.Next());
            float lean = Mathf.Lerp(0.1f, 0.45f, random.Next()) * bladeHeightNow;
            float width = bladeWidth * Mathf.Lerp(0.7f, 1.25f, random.Next());

            Vector3 tipPosition = start + direction * lean + Vector3.up * (bladeHeightNow + BuriedDepth);
            builder.AddBlade(start - side * (width * 0.5f), start + side * (width * 0.5f), tipPosition, normal, baseColor, tip, bladeHeightNow + BuriedDepth);
        }

        TuftCount++;
        BladeCount += blades;
    }

    private void CreateChunk(Vector2Int cell, GrassMeshBuilder builder)
    {
        // Els vèrtexs es guarden en l'espai d'aquest objecte (per si es mou)
        for (int i = 0; i < builder.Vertices.Count; i++)
        {
            builder.Vertices[i] = transform.InverseTransformPoint(builder.Vertices[i]);
            builder.Normals[i] = transform.InverseTransformDirection(builder.Normals[i]);
        }

        Mesh mesh = new Mesh { name = "Herba " + cell.x + "," + cell.y, hideFlags = HideFlags.HideAndDontSave };
        if (builder.Vertices.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
        mesh.SetVertices(builder.Vertices);
        mesh.SetNormals(builder.Normals);
        mesh.SetColors(builder.Colors);
        mesh.SetUVs(0, builder.Uvs);
        mesh.SetTriangles(builder.Triangles, 0);
        mesh.RecalculateBounds();

        // Les fulles es mouen (vent i aranya): els límits són una mica més grans perquè no desapareguin a les vores de la pantalla
        Bounds bounds = mesh.bounds;
        bounds.Expand(new Vector3(2f, 1f, 2f));
        mesh.bounds = bounds;

        GameObject chunk = new GameObject(mesh.name) { hideFlags = HideFlags.HideAndDontSave };
        chunk.layer = gameObject.layer;
        chunk.transform.SetParent(transform, false);
        chunk.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer chunkRenderer = chunk.AddComponent<MeshRenderer>();
        chunkRenderer.sharedMaterial = material;
        chunkRenderer.shadowCastingMode = ShadowCastingMode.Off;
        chunkRenderer.receiveShadows = true;
        chunkRenderer.lightProbeUsage = LightProbeUsage.Off;
        chunkRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

        chunks.Add(chunk);
        meshes.Add(mesh);
        chunkRenderers.Add(chunkRenderer);
    }

    // --- Ajudes ---

    // Números aleatoris que depenen només del punt de la quadrícula (així l'herba no canvia d'una vegada a l'altra)
    private struct TuftRandom
    {
        private uint state;

        public TuftRandom(int x, int z, int seed)
        {
            unchecked
            {
                state = (uint)(x * 73856093) ^ (uint)(z * 19349663) ^ (uint)(seed * 83492791) ^ 0x9E3779B9u;
                if (state == 0) state = 1;
                Next();
                Next();
            }
        }

        public float Next()
        {
            // Xorshift: ràpid i suficient per repartir herba
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return (state & 0xFFFFFF) / (float)0x1000000;
        }
    }

    // Quin material hi ha a cada triangle d'un terra
    private class GroundInfo
    {
        private readonly Material[] materials;
        private readonly int[] subMeshStarts;
        private readonly int[] subMeshEnds;

        public GroundInfo(Collider collider)
        {
            Renderer groundRenderer = collider.GetComponent<Renderer>();
            if (groundRenderer == null) groundRenderer = collider.GetComponentInParent<Renderer>();
            materials = groundRenderer != null ? groundRenderer.sharedMaterials : new Material[0];

            // Només es pot saber el material de cada triangle si el col·lisionador fa servir la mateixa malla que es veu
            MeshCollider meshCollider = collider as MeshCollider;
            MeshFilter filter = groundRenderer != null ? groundRenderer.GetComponent<MeshFilter>() : null;
            if (materials.Length > 1 && meshCollider != null && filter != null && meshCollider.sharedMesh == filter.sharedMesh && meshCollider.sharedMesh != null)
            {
                Mesh mesh = meshCollider.sharedMesh;
                subMeshStarts = new int[mesh.subMeshCount];
                subMeshEnds = new int[mesh.subMeshCount];
                for (int i = 0; i < mesh.subMeshCount; i++)
                {
                    SubMeshDescriptor descriptor = mesh.GetSubMesh(i);
                    subMeshStarts[i] = descriptor.indexStart;
                    subMeshEnds[i] = descriptor.indexStart + descriptor.indexCount;
                }
            }
        }

        public Material MaterialAt(int triangleIndex)
        {
            if (materials.Length == 0) return null;
            if (materials.Length == 1) return materials[0];
            if (subMeshStarts == null || triangleIndex < 0) return null;

            int index = triangleIndex * 3;
            for (int i = 0; i < subMeshStarts.Length; i++)
            {
                if (index >= subMeshStarts[i] && index < subMeshEnds[i]) return i < materials.Length ? materials[i] : null;
            }
            return null;
        }
    }

    private class GrassMeshBuilder
    {
        public readonly List<Vector3> Vertices = new List<Vector3>();
        public readonly List<Vector3> Normals = new List<Vector3>();
        public readonly List<Color32> Colors = new List<Color32>();
        public readonly List<Vector2> Uvs = new List<Vector2>();
        public readonly List<int> Triangles = new List<int>();

        // Una fulla és un triangle: dos vèrtexs a la base i un a la punta. A les UV: x = alçada de la fulla, y = 0 a la base i 1 a la punta
        public void AddBlade(Vector3 left, Vector3 right, Vector3 tip, Vector3 normal, Color baseColor, Color tipColor, float height)
        {
            int first = Vertices.Count;
            Vertices.Add(left);
            Vertices.Add(right);
            Vertices.Add(tip);
            Normals.Add(normal);
            Normals.Add(normal);
            Normals.Add(normal);
            Colors.Add(baseColor);
            Colors.Add(baseColor);
            Colors.Add(tipColor);
            Uvs.Add(new Vector2(height, 0f));
            Uvs.Add(new Vector2(height, 0f));
            Uvs.Add(new Vector2(height, 1f));
            Triangles.Add(first);
            Triangles.Add(first + 1);
            Triangles.Add(first + 2);
        }
    }
}
