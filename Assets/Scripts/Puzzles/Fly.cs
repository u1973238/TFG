using System.Collections.Generic;
using UnityEngine;

// Mosca per al trencaclosques «Atrapa les mosques»: vola seguint una ruta (o donant voltes per una zona)
// i, si travessa un pont de teranyina, s'hi queda enganxada i suma un punt a la missió.
// El jugador ha d'observar per on passen i teixir teranyines en el seu camí
public class Fly : MonoBehaviour
{
    [Header("Missió (opcional)")]
    [SerializeField] private MissionData mission;

    [Header("Vol")]
    [SerializeField] private List<Transform> waypoints = new List<Transform>(); // Ruta; si és buida, vola al voltant del punt inicial
    [SerializeField] private float speed = 3f;
    [SerializeField] private float wanderRadius = 4f;
    [SerializeField] private float buzzAmount = 0.25f; // Petits moviments irregulars, com una mosca de debò

    [Header("Aspecte")]
    [SerializeField] private float visualSize = 0.5f; // Mida de la mosca que es crea si no hi ha cap model

    private Rigidbody body;
    private int currentWaypoint = 0;
    private Vector3 wanderCenter;
    private Vector3 wanderTarget;
    private bool caught = false;
    private bool missionCounted = false;
    private Vector3 caughtPosition;
    private Transform wingLeft;
    private Transform wingRight;
    private AudioSource buzzSource;
    private float noiseSeed;

    void Awake()
    {
        // Rigidbody cinemàtic + col·lider Trigger: així detecta els ponts de teranyina (que són col·liders estàtics)
        body = GetComponent<Rigidbody>();
        if (body == null) body = gameObject.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;

        Collider trigger = GetComponent<Collider>();
        if (trigger == null)
        {
            SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
            sphere.radius = visualSize;
            trigger = sphere;
        }
        trigger.isTrigger = true;

        if (GetComponentInChildren<Renderer>() == null) BuildVisual();

        noiseSeed = Random.Range(0f, 100f);
        wanderCenter = transform.position;
        wanderTarget = PickWanderTarget();
    }

    void Start()
    {
        // Brunzit 3D: se sent més fort com més a prop
        buzzSource = gameObject.AddComponent<AudioSource>();
        buzzSource.clip = AudioManager.GetClip("buzz");
        buzzSource.loop = true;
        buzzSource.spatialBlend = 1f;
        buzzSource.minDistance = 1.5f;
        buzzSource.maxDistance = 15f;
        buzzSource.volume = 0.5f * GameSettings.EffectsVolume;
        buzzSource.pitch = Random.Range(0.9f, 1.1f);
        if (buzzSource.clip != null) buzzSource.Play();
    }

    void Update()
    {
        // Ales: bateguen molt de pressa mentre vola i s'arronsen quan està enganxada
        float flap = caught ? Mathf.Sin(Time.time * 15f) * 10f : Mathf.Sin(Time.time * 60f) * 35f;
        if (wingLeft != null) wingLeft.localRotation = Quaternion.Euler(0f, 0f, 20f + flap);
        if (wingRight != null) wingRight.localRotation = Quaternion.Euler(0f, 0f, -20f - flap);

        // Si s'ha enganxat abans d'activar la missió, ho anem provant fins que el gestor accepti el punt
        if (caught && !missionCounted && mission != null && MissionManager.Instance != null)
        {
            missionCounted = MissionManager.Instance.ProgressMission(mission, 1);
        }
    }

