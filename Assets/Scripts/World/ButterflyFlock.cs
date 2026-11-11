using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Papallones que volen per les clapes de flors de l'herba (GrassField). Van de flor en flor fent ziga-zagues,
// s'hi posen i hi descansen una estona (les ales s'obren i es tanquen a poc a poc, i es gronxen amb la flor quan fa vent).
// Quan l'aranya s'hi acosta, s'espanten i se'n van volant. Només decoren: no es poden caçar (les mosques de les missions són una altra cosa).
// Es creen soles en començar el joc. Fan servir el material de l'herba: amb les UV a zero, el shader no les doblega com si fossin fulles
[RequireComponent(typeof(GrassField))]
public class ButterflyFlock : MonoBehaviour
{
    [Header("Quantes")]
    [Tooltip("Hi ha una papallona per cada tantes flors")]
    public int flowersPerButterfly = 15;
    public int maxButterflies = 40;
    public int seed = 3;

    [Header("Com són")]
    public Vector2 wingspan = new Vector2(0.55f, 0.85f); // Envergadura: mínima i màxima (metres)
    public Color[] wingColors =
    {
        new Color(1f, 0.55f, 0.12f), // Taronja
        new Color(0.25f, 0.5f, 1f), // Blau
        new Color(0.95f, 0.95f, 0.9f), // Blanca
        new Color(1f, 0.45f, 0.7f), // Rosa
        new Color(0.6f, 0.35f, 0.9f), // Lila
    };
    public Color bodyColor = new Color(0.12f, 0.1f, 0.08f);

    [Header("Com volen")]
    public float flySpeed = 2.2f; // Metres per segon
    public Vector2 restTime = new Vector2(2f, 6f); // Segons que descansen a cada flor: mínim i màxim
    public float hopDistance = 10f; // Fins on van a buscar la flor següent (metres)
    public float scareDistance = 3.5f; // Si l'aranya s'acosta més que això, s'espanten
    public float activeDistance = 55f; // Més lluny de la càmera no es mouen ni es dibuixen (l'herba ja no s'hi veu)

    private enum State { Flying, Resting }

    private class Butterfly
    {
        public Transform Root;
        public Transform LeftWing;
        public Transform RightWing;
        public Renderer[] Renderers;
        public State State;
        public Vector3 Position;
        public Vector3 Velocity;
        public Vector3 Target; // On va quan fuig (quan no va a cap flor)
        public int Flower = -1; // La flor on va o on descansa (-1 = cap: està fugint)
        public float GroundY; // El terra de la darrera flor: no vola per sota
        public float Timer;
        public float Phase;
        public float Yaw;
        public float NoiseSeed;
        public bool Visible = true;
    }

    private GrassField field;
    private readonly List<Butterfly> butterflies = new List<Butterfly>();
    private readonly List<Mesh> meshes = new List<Mesh>();
    private readonly List<int> candidates = new List<int>();
    private Mesh bodyMesh;
    private Mesh[] leftWingMeshes;
    private Mesh[] rightWingMeshes;
    private int spawnedGeneration = -1;
    private Transform spider;
    private System.Random random;

    // El vent del material de l'herba, per gronxar-se amb la flor
    private float windStrength;
    private float windSpeed;
    private float windScale;
    private Vector3 windDirection;

    public int Count => butterflies.Count;
    public int Landings { get; private set; } // Quantes vegades s'han posat en una flor
    public int Escapes { get; private set; } // Quantes vegades s'han espantat per l'aranya

    void OnDisable()
    {
        Clear();
        spawnedGeneration = -1;
    }

    void Update()
    {
        if (field == null) field = GetComponent<GrassField>();
        if (field.Generation != spawnedGeneration) Spawn();
        if (butterflies.Count == 0) return;

        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f) return; // Joc en pausa: es queden quietes

        if (spider == null)
        {
            SpiderLocomotion locomotion = FindAnyObjectByType<SpiderLocomotion>();
            if (locomotion != null) spider = locomotion.transform;
        }

