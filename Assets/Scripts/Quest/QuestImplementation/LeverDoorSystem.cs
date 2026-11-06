using System.Collections.Generic;
using UnityEngine;

public class LeverDoorSystem : MonoBehaviour, IPuzzleSwitch
{
    [Header("Configuració de la Missió")]
    [SerializeField] private string missionID = "salvar_animals";

    [Header("Referències d'Elements")]
    [SerializeField] private Transform doorObject;  // Opcional: també es pot deixar buit i fer servir un PuzzleMover
    [SerializeField] private Transform leverHandle; 

    [Header("Paràmetres de Moviment")]
    [SerializeField] private Vector3 doorOpenOffset = new Vector3(0, -5f, 0);
    [SerializeField] private float leverTargetRotation = 45f;

    [Header("Comportament")]
    // Un cop activada (per l'aranya o per un pont), la palanca es queda baixada i la porta oberta.
    // Desmarca-ho perquè només aguanti mentre l'aranya hi és al costat o un pont la travessa
    [SerializeField] private bool stayActivated = true;

    // Variables internes per controlar qui està activant la palanca
    private bool isPlayerInside = false;
    private readonly HashSet<Collider> websInside = new HashSet<Collider>();
    private bool isDoorOpen = false;
    private bool latched = false;

    private Vector3 doorInitialPosition;
    private Quaternion leverInitialRotation;

    // La palanca està activada (la poden escoltar altres elements del trencaclosques)
    public bool IsActive => isDoorOpen;

    void Start()
    {
        if (doorObject != null) doorInitialPosition = doorObject.position;
        if (leverHandle != null) leverInitialRotation = leverHandle.localRotation;
    }

    void Update()
    {
        // Els ponts de teranyina es poden esborrar; traiem els que ja no existeixen
        websInside.RemoveWhere(web => web == null || !web.enabled || !web.gameObject.activeInHierarchy);

        // CONDICIÓ D'OBERTURA: La palanca s'activa si l'aranya està a dins O si hi ha alguna teranyina tocant-la
        // (o si ja s'havia activat abans i es queda baixada)
        bool shouldOpen = latched || isPlayerInside || websInside.Count > 0;

        if (shouldOpen)
        {
            // Efecte visual: La palanca s'abaixa i la porta s'obre
            if (leverHandle != null) leverHandle.localRotation = Quaternion.Slerp(leverHandle.localRotation, Quaternion.Euler(leverTargetRotation, 0, 0), Time.deltaTime * 5f);
            if (doorObject != null) doorObject.position = Vector3.Lerp(doorObject.position, doorInitialPosition + doorOpenOffset, Time.deltaTime * 3f);

            if (!isDoorOpen)
            {
                isDoorOpen = true;
                if (stayActivated) latched = true;
                AudioManager.Play("door");
                if (MissionManager.Instance != null)
                {
                    MissionManager.Instance.ProgressMission(missionID, 0); // Codi entic, es per si la misio es completada amb aquesta acció
                }
            }
        }
        else
        {
            // Efecte visual: La palanca puja sola i la porta es tanca de cop
            if (isDoorOpen) AudioManager.Play("door");
            isDoorOpen = false;
            if (leverHandle != null) leverHandle.localRotation = Quaternion.Slerp(leverHandle.localRotation, leverInitialRotation, Time.deltaTime * 2f);
            if (doorObject != null) doorObject.position = Vector3.Lerp(doorObject.position, doorInitialPosition, Time.deltaTime * 2f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Cas A: És el jugador (l'aranya)
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
        }

        // Cas B: És una teranyina (objecte generat a la capa Climbing)
        if (other.gameObject.layer == LayerMask.NameToLayer("Climbing"))
        {
            websInside.Add(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Cas A: L'aranya marxa
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
        }

        // Cas B: La teranyina marxa o desapareix (abans es comprovava la capa Walkable per error)
        if (other.gameObject.layer == LayerMask.NameToLayer("Climbing"))
        {
            websInside.Remove(other);
        }
    }
}
