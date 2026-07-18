using UnityEngine;

public class MissionNPC : MonoBehaviour
{
    [Header("Referències d'Arquitectura")]
    [SerializeField] private InputReader inputReader;
    public MissionData missionToGive;

    [Header("Interfície Flotant (NOU)")]
    [SerializeField] private GameObject visualPrompt; // El cartell de "Press E"

    [Header("Diàlegs de text (Depuració)")]
    public string dialegInicial = "Hola aranya! Pots ajudar-me a recollir aquests objectes?";
    public string dialegEnProgres = "Encara estàs treballant en el meu encàrrec?";
    public string dialegCompletat = "Uau, moltes gràcies per salvar-me!";

    private bool playerInRange = false;

    void Start()
    {
        inputReader.interactEvent += HandleInteraction;

        // Ens assegurem que el cartell comenci amagat al principi del joc
        if (visualPrompt != null)
        {
            visualPrompt.SetActive(false);
        }
    }

    void OnDestroy()
    {
        inputReader.interactEvent -= HandleInteraction;
    }

    private void HandleInteraction()
    {
        if (!playerInRange) return;

        if (!missionToGive.isActive && !missionToGive.isCompleted)
        {
            Debug.Log($"[NPC {name}]: {dialegInicial}");
            MissionManager.Instance.ActivateMission(missionToGive.missionID);
        }
        else if (missionToGive.isActive && !missionToGive.isCompleted)
        {
            Debug.Log($"[NPC {name}]: {dialegEnProgres}");
        }
        else if (missionToGive.isCompleted)
        {
            Debug.Log($"[NPC {name}]: {dialegCompletat}");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;

            // NOU: Mostrem el cartell "Press E" quan l'aranya s'apropa
            if (visualPrompt != null)
            {
                visualPrompt.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;

            // NOU: Amaguem el cartell quan l'aranya s'allunya
            if (visualPrompt != null)
            {
                visualPrompt.SetActive(false);
            }
        }
    }
}
