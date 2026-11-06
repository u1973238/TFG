using System.Collections;
using UnityEngine;

// Zona de rescat: un volum invisible al buit entre dues plataformes. Quan un pont de teranyina el travessa,
// l'animal atrapat queda connectat i suma +1 a la missió. Es poden posar tantes zones com animals atrapats
// (posa a la missió «Required Amount» = nombre de zones). Si hi ha un animal assignat, camina fins al pont,
// el creua per sobre dels fils i baixa a l'altra banda
public class PlatformLinkTrigger : MonoBehaviour
{
    [Header("Configuración de la Misión")]
    [SerializeField] private string missionID = "conectar_plataformas";

    [Header("Animal rescatat (opcional)")]
    [SerializeField] private Transform rescuedAnimal; // L'animal atrapat
    [SerializeField] private Transform safePoint; // On va després de creuar el pont (si es deixa buit, s'atura una mica més enllà del pont)
    [SerializeField] private float walkSpeed = 2.5f;
    [SerializeField] private float hopHeight = 0.3f;
    [SerializeField] private float stepLength = 0.6f; // Distància que avança a cada saltet
    [SerializeField] private float facingOffset = 0f; // Si l'animal camina de costat o d'esquena, gira aquest angle (graus) per corregir-ho
    [SerializeField] private float stopNearNPCDistance = 3f; // Després del pont, si passa a prop d'un NPC s'hi atura al costat i el mira (0 = no)

    private bool isConnected = false;
    private bool bridgeDetected = false;
    private WebBridge bridge; // El pont que l'ha rescatat: l'animal hi camina per sobre

    // Per a la fletxa de l'objectiu
    public string MissionID => missionID;
    public bool IsRescued => isConnected;
    public Transform RescuedAnimal => rescuedAnimal;