        Camera cam = Camera.main;
        Vector3 cameraPosition = cam != null ? cam.transform.position : transform.position;
        ReadWind();

        foreach (Butterfly butterfly in butterflies)
        {
            bool visible = (butterfly.Position - cameraPosition).sqrMagnitude < activeDistance * activeDistance;
            if (visible != butterfly.Visible)
            {
                butterfly.Visible = visible;
                foreach (Renderer part in butterfly.Renderers) part.enabled = visible;
            }
            if (!visible) continue;

            Think(butterfly, deltaTime);
            Pose(butterfly, deltaTime);
        }
    }

    // --- Què fa cada papallona ---

    private void Think(Butterfly butterfly, float deltaTime)
    {
        bool scared = spider != null && (spider.position - butterfly.Position).sqrMagnitude < scareDistance * scareDistance;

        if (butterfly.State == State.Resting)
        {
            butterfly.Timer -= deltaTime;
            butterfly.Position = Landing(butterfly.Flower); // Es gronxa amb la flor
            if (scared) Flee(butterfly);
            else if (butterfly.Timer <= 0f) FlyTo(butterfly, NextFlower(butterfly, false));
            return;
        }

        if (scared && butterfly.Flower >= 0) Flee(butterfly);

        // Vol de papallona: va cap on vol anar fent ziga-zagues (soroll de Perlin), i a prop de la flor es calma
        Vector3 target = butterfly.Flower >= 0 ? Landing(butterfly.Flower) : butterfly.Target;
        Vector3 toTarget = target - butterfly.Position;
        float distance = toTarget.magnitude;
        float time = Time.time * 1.3f + butterfly.NoiseSeed;
        Vector3 wobble = new Vector3(
            Mathf.PerlinNoise(time, butterfly.NoiseSeed) - 0.5f,
            (Mathf.PerlinNoise(butterfly.NoiseSeed, time) - 0.5f) * 0.7f,
            Mathf.PerlinNoise(time + 17.3f, butterfly.NoiseSeed + 5.1f) - 0.5f) * 2f;
        float approach = Mathf.Clamp01(distance / 1.5f);
        Vector3 desired = (distance > 0.001f ? toTarget / distance : Vector3.zero) * flySpeed * Mathf.Lerp(0.35f, 1f, approach)
                        + wobble * (flySpeed * 0.7f * approach);
        butterfly.Velocity = Vector3.Lerp(butterfly.Velocity, desired, 1f - Mathf.Exp(-3f * deltaTime));
        butterfly.Position += butterfly.Velocity * deltaTime;

        // No vola per sota de l'herba
        float minY = butterfly.GroundY + 0.5f;
        if (butterfly.Position.y < minY && (butterfly.Flower < 0 || distance > 0.5f))
        {
            butterfly.Position.y = minY;
            if (butterfly.Velocity.y < 0f) butterfly.Velocity.y = 0f;
        }

        if (butterfly.Flower >= 0)
        {
            if (distance < 0.12f) Land(butterfly);
        }
        else
        {
            // Ha fugit prou: busca una flor lluny de l'aranya
            butterfly.Timer -= deltaTime;
            if (butterfly.Timer <= 0f) FlyTo(butterfly, NextFlower(butterfly, true));
        }
    }

    private void Land(Butterfly butterfly)
    {
        butterfly.State = State.Resting;
        butterfly.Timer = Mathf.Lerp(restTime.x, restTime.y, Next());
        butterfly.Velocity = Vector3.zero;
        Landings++;
    }

    private void FlyTo(Butterfly butterfly, int flower)
    {
        butterfly.State = State.Flying;
        butterfly.Flower = flower;
        if (flower >= 0) butterfly.GroundY = field.FlowerPositions[flower].y - field.FlowerHeights[flower];
        butterfly.Velocity += Vector3.up * 0.8f; // S'enlaira
    }

    private void Flee(Butterfly butterfly)
    {
        Vector3 away = butterfly.Position - spider.position;
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f) away = Quaternion.Euler(0f, Next() * 360f, 0f) * Vector3.forward;
        away.Normalize();

        butterfly.State = State.Flying;
        butterfly.Flower = -1;
        butterfly.Target = butterfly.Position + away * 6f + Vector3.up * Mathf.Lerp(2.5f, 4f, Next());
        butterfly.Timer = Mathf.Lerp(2f, 3.5f, Next());
        butterfly.Velocity += away * 1.5f + Vector3.up * 1.5f; // S'aixeca de cop
        Escapes++;
    }

    // Una flor a l'abast (i, si fuig, lluny de l'aranya). Si no n'hi ha cap a l'abast, la més propera
    private int NextFlower(Butterfly butterfly, bool awayFromSpider)
    {
        IReadOnlyList<Vector3> flowers = field.FlowerPositions;
        float safe = scareDistance * 2f;
        candidates.Clear();
        int nearest = -1;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < flowers.Count; i++)
        {
            if (i == butterfly.Flower) continue;
            if (awayFromSpider && spider != null && (flowers[i] - spider.position).sqrMagnitude < safe * safe) continue;

            float distance = (flowers[i] - butterfly.Position).sqrMagnitude;
            if (distance < hopDistance * hopDistance) candidates.Add(i);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = i;
            }
        }
        return candidates.Count > 0 ? candidates[random.Next(candidates.Count)] : nearest;
    }

    // On es posa la papallona: damunt de la flor, que es mou amb el vent
    private Vector3 Landing(int flower)
    {
        Vector3 head = field.FlowerPositions[flower];
        float height = field.FlowerHeights[flower];
        return head + WindOffset(head, height) + Vector3.up * 0.1f;
    }

    // El mateix vent que fa servir el shader de l'herba (BendGrass) per a la punta d'una planta
    private Vector3 WindOffset(Vector3 head, float height)
    {
        Vector3 root = head - Vector3.up * height;
        float phase = Time.timeSinceLevelLoad * windSpeed + (root.x + root.z * 0.7f) * windScale;
        float gust = Mathf.Sin(phase) * 0.65f + Mathf.Sin(phase * 2.17f + root.x * 0.9f) * 0.35f;
        return windDirection * ((gust * 0.5f + 0.5f) * windStrength * height);
    }

    private void ReadWind()
    {
        Material grass = field.material;
        if (grass == null) return;
        windStrength = grass.HasProperty("_WindStrength") ? grass.GetFloat("_WindStrength") : 0f;
        windSpeed = grass.HasProperty("_WindSpeed") ? grass.GetFloat("_WindSpeed") : 0f;
        windScale = grass.HasProperty("_WindScale") ? grass.GetFloat("_WindScale") : 0f;
        Vector4 direction = grass.HasProperty("_WindDirection") ? grass.GetVector("_WindDirection") : Vector4.zero;
        windDirection = new Vector3(direction.x, 0f, direction.z).normalized;
    }

    // Posició, orientació i aleteig
    private void Pose(Butterfly butterfly, float deltaTime)
    {
        bool flying = butterfly.State == State.Flying;
        butterfly.Phase += deltaTime * (flying ? 9f : 1.1f) * Mathf.PI * 2f;

        // Angle de les ales (0 = esteses, 90 = tancades amunt): volant baten ràpid; descansant, s'obren i es tanquen a poc a poc
        float angle = flying ? 15f + 55f * Mathf.Sin(butterfly.Phase) : 50f + 30f * Mathf.Sin(butterfly.Phase);

        Vector3 position = butterfly.Position;
        if (flying) position.y += Mathf.Sin(butterfly.Phase) * 0.03f; // A cada cop d'ala s'aixeca una mica

        // Mira cap on vola i s'inclina segons si puja o baixa
        Vector3 horizontal = new Vector3(butterfly.Velocity.x, 0f, butterfly.Velocity.z);
        if (flying && horizontal.sqrMagnitude > 0.01f)
        {
            float heading = Mathf.Atan2(horizontal.x, horizontal.z) * Mathf.Rad2Deg;
            butterfly.Yaw = Mathf.LerpAngle(butterfly.Yaw, heading, 1f - Mathf.Exp(-6f * deltaTime));
        }
        float pitch = flying ? Mathf.Clamp(-butterfly.Velocity.y * 8f, -25f, 25f) : 0f;

        butterfly.Root.SetPositionAndRotation(position, Quaternion.Euler(pitch, butterfly.Yaw, 0f));
        butterfly.LeftWing.localRotation = Quaternion.Euler(0f, 0f, -angle);
        butterfly.RightWing.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    // --- Crear les papallones ---

    private void Spawn()
    {
        Clear();
        spawnedGeneration = field.Generation;
        if (field.material == null || field.FlowerCount == 0 || flowersPerButterfly <= 0 || wingColors == null || wingColors.Length == 0) return;

        random = new System.Random(seed);
        BuildMeshes();
        Landings = 0;
        Escapes = 0;

        int count = Mathf.Min(maxButterflies, field.FlowerCount / flowersPerButterfly);
        for (int i = 0; i < count; i++)
        {
            Butterfly butterfly = Create(random.Next(wingColors.Length), Mathf.Lerp(wingspan.x, wingspan.y, Next()));
            butterfly.Flower = random.Next(field.FlowerCount);
            butterfly.GroundY = field.FlowerPositions[butterfly.Flower].y - field.FlowerHeights[butterfly.Flower];
            butterfly.Position = Landing(butterfly.Flower);
            butterfly.Yaw = Next() * 360f;
            butterfly.Phase = Next() * 10f;
            butterfly.NoiseSeed = Next() * 100f;

            // Al principi, unes descansen i les altres arriben volant
            if (Next() < 0.5f)
            {
                butterfly.State = State.Resting;
                butterfly.Timer = Mathf.Lerp(0f, restTime.y, Next());
            }
            else
            {
                butterfly.State = State.Flying;
                butterfly.Position += Quaternion.Euler(0f, Next() * 360f, 0f) * Vector3.forward * Mathf.Lerp(2f, 5f, Next()) + Vector3.up;
            }
            butterflies.Add(butterfly);
        }
    }

    private Butterfly Create(int colorIndex, float size)
    {
        GameObject root = new GameObject("Papallona") { hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave };
        root.transform.SetParent(transform, false);
        root.transform.localScale = Vector3.one * size; // Les malles fan 1 metre d'envergadura

        GameObject left = new GameObject("AlaEsquerra") { hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave };
        left.transform.SetParent(root.transform, false);
        GameObject right = new GameObject("AlaDreta") { hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave };
        right.transform.SetParent(root.transform, false);

        return new Butterfly
        {
            Root = root.transform,
            LeftWing = left.transform,
            RightWing = right.transform,
            Renderers = new[] { AddMesh(root, bodyMesh), AddMesh(left, leftWingMeshes[colorIndex]), AddMesh(right, rightWingMeshes[colorIndex]) },
        };
    }

    private Renderer AddMesh(GameObject target, Mesh mesh)
    {
        target.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer part = target.AddComponent<MeshRenderer>();
        part.sharedMaterial = field.material;
        part.shadowCastingMode = ShadowCastingMode.Off;
        part.receiveShadows = true;
        part.lightProbeUsage = LightProbeUsage.Off;
        part.reflectionProbeUsage = ReflectionProbeUsage.Off;
        return part;
    }

    private void Clear()
    {
        foreach (Butterfly butterfly in butterflies)
        {
            if (butterfly.Root != null) Destroy(butterfly.Root.gameObject);
        }
        butterflies.Clear();
        foreach (Mesh mesh in meshes) if (mesh != null) Destroy(mesh);
        meshes.Clear();
    }

    private float Next() => (float)random.NextDouble();

    // Malles d'1 metre d'envergadura. El cos va de davant (z+) a darrere; cada ala surt cap a un costat i gira per l'eix del cos
    private void BuildMeshes()
    {
        Color body = bodyColor;
        bodyMesh = NewMesh("Cos papallona");
        List<Vector3> vertices = new List<Vector3>();
        List<Color> colors = new List<Color>();

        // Cos: un rombe allargat, i dues antenes primes
        Vector3 head = new Vector3(0f, 0f, 0.16f), tail = new Vector3(0f, 0f, -0.24f);
        Vector3[] ring = { new Vector3(0.035f, 0f, 0f), new Vector3(0f, 0.035f, 0f), new Vector3(-0.035f, 0f, 0f), new Vector3(0f, -0.035f, 0f) };
        for (int i = 0; i < 4; i++)
        {
            Triangle(vertices, colors, head, ring[i], ring[(i + 1) % 4], body, body, body);
            Triangle(vertices, colors, tail, ring[(i + 1) % 4], ring[i], body, body, body);
        }
        for (int side = -1; side <= 1; side += 2)
        {
            Vector3 start = new Vector3(0f, 0.01f, 0.14f);
            Triangle(vertices, colors, start, start + new Vector3(side * 0.008f, 0f, 0f), new Vector3(side * 0.07f, 0.07f, 0.3f), body, body, body);
        }
        Fill(bodyMesh, vertices, colors);

        leftWingMeshes = new Mesh[wingColors.Length];
        rightWingMeshes = new Mesh[wingColors.Length];
        for (int c = 0; c < wingColors.Length; c++)
        {
            leftWingMeshes[c] = WingMesh(wingColors[c], -1f);
            rightWingMeshes[c] = WingMesh(wingColors[c], 1f);
        }
    }

    // Una ala: la de davant (gran, amb la punta fosca) i la de darrere (més petita)
    private Mesh WingMesh(Color color, float side)
    {
        Mesh mesh = NewMesh(side > 0f ? "Ala dreta" : "Ala esquerra");
        List<Vector3> vertices = new List<Vector3>();
        List<Color> colors = new List<Color>();
        Vector3 S(float x, float z) => new Vector3(x * side, 0f, z);

        Color inner = color * 0.55f;
        inner.a = 1f;
        Color tip = Color.Lerp(color, Color.black, 0.65f);
        Color hind = color * 0.85f;
        hind.a = 1f;

        // Ala de davant
        Triangle(vertices, colors, S(0.02f, 0.08f), S(0.47f, 0.24f), S(0.43f, -0.04f), inner, tip, color);
        Triangle(vertices, colors, S(0.02f, 0.08f), S(0.43f, -0.04f), S(0.02f, -0.05f), inner, color, inner);
        // Ala de darrere
        Triangle(vertices, colors, S(0.02f, -0.03f), S(0.32f, -0.12f), S(0.18f, -0.34f), inner, color, hind);
        Triangle(vertices, colors, S(0.02f, -0.03f), S(0.18f, -0.34f), S(0.02f, -0.2f), inner, hind, inner);

        Fill(mesh, vertices, colors);
        return mesh;
    }

    private Mesh NewMesh(string meshName)
    {
        Mesh mesh = new Mesh { name = meshName, hideFlags = HideFlags.DontSave };
        meshes.Add(mesh);
        return mesh;
    }

    private static void Triangle(List<Vector3> vertices, List<Color> colors, Vector3 a, Vector3 b, Vector3 c, Color colorA, Color colorB, Color colorC)
    {
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        colors.Add(colorA);
        colors.Add(colorB);
        colors.Add(colorC);
    }

    private static void Fill(Mesh mesh, List<Vector3> vertices, List<Color> colors)
    {
        int[] triangles = new int[vertices.Count];
        Vector3[] normals = new Vector3[vertices.Count];
        Vector2[] uvs = new Vector2[vertices.Count]; // Zero: el shader de l'herba no les doblega
        for (int i = 0; i < vertices.Count; i++)
        {
            triangles[i] = i;
            normals[i] = Vector3.up;
        }
        mesh.SetVertices(vertices);
        mesh.SetColors(colors);
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }
}
