using System.Collections.Generic;
using UnityEngine;

// Mou un objecte (porta, ascensor, pont llevadís...) entre la posició inicial i una posició "oberta"
// quan s'activen els interruptors connectats (palanques, plaques de pressió o qualsevol IPuzzleSwitch).
// Exemple de trencaclosques: una porta que només s'obre si hi ha una roca a cada una de les dues plaques
public class PuzzleMover : MonoBehaviour
{
    public enum Condition { AllSwitches, AnySwitch }

    [Header("Interruptors")]
    [Tooltip("Palanques, plaques de pressió o qualsevol component que implementi IPuzzleSwitch")]
    [SerializeField] private List<MonoBehaviour> switches = new List<MonoBehaviour>();
    [SerializeField] private Condition condition = Condition.AllSwitches;
    [SerializeField] private bool stayOpenOnceOpened = false; // Si és cert, quan s'obre ja no es torna a tancar (trencaclosques resolt)

    [Header("Moviment")]
    [SerializeField] private Transform target; // Si es deixa buit, es mou aquest mateix objecte
    [SerializeField] private Vector3 openOffset = new Vector3(0f, -5f, 0f); // Desplaçament en coordenades del món
    [SerializeField] private Vector3 openRotation = Vector3.zero; // Gir extra (graus) quan està obert, per a ponts llevadissos
    [SerializeField] private float openDuration = 1.5f; // Segons que tarda a obrir-se del tot (en un ascensor, millor 3 o 4 perquè l'aranya no surti disparada)
    [SerializeField] private float closeDuration = 2f; // Segons que tarda a tornar a tancar-se

    [Header("Missió (opcional)")]
    [SerializeField] private MissionData missionOnOpen; // Suma 1 a aquesta missió la primera vegada que s'obre

    private Vector3 closedPosition;
    private Quaternion closedRotation;
    private Rigidbody body;
    private float openAmount = 0f; // 0 = tancat, 1 = obert
    private bool isOpen = false;
    private bool latched = false;
    private bool missionDone = false;

    public bool IsOpen => isOpen;
    public MissionData MissionOnOpen => missionOnOpen;
    public Transform MovedObject => target != null ? target : transform; // El que es mou (per ensenyar-lo amb la càmera)
    public bool HasSwitch(MonoBehaviour candidate) => switches.Contains(candidate);

    void Start()
    {
        if (target == null) target = transform;
        closedPosition = target.position;
        closedRotation = target.rotation;

        // L'objecte es mou amb un Rigidbody cinemàtic: així la física sap que es mou i porta l'aranya o les roques que hi ha a sobre.
        // Si es mogués canviant només la posició, la física els empenyeria de cop (i podrien sortir disparats o travessar-lo)
        body = target.GetComponent<Rigidbody>();
        if (body == null)
        {
            body = target.gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
        }
        if (body.isKinematic) body.interpolation = RigidbodyInterpolation.Interpolate;

        foreach (MonoBehaviour candidate in switches)
        {
            if (candidate != null && !(candidate is IPuzzleSwitch))
            {
                Debug.LogWarning($"[PuzzleMover] {candidate.name} no és un interruptor (IPuzzleSwitch) i s'ignorarà.", this);
            }
        }
    }

    void Update()
    {
        bool shouldOpen = latched || ConditionMet();

        if (shouldOpen != isOpen)
        {
            isOpen = shouldOpen;
            AudioManager.Play("door");

            if (isOpen && stayOpenOnceOpened) latched = true;

            // La primera vegada que s'obre, avisem la missió (si n'hi ha) i ho tornem a provar fins que l'accepti
            if (isOpen && missionOnOpen != null) missionDone = false;
        }

        if (isOpen && missionOnOpen != null && !missionDone && MissionManager.Instance != null)
        {
            missionDone = MissionManager.Instance.ProgressMission(missionOnOpen, 1);
        }
    }

    void FixedUpdate()
    {
        // Moviment suau cap a la posició oberta o tancada (a FixedUpdate, al mateix ritme que la física)
        float duration = isOpen ? openDuration : closeDuration;
        openAmount = Mathf.MoveTowards(openAmount, isOpen ? 1f : 0f, Time.fixedDeltaTime / Mathf.Max(0.01f, duration));
        float eased = Mathf.SmoothStep(0f, 1f, openAmount);

        Vector3 position = closedPosition + openOffset * eased;
        Quaternion rotation = Quaternion.Euler(openRotation * eased) * closedRotation;

        if (body != null && body.isKinematic)
        {
            body.MovePosition(position);
            body.MoveRotation(rotation);
        }
        else
        {
            target.SetPositionAndRotation(position, rotation);
        }
    }

    private bool ConditionMet()
    {
        int total = 0;
        int active = 0;
        foreach (MonoBehaviour candidate in switches)
        {
            if (candidate is IPuzzleSwitch puzzleSwitch)
            {
                total++;
                if (puzzleSwitch.IsActive) active++;
            }
        }

        if (total == 0) return false;
        return condition == Condition.AllSwitches ? active == total : active > 0;
    }

    private void OnDrawGizmosSelected()
    {
        // A l'editor es veu on anirà l'objecte quan estigui obert
        Transform moved = target != null ? target : transform;
        Vector3 from = Application.isPlaying ? closedPosition : moved.position;
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.8f);
        Gizmos.DrawLine(from, from + openOffset);
        Gizmos.DrawWireCube(from + openOffset, moved.lossyScale);

        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.8f);
        foreach (MonoBehaviour candidate in switches)
        {
            if (candidate != null) Gizmos.DrawLine(moved.position, candidate.transform.position);
        }
    }
}
