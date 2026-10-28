using UnityEngine;
using UnityEngine.UI;

public class DynamicCrosshair : MonoBehaviour
{
    [Header("Referències de Càmera i UI")]
    [SerializeField] private Transform cam;          
    [SerializeField] private Image crosshairImage;    

    [Header("Configuració de Detecció")]
    [SerializeField] private float maxDistance = 25f;  
    [SerializeField] private LayerMask swingableLayers;
    [SerializeField] private float aimAssistRadius = 1.2f; // Igual que al Swing, perquè el verd coincideixi amb on s'enganxarà el fil
    [SerializeField] private Transform player; // Opcional: es busca sol pel tag Player

    [Header("Colors del Punt de Mira")]
    [SerializeField] private Color inactiveColor = new Color(1, 1, 1, 0.5f); 
    [SerializeField] private Color activeColor = Color.green;                
    [SerializeField] private Color connectPendingColor = new Color(0.3f, 0.85f, 1f, 1f); // Pont a mig fer i segon punt vàlid

    [Header("Connector de Ponts (opcional, es busca sol)")]
    [SerializeField] private WebConnector webConnector;

    void Start()
    {
        if (webConnector == null) webConnector = FindAnyObjectByType<WebConnector>();
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) player = playerObject.transform;
        }
    }

    void Update()
    {
        if (cam == null || crosshairImage == null) return;

        // Si hi ha un pont a mig fer, el punt de mira indica si el segon punt és vàlid
        if (webConnector != null && webConnector.HasPendingAnchor)
        {
            bool validAnchor = webConnector.IsAimingAtValidAnchor;
            crosshairImage.color = validAnchor ? connectPendingColor : inactiveColor;
            Vector3 targetScale = validAnchor ? new Vector3(1.2f, 1.2f, 1.2f) : Vector3.one;
            crosshairImage.transform.localScale = Vector3.Lerp(crosshairImage.transform.localScale, targetScale, Time.unscaledDeltaTime * 10f);
            return;
        }

        // Llançem un raig invisible frame a frame cap on mira el jugador (amb la mateixa ajuda d'apuntat que el Swing)
        Vector3 playerPosition = player != null ? player.position : cam.position;
        if (SpiderSwingingSimple.FindSwingPoint(cam, maxDistance * PlayerUpgrades.WebRangeMultiplier, swingableLayers, aimAssistRadius, playerPosition, out RaycastHit hit))
        {
            // SI TOQUEM UNA CAPA VÀLIDA: El punt es posa verd (Pots llançar!)
            crosshairImage.color = activeColor;

            // El punt de mira es faci una mica més gran quan és vàlid
            crosshairImage.transform.localScale = Vector3.Lerp(crosshairImage.transform.localScale, new Vector3(1.2f, 1.2f, 1.2f), Time.deltaTime * 10f);
        }
        else
        {
            // SI NO TOQUEM RES O ESTÀ MASSA LLUNY: El punt es posa blanc/inactiu
            crosshairImage.color = inactiveColor;
            crosshairImage.transform.localScale = Vector3.Lerp(crosshairImage.transform.localScale, Vector3.one, Time.deltaTime * 10f);
        }
    }
}

