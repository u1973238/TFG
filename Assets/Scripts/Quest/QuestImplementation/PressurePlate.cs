using System.Collections.Generic;
using UnityEngine;

public class PressurePlate : MonoBehaviour, IPuzzleSwitch
{
    [Header("Referències d'Elements")]
    [SerializeField] private Transform doorObject; // Opcional: també es pot deixar buit i fer servir un PuzzleMover
    [SerializeField] private Transform plateVisual;

    [Header("Configuració del Moviment")]
    [SerializeField] private Vector3 doorOpenOffset = new Vector3(0, -5f, 0);
    [SerializeField] private float plateDownOffset = 0.15f;
    [SerializeField] private string targetTag = "Pushable";

    [Header("Trencaclosques")]
    [SerializeField] private bool playerCounts = true; // Si és fals, només hi compten les roques (l'aranya no pesa prou)

    private readonly HashSet<Collider> weightsOnTop = new HashSet<Collider>();

    private Vector3 doorInitialPosition;
    private Vector3 plateInitialPosition;

    // La placa està premuda (la poden escoltar altres elements del trencaclosques)
    public bool IsActive => weightsOnTop.Count > 0;

    void Start()
    {
        if (doorObject != null) doorInitialPosition = doorObject.position;
        if (plateVisual != null) plateInitialPosition = plateVisual.localPosition;
    }

    void Update()
    {
        // Si un objecte desapareix mentre és a sobre, Unity no avisa amb OnTriggerExit: el traiem nosaltres
        int before = weightsOnTop.Count;
        weightsOnTop.RemoveWhere(weight => weight == null || !weight.enabled || !weight.gameObject.activeInHierarchy);
        if (before > 0 && weightsOnTop.Count == 0) AudioManager.Play("door");

        // Si hi ha algun pes (l'aranya, la pedra o totes dues coses)
        if (IsActive)
        {
            if (doorObject != null) doorObject.position = Vector3.Lerp(doorObject.position, doorInitialPosition + doorOpenOffset, Time.deltaTime * 3f);
            if (plateVisual != null) plateVisual.localPosition = Vector3.Lerp(plateVisual.localPosition, plateInitialPosition - new Vector3(0, plateDownOffset, 0), Time.deltaTime * 5f);
        }
        else
        {
            if (doorObject != null) doorObject.position = Vector3.Lerp(doorObject.position, doorInitialPosition, Time.deltaTime * 2f);
            if (plateVisual != null) plateVisual.localPosition = Vector3.Lerp(plateVisual.localPosition, plateInitialPosition, Time.deltaTime * 2f);
        }
    }

    public bool IsPlateActivated()
    {
        return IsActive;
    }

    private bool CountsAsWeight(Collider other)
    {
        return other.CompareTag(targetTag) || (playerCounts && other.CompareTag("Player"));
    }

    private void OnTriggerEnter(Collider other)
    {
        // Si entra la pedra O si entra el jugador (l'aranya), sumem pes
        if (CountsAsWeight(other) && weightsOnTop.Add(other) && weightsOnTop.Count == 1)
        {
            AudioManager.Play("plate");
            AudioManager.Play("door");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Si surt la pedra O surt el jugador, restem pes
        if (weightsOnTop.Remove(other) && weightsOnTop.Count == 0)
        {
            AudioManager.Play("door");
        }
    }
}
