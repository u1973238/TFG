using UnityEngine;

public class RaceNPC : MonoBehaviour
{
    [Header("Referències d'Arquitectura")]
    [SerializeField] private InputReader inputReader;
    [SerializeField] private TimeTrialChallenge challengeMeta; // La meta que està a dalt de tot

    [Header("Interfície Flotant")]
    [SerializeField] private GameObject visualPrompt; // El cartell de [E] Parlar

    private bool playerInRange = false;

    void Start()
    {
        inputReader.interactEvent += HandleInteraction;
        if (visualPrompt != null) visualPrompt.SetActive(false);

        // L'animació s'afegeix sola si el prefab encara no la té
        if (GetComponent<NPCAnimator>() == null) gameObject.AddComponent<NPCAnimator>();
    }

    void OnDestroy()
    {
        inputReader.interactEvent -= HandleInteraction;
    }

    private void HandleInteraction()
    {
        if (!playerInRange) return;

        MissionNPC missionNPC = GetComponent<MissionNPC>();
        MissionData mission = missionNPC != null ? missionNPC.missionToGive : null;
        bool canRace = mission == null || (!mission.isCompleted && (mission.isActive || MissionManager.Instance == null || MissionManager.Instance.CanAccept(mission)));

        if (challengeMeta != null && canRace)
        {
            // Donem el tret de sortida a la cursa i activem el cronòmetre de la meta!
            challengeMeta.StartChallenge();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            if (visualPrompt != null) visualPrompt.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (visualPrompt != null) visualPrompt.SetActive(false);
        }
    }
}
