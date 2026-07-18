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
    }

    void OnDestroy()
    {
        inputReader.interactEvent -= HandleInteraction;
    }

    private void HandleInteraction()
    {
        if (!playerInRange) return;

        if (challengeMeta != null)
        {
            // Donem el tret de sortida a la cursa i activem el cronòmetre de la meta!
            challengeMeta.StartChallenge();

            // Opcional: Desactivem el cartell de parlar perquè ja estem corrent
            if (visualPrompt != null) visualPrompt.SetActive(false);
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
