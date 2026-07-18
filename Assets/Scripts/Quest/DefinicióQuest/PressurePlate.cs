using UnityEngine;

public class PressurePlate : MonoBehaviour
{
    [Header("Referències d'Elements")]
    [SerializeField] private Transform doorObject;
    [SerializeField] private Transform plateVisual;

    [Header("Configuració del Moviment")]
    [SerializeField] private Vector3 doorOpenOffset = new Vector3(0, -5f, 0);
    [SerializeField] private float plateDownOffset = 0.15f;
    [SerializeField] private string targetTag = "Pushable";

    // NOU: Comptador per saber quants cossos pesants hi ha sobre la placa
    private int weightsOnTopCount = 0;

    private Vector3 doorInitialPosition;
    private Vector3 plateInitialPosition;

    void Start()
    {
        if (doorObject != null) doorInitialPosition = doorObject.position;
        if (plateVisual != null) plateInitialPosition = plateVisual.localPosition;
    }

    void Update()
    {
        // Si el comptador és major que 0 (hi ha l'aranya, la pedra o totes dues coses)
        if (weightsOnTopCount > 0)
        {
            doorObject.position = Vector3.Lerp(doorObject.position, doorInitialPosition + doorOpenOffset, Time.deltaTime * 3f);
            plateVisual.localPosition = Vector3.Lerp(plateVisual.localPosition, plateInitialPosition - new Vector3(0, plateDownOffset, 0), Time.deltaTime * 5f);
        }
        else
        {
            doorObject.position = Vector3.Lerp(doorObject.position, doorInitialPosition, Time.deltaTime * 2f);
            plateVisual.localPosition = Vector3.Lerp(plateVisual.localPosition, plateInitialPosition, Time.deltaTime * 2f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // NOU: Si entra la pedra O si entra el jugador (l'aranya), sumem pes
        if (other.CompareTag(targetTag) || other.CompareTag("Player"))
        {
            weightsOnTopCount++;
            Debug.Log($"Pes afegit a la placa. Objectes a sobre: {weightsOnTopCount}");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // NOU: Si surt la pedra O surt el jugador, restem pes
        if (other.CompareTag(targetTag) || other.CompareTag("Player"))
        {
            weightsOnTopCount--;
            if (weightsOnTopCount < 0) weightsOnTopCount = 0; // Seguretat
            Debug.Log($"Pes retirat de la placa. Objectes a sobre: {weightsOnTopCount}");
        }
    }
}

