using UnityEngine;

// Zona amb límit de ponts: mentre l'aranya hi és a dins, només pot tenir uns quants ponts de teranyina alhora
// (i, si es vol, més curts). En sortir-ne, es tornen a aplicar els valors normals del WebConnector.
// Serveix per a trencaclosques en què el jugador ha de pensar quins ponts li calen de veritat
[RequireComponent(typeof(BoxCollider))]
public class BridgeLimitZone : MonoBehaviour
{
    [SerializeField] private int maxBridges = 2; // Ponts alhora dins de la zona; si se'n fa un de més, desapareix el més antic
    [SerializeField] private float maxBridgeLength = 0f; // Llargada màxima d'un pont dins de la zona (0 = la de sempre)
    [SerializeField] private bool showNotice = true; // Avisa el jugador del límit quan entra a la zona

    private WebConnector connector;
    private int normalMaxBridges;
    private float normalMaxBridgeLength;

    void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (connector != null || !other.CompareTag("Player")) return;

        connector = other.GetComponentInParent<WebConnector>();
        if (connector == null) return;

        normalMaxBridges = connector.maxBridges;
        normalMaxBridgeLength = connector.maxBridgeLength;
        connector.maxBridges = maxBridges;
        if (maxBridgeLength > 0f) connector.maxBridgeLength = maxBridgeLength;

        // Si ja tenia més ponts dels permesos, desapareixen els més antics
        connector.TrimBridges();

        if (showNotice && MissionManager.Instance != null)
        {
            string bridges = maxBridges == 1 ? "1 pont de teranyina" : maxBridges + " ponts de teranyina";
            MissionManager.Instance.ShowNotification($"Aquí només pots tenir {bridges} alhora.", NotificationKind.Info);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (connector == null || !other.CompareTag("Player") || other.GetComponentInParent<WebConnector>() != connector) return;

        connector.maxBridges = normalMaxBridges;
        connector.maxBridgeLength = normalMaxBridgeLength;
        connector = null;
    }

    private void OnDrawGizmos()
    {
        // A l'editor es veu la caixa de la zona en blau cel
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null) return;
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.35f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(box.center, box.size);
    }
}
