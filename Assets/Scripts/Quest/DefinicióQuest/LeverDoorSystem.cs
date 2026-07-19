using UnityEngine;

public class LeverDoorSystem : MonoBehaviour
{
    [Header("Configuració de la Missió")]
    [SerializeField] private string missionID = "salvar_animals";

    [Header("Referències d'Elements")]
    [SerializeField] private Transform doorObject;   // La porta que s'obrirà
    [SerializeField] private Transform leverHandle;  // El pal de la palanca que rotarà

    [Header("Paràmetres de Moviment")]
    [SerializeField] private Vector3 doorOpenOffset = new Vector3(0, -5f, 0);
    [SerializeField] private float leverTargetRotation = 45f;

    // Variables internes per controlar qui està activant la palanca
    private bool isPlayerInside = false;
    private int websInsideCount = 0;
    private bool isDoorOpen = false;

    private Vector3 doorInitialPosition;
    private Quaternion leverInitialRotation;

    void Start()
    {
        if (doorObject != null) doorInitialPosition = doorObject.position;
        if (leverHandle != null) leverInitialRotation = leverHandle.localRotation;
    }

    void Update()
    {
        // CONDICIÓ D'OBERTURA: La palanca s'activa si l'aranya està a dins O si hi ha alguna teranyina tocant-la
        bool shouldOpen = isPlayerInside || (websInsideCount > 0);

        if (shouldOpen)
        {
            // Efecte visual: La palanca s'abaixa i la porta s'obre
            leverHandle.localRotation = Quaternion.Slerp(leverHandle.localRotation, Quaternion.Euler(leverTargetRotation, 0, 0), Time.deltaTime * 5f);
            doorObject.position = Vector3.Lerp(doorObject.position, doorInitialPosition + doorOpenOffset, Time.deltaTime * 3f);

            if (!isDoorOpen)
            {
                isDoorOpen = true;
                if (MissionManager.Instance != null)
                {
                    MissionManager.Instance.ProgressMission(missionID, 1);
                }
            }
        }
        else
        {
            // Efecte visual: La palanca puja sola i la porta es tanca de cop
            isDoorOpen = false;
            leverHandle.localRotation = Quaternion.Slerp(leverHandle.localRotation, leverInitialRotation, Time.deltaTime * 2f);
            doorObject.position = Vector3.Lerp(doorObject.position, doorInitialPosition, Time.deltaTime * 2f);
        }
    }

    // DETECCIÓ: Quan alguna cosa entra en contacte amb la palanca
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
            websInsideCount++;
        }
    }

    // DETECCIÓ: Quan alguna cosa perd el contacte amb la palanca
    private void OnTriggerExit(Collider other)
    {
        // Cas A: L'aranya marxa
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
        }

        // Cas B: La teranyina marxa o desapareix
        if (other.gameObject.layer == LayerMask.NameToLayer("Walkable"))
        {
            websInsideCount--;
            if (websInsideCount < 0) websInsideCount = 0; // Seguretat per evitar números negatius
        }
    }
}