    void Update()
    {
        // Si ja hi ha un pont però la missió encara no estava activa, ho tornem a intentar fins que el gestor accepti el progrés
        if (bridgeDetected && !isConnected && MissionManager.Instance != null)
        {
            isConnected = MissionManager.Instance.ProgressMission(missionID, 1);
            if (isConnected) OnRescued();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Deteció si es una trenyina
        if (other.gameObject.layer == LayerMask.NameToLayer("Climbing"))
        {
            bridgeDetected = true;
            if (bridge == null) bridge = other.GetComponentInParent<WebBridge>();
        }
    }

    private void OnRescued()
    {
        if (MissionManager.Instance != null) MissionManager.Instance.ShowNotification("Animal rescatat!");
        if (rescuedAnimal != null) StartCoroutine(RescueWalk());
    }

    // --- L'animal creua el pont ---

    private IEnumerator RescueWalk()
    {
        Transform animal = rescuedAnimal;

        // Si l'animal és un NPC, mentre camina el gir i el balanceig els fa aquest script
        NPCAnimator npcAnimator = animal.GetComponent<NPCAnimator>();
        if (npcAnimator != null) npcAnimator.enabled = false;

        Quaternion baseRotation = animal.rotation;
        float feet = HeightAboveGround(animal);

        // Un saltet d'alegria abans de començar a caminar
        yield return HopInPlace(animal, 0.4f, hopHeight * 2f);

        if (bridge != null)
        {
            // Va fins a l'extrem del pont que té més a prop, el creua i baixa a l'altra banda
            bool fromStart = FlatDistance(animal.position, bridge.StartPoint) <= FlatDistance(animal.position, bridge.EndPoint);
            Vector3 entry = fromStart ? bridge.StartPoint : bridge.EndPoint;
            Vector3 exit = fromStart ? bridge.EndPoint : bridge.StartPoint;

            yield return WalkOnGround(animal, entry, feet, baseRotation, true, false);
            yield return WalkOnBridge(animal, bridge, fromStart, entry, exit, feet, baseRotation);

            Vector3 beyond = Flat(exit - entry).normalized * 3f;
            Vector3 destination = safePoint != null ? safePoint.position : exit + beyond;
            yield return WalkOnGround(animal, destination, feet, baseRotation, false, true);
            yield return Land(animal, feet);
        }
        else if (safePoint != null)
        {
            yield return WalkOnGround(animal, safePoint.position, feet, baseRotation, false, true);
            yield return Land(animal, feet);
        }

        // Si s'ha aturat al costat d'un NPC (per exemple, el que demanava ajuda), es gira cap a ell
        if (friend != null) yield return TurnTowards(animal, friend.position, baseRotation, 0.4f);

        Effects.Puff(animal.position, Vector3.up, new Color(1f, 0.85f, 0.3f, 0.9f), 12, 0.3f, 2f);
        AudioManager.Play("collect");
        yield return HopInPlace(animal, 0.4f, hopHeight * 2f);

        if (npcAnimator != null)
        {
            npcAnimator.RecaptureBase();
            npcAnimator.enabled = true;
        }
    }

    private Transform friend; // L'NPC al costat del qual s'ha aturat, si n'hi ha

    // Camina per terra fins a un punt, seguint el relleu. Si el punt és l'extrem d'un pont (que pot ser a la paret
    // d'un precipici), els últims metres baixa o puja suaument fins a l'altura del pont.
    // Si «stopNearNPC», s'atura abans si arriba al costat d'un NPC (perquè no se li posi a sobre)
    private IEnumerator WalkOnGround(Transform animal, Vector3 target, float feet, Quaternion baseRotation, bool reachTargetHeight, bool stopNearNPC)
    {
        const float blend = 1.5f;
        Vector3 start = animal.position;
        Vector3 flatPath = Flat(target - start);
        float length = flatPath.magnitude;
        if (length < 0.05f) yield break;

        MissionNPC[] npcs = stopNearNPC && stopNearNPCDistance > 0f ? FindObjectsByType<MissionNPC>() : new MissionNPC[0];

        float traveled = 0f;
        float lastGround = start.y - feet;
        while (traveled < length)
        {
            float before = traveled;
            traveled = Mathf.Min(length, traveled + walkSpeed * Time.deltaTime);
            float t = traveled / length;
            Vector3 point = start + flatPath * t;

            foreach (MissionNPC npc in npcs)
            {
                if (npc.transform.IsChildOf(animal) || animal.IsChildOf(npc.transform)) continue;
                if (FlatDistance(point, npc.transform.position) < stopNearNPCDistance)
                {
                    friend = npc.transform;
                    yield break;
                }
            }

            // Altura: la del terra (el més proper a on ja era); al principi continua des d'on era, i al final arriba a l'altura del pont
            if (GroundHeight(animal, point, lastGround, out float ground)) lastGround = ground;
            float height = lastGround + feet;
            height = Mathf.Lerp(height, start.y, 1f - Mathf.Clamp01(traveled / blend));
            if (reachTargetHeight) height = Mathf.Lerp(height, target.y + feet, 1f - Mathf.Clamp01((length - traveled) / blend));

            point.y = height;
            Step(animal, point, before, traveled, flatPath, baseRotation, null);
            yield return null;
        }
    }

    // Camina per sobre del pont, seguint la corba dels fils i fent-los tremolar a cada pas
    private IEnumerator WalkOnBridge(Transform animal, WebBridge path, bool fromStart, Vector3 entry, Vector3 exit, float feet, Quaternion baseRotation)
    {
        float length = Vector3.Distance(entry, exit);
        float traveled = 0f;
        while (traveled < length)
        {
            float before = traveled;
            traveled = Mathf.Min(length, traveled + walkSpeed * Time.deltaTime);
            float t = traveled / length;

            // Si el pont desaparegués mentre el creua, continua en línia recta
            Vector3 point = path != null ? path.PointAt(fromStart ? t : 1f - t) : Vector3.Lerp(entry, exit, t);
            point.y += feet;
            Step(animal, point, before, traveled, exit - entry, baseRotation, path);
            yield return null;
        }
    }

    // Col·loca l'animal en un punt del camí amb l'animació de caminar: saltets, balanceig de costat a costat i girat cap on va
    private void Step(Transform animal, Vector3 point, float before, float traveled, Vector3 direction, Quaternion baseRotation, WebBridge onBridge)
    {
        float phase = traveled / stepLength * Mathf.PI;

        // Cada vegada que toca a terra (o als fils): so de passa i, si és al pont, el fa tremolar
        if (Mathf.FloorToInt(traveled / stepLength) != Mathf.FloorToInt(before / stepLength))
        {
            AudioManager.Play("step", 0.3f, 0.1f);
            if (onBridge != null) onBridge.Wobble(0.12f);
        }

        animal.position = point + Vector3.up * Mathf.Abs(Mathf.Sin(phase)) * hopHeight;

        Vector3 flatDirection = Flat(direction);
        if (flatDirection.sqrMagnitude < 0.0001f) return;
        flatDirection.Normalize();

        Vector3 baseForward = Flat(Quaternion.Euler(0f, facingOffset, 0f) * (baseRotation * Vector3.forward));
        float yaw = baseForward.sqrMagnitude > 0.0001f ? Vector3.SignedAngle(baseForward, flatDirection, Vector3.up) : 0f;
        Quaternion facing = Quaternion.AngleAxis(yaw, Vector3.up) * baseRotation;
        Quaternion waddle = Quaternion.AngleAxis(Mathf.Sin(phase) * 6f, flatDirection);
        animal.rotation = Quaternion.Slerp(animal.rotation, waddle * facing, Time.deltaTime * 10f);
    }

    // En acabar de caminar, posa els peus a terra (no es queda aturat a mig saltet)
    private IEnumerator Land(Transform animal, float feet)
    {
        if (!GroundHeight(animal, animal.position, animal.position.y - feet - hopHeight, out float ground)) yield break;

        Vector3 from = animal.position;
        Vector3 to = new Vector3(from.x, ground + feet, from.z);
        float elapsed = 0f;
        while (elapsed < 0.12f)
        {
            elapsed += Time.deltaTime;
            animal.position = Vector3.Lerp(from, to, elapsed / 0.12f);
            yield return null;
        }
        animal.position = to;
    }

    private IEnumerator TurnTowards(Transform animal, Vector3 target, Quaternion baseRotation, float duration)
    {
        Vector3 direction = Flat(target - animal.position);
        Vector3 baseForward = Flat(Quaternion.Euler(0f, facingOffset, 0f) * (baseRotation * Vector3.forward));
        if (direction.sqrMagnitude < 0.0001f || baseForward.sqrMagnitude < 0.0001f) yield break;

        Quaternion from = animal.rotation;
        Quaternion to = Quaternion.AngleAxis(Vector3.SignedAngle(baseForward, direction, Vector3.up), Vector3.up) * baseRotation;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            animal.rotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / duration));
            yield return null;
        }
    }

    private IEnumerator HopInPlace(Transform animal, float duration, float height)
    {
        Vector3 start = animal.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            animal.position = start + Vector3.up * Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI) * height;
            yield return null;
        }
        animal.position = start;
    }

    // --- Ajudes ---

    // Busca el terra sota un punt: de totes les superfícies que hi ha a sota i a sobre, la més propera a l'altura on ja era
    // (així no puja de cop a la copa d'un arbre). S'ignoren les zones invisibles, el mateix animal i l'aranya
    private bool GroundHeight(Transform animal, Vector3 point, float expectedY, out float ground)
    {
        ground = 0f;
        float best = float.MaxValue;
        Vector3 origin = new Vector3(point.x, expectedY + 10f, point.z);
        foreach (RaycastHit hit in Physics.RaycastAll(origin, Vector3.down, 25f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.IsChildOf(animal) || hit.collider.CompareTag("Player")) continue;
            float difference = Mathf.Abs(hit.point.y - expectedY);
            if (difference >= best) continue;
            best = difference;
            ground = hit.point.y;
        }
        return best < float.MaxValue;
    }

    private float HeightAboveGround(Transform animal)
    {
        return GroundHeight(animal, animal.position, animal.position.y, out float ground) ? Mathf.Clamp(animal.position.y - ground, 0f, 3f) : 0f;
    }

    private static Vector3 Flat(Vector3 vector)
    {
        vector.y = 0f;
        return vector;
    }

    private static float FlatDistance(Vector3 a, Vector3 b) => Flat(a - b).magnitude;

    private void OnDrawGizmosSelected()
    {
        // A l'editor es veu cap on anirà l'animal
        if (rescuedAnimal == null || safePoint == null) return;
        Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.8f);
        Gizmos.DrawLine(rescuedAnimal.position, safePoint.position);
        Gizmos.DrawWireSphere(safePoint.position, 0.5f);
    }
}