    void FixedUpdate()
    {
        if (caught)
        {
            // Enganxada: només es remena una mica
            body.MovePosition(caughtPosition + Random.insideUnitSphere * 0.03f);
            return;
        }

        Vector3 target = waypoints.Count > 0 && waypoints[currentWaypoint] != null ? waypoints[currentWaypoint].position : wanderTarget;
        Vector3 next = Vector3.MoveTowards(body.position, target, speed * Time.fixedDeltaTime);

        // Moviment irregular amb soroll de Perlin
        float t = Time.time * 2f + noiseSeed;
        Vector3 buzz = new Vector3(Mathf.PerlinNoise(t, 0f) - 0.5f, Mathf.PerlinNoise(0f, t) - 0.5f, Mathf.PerlinNoise(t, t) - 0.5f) * buzzAmount;
        body.MovePosition(next + buzz * Time.fixedDeltaTime * 10f);

        Vector3 direction = target - body.position;
        if (direction.sqrMagnitude > 0.01f)
        {
            body.MoveRotation(Quaternion.Slerp(body.rotation, Quaternion.LookRotation(direction), Time.fixedDeltaTime * 6f));
        }

        if (Vector3.Distance(body.position, target) < 0.3f)
        {
            if (waypoints.Count > 0) currentWaypoint = (currentWaypoint + 1) % waypoints.Count;
            else wanderTarget = PickWanderTarget();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (caught || other.GetComponentInParent<WebBridge>() == null) return;

        // Ha topat amb una teranyina: s'hi queda enganxada
        caught = true;
        caughtPosition = body.position;
        AudioManager.Play("fly_caught");
        Effects.Puff(caughtPosition, Vector3.up, new Color(1f, 1f, 1f, 0.8f), 10, 0.3f, 1.5f);
        if (buzzSource != null) buzzSource.pitch *= 1.3f;
    }

    private Vector3 PickWanderTarget()
    {
        return wanderCenter + Random.insideUnitSphere * wanderRadius;
    }

    // Mosca senzilla de figures bàsiques (estil low poly): cos negre, ulls vermells i dues ales transparents
    private void BuildVisual()
    {
        GameObject visual = new GameObject("Mosca_Model");
        visual.transform.SetParent(transform, false);

        CreatePart(PrimitiveType.Sphere, visual.transform, Vector3.zero, new Vector3(0.7f, 0.6f, 1f) * visualSize, new Color(0.08f, 0.08f, 0.08f));
        CreatePart(PrimitiveType.Sphere, visual.transform, new Vector3(-0.15f, 0.1f, 0.42f) * visualSize, Vector3.one * 0.28f * visualSize, new Color(0.7f, 0.1f, 0.1f));
        CreatePart(PrimitiveType.Sphere, visual.transform, new Vector3(0.15f, 0.1f, 0.42f) * visualSize, Vector3.one * 0.28f * visualSize, new Color(0.7f, 0.1f, 0.1f));

        wingLeft = CreateWing(visual.transform, -1f);
        wingRight = CreateWing(visual.transform, 1f);
    }

    private Transform CreateWing(Transform parent, float side)
    {
        // L'ala gira des d'un punt a la part de dalt del cos
        GameObject pivot = new GameObject(side < 0 ? "Ala_Esquerra" : "Ala_Dreta");
        pivot.transform.SetParent(parent, false);
        pivot.transform.localPosition = new Vector3(side * 0.15f, 0.25f, 0f) * visualSize;

        GameObject wing = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(wing.GetComponent<Collider>());
        wing.transform.SetParent(pivot.transform, false);
        wing.transform.localPosition = new Vector3(side * 0.35f, 0f, -0.1f) * visualSize;
        wing.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        wing.transform.localScale = new Vector3(0.7f, 0.4f, 1f) * visualSize;

        // Material semitransparent (el mateix shader que els fils, que es veu per les dues cares)
        Renderer wingRenderer = wing.GetComponent<Renderer>();
        Material wingMaterial = new Material(WebConnector.GetWebMaterial());
        wingMaterial.color = new Color(0.85f, 0.95f, 1f, 0.45f);
        wingRenderer.material = wingMaterial;
        return pivot.transform;
    }

    private void CreatePart(PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Color color)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        Destroy(part.GetComponent<Collider>());
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().material.color = color;
    }
}
