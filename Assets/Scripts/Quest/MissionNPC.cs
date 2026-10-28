using System.Collections.Generic;
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
    public string interactionPrompt = "Prem E per parlar";
    public string dialegInicial = "Hola, aranya! Em pots ajudar a recollir aquests objectes?";
    public string dialegEnProgres = "Encara estàs treballant en el meu encàrrec?";
    public string dialegCompletat = "Uau, moltes gràcies per salvar-me!"; // Es diu en lliurar la missió
    public string dialegDespres = "Gràcies una altra vegada per la teva ajuda!"; // Quan ja està lliurada
    public string dialegBloquejat = "Encara no estàs preparada per a això. Primer ajuda altres animals de l'illa."; // Si falta alguna missió prèvia


    private bool playerInRange = false;
    private NPCAnimator npcAnimator;

    void Start()
    {
        inputReader.interactEvent += HandleInteraction;

        // L'animació s'afegeix sola si el prefab encara no la té
        npcAnimator = GetComponent<NPCAnimator>();
        if (npcAnimator == null) npcAnimator = gameObject.AddComponent<NPCAnimator>();

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

        // L'NPC fa un saltet i un so quan parla
        if (npcAnimator != null) npcAnimator.Talk();
        AudioManager.Play("talk");

        if (!missionToGive.isActive && !missionToGive.isCompleted)
        {
            // Si cal acabar abans altres missions, l'NPC diu quines
            List<MissionData> missing = MissionManager.Instance.GetMissingRequirements(missionToGive);
            if (missing.Count > 0)
            {
                List<string> titles = missing.ConvertAll(m => m.title);
                SetDialog(dialegBloquejat + "\n<size=80%><i>Abans: " + string.Join(", ", titles) + "</i></size>");
                return;
            }

            //Debug.Log($"[NPC {name}]: {dialegInicial}");
            MissionManager.Instance.ActivateMission(missionToGive.missionID);
            SetDialog(dialegInicial);
        }
        else if (missionToGive.isActive && !missionToGive.isCompleted)
        {
            //Debug.Log($"[NPC {name}]: {dialegEnProgres}");
            SetDialog(dialegEnProgres);
        }
        else if (missionToGive.IsReadyToDeliver)
        {
            // Lliurament: la missió s'acaba i l'aranya rep la recompensa
            MissionManager.Instance.DeliverMission(missionToGive, out string rewardText);
            string text = dialegCompletat;
            if (rewardText != "") text += "\n<size=80%><color=#FFD54A>Recompensa: " + rewardText + "</color></size>";
            SetDialog(text);
        }
        else
        {
            SetDialog(dialegDespres);
        }
    }

    // Actualitzem el text del diàleg en pantalla
    private void SetDialog(string text)
    {
        if (liveDialog != null) liveDialog.text = text;
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
