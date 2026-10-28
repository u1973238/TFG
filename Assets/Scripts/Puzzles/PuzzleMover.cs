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
    [SerializeField] private float openSpeed = 2f;
    [SerializeField] private float closeSpeed = 1.5f;

    [Header("Missió (opcional)")]
    [SerializeField] private MissionData missionOnOpen; // Suma 1 a aquesta missió la primera vegada que s'obre

    private Vector3 closedPosition;
    private Quaternion closedRotation;
    private float openAmount = 0f; // 0 = tancat, 1 = obert
    private bool isOpen = false;
    private bool latched = false;
    private bool missionDone = false;

    public bool IsOpen => isOpen;

    void Start()
    {
        if (target == null) target = transform;
        closedPosition = target.position;
        closedRotation = target.rotation;

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

        // Moviment suau cap a la posició oberta o tancada
        float speed = isOpen ? openSpeed : closeSpeed;
        openAmount = Mathf.MoveTowards(openAmount, isOpen ? 1f : 0f, speed * Time.deltaTime);
        float eased = Mathf.SmoothStep(0f, 1f, openAmount);

        target.position = closedPosition + openOffset * eased;
        target.rotation = Quaternion.Euler(openRotation * eased) * closedRotation;
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
