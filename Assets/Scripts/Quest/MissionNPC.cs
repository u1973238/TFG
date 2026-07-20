using TMPro;
using UnityEngine;

public class MissionNPC : MonoBehaviour
{
    [Header("Referències d'Arquitectura")]
    [SerializeField] private InputReader inputReader;
    public MissionData missionToGive;

    [Header("Interfície Flotant (NOU)")]
    [SerializeField] private GameObject visualPrompt;
    [SerializeField] private TextMeshProUGUI liveDialog;

    [Header("Diàlegs de text (Depuració)")]
    public string interactionPrompt = "Prem E per interactuar";
    public string dialegInicial = "Hola aranya! Pots ajudar-me a recollir aquests objectes?";
    public string dialegEnProgres = "Encara estàs treballant en el meu encàrrec?";
    public string dialegCompletat = "Uau, moltes gràcies per salvar-me!";


    private bool playerInRange = false;

    void Start()
    {
        inputReader.interactEvent += HandleInteraction;

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
            //Debug.Log($"[NPC {name}]: {dialegInicial}");
            MissionManager.Instance.ActivateMission(missionToGive.missionID);
            liveDialog.text = dialegInicial; // Actualitzem el text del diàleg en pantalla
        }
        else if (missionToGive.isActive && !missionToGive.isCompleted)
        {
            //Debug.Log($"[NPC {name}]: {dialegEnProgres}");
            liveDialog.text = dialegEnProgres; // Actualitzem el text del diàleg en pantalla
        }
        else if (missionToGive.isCompleted)
        {
            //Debug.Log($"[NPC {name}]: {dialegCompletat}");
            liveDialog.text = dialegCompletat; // Actualitzem el text del diàleg en pantalla
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;

            // Mostrem el cartell "Press E" quan l'aranya s'apropa
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

            // Amaguem el cartell quan l'aranya s'allunya
            if (visualPrompt != null)
            {
                visualPrompt.SetActive(false);
                liveDialog.text = interactionPrompt;
            }
        }
    }
}
