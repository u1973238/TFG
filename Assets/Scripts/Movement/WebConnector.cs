using UnityEngine;

public class WebConnector : MonoBehaviour
{
    [Header("Referències de l'Arquitectura")]
    [SerializeField] private InputReader inputReader;

    [Header("Configuració dels Ponts")]
    public LayerMask anchorLayers; // Superfícies aptes per fixar els ponts (ex: Walkable)
    public float maxConnectDistance = 30f; // Distància màxima del pont
    public float bridgeWidth = 0.4f; // Amplada de la passarel·la física de teranyina

    private Vector3? firstAnchorPoint = null; // Primer punt guardat (nullable)

    void Start()
    {
        // Ens subscrivim al clic dret de l'InputReader
        inputReader.connectEvent += HandleConnectionAttempt;
    }

    void OnDestroy()
    {
        inputReader.connectEvent -= HandleConnectionAttempt;
    }

    private void HandleConnectionAttempt()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); // Centre de la pantalla

        if (Physics.Raycast(ray, out hit, maxConnectDistance, anchorLayers))
        {
            if (firstAnchorPoint == null)
            {
                // 1. PRIMER CLIC: Guardem l'origen de la teranyina
                firstAnchorPoint = hit.point;
                Debug.Log($"Primer ancoratge col·locat a: {firstAnchorPoint}");
                // (Opcional: Podries fer aparcar una petita partícula o punt visual aquí)
            }
            else
            {
                // 2. SEGON CLIC: Connectem amb el destí i generem el pont
                Vector3 secondAnchorPoint = hit.point;

                // Evitem crear ponts si els dos clics són massa propers
                if (Vector3.Distance(firstAnchorPoint.Value, secondAnchorPoint) > 1f)
                {
                    GenerateWebBridge(firstAnchorPoint.Value, secondAnchorPoint);
                }

                // Resetejem l'estat per poder fer una altra connexió independent
                firstAnchorPoint = null;
            }
        }
    }

    private void GenerateWebBridge(Vector3 start, Vector3 end)
    {
        // 1. Creem un GameObject buit que contindrà la física i gràfics del pont
        GameObject bridge = new GameObject("WebBridge_Dynamic");

        // El col·loquem al centre exacte entre els dos punts d'ancoratge
        Vector3 centerPoint = (start + end) / 2f;
        bridge.transform.position = centerPoint;

        // Orientem el pont des de l'inici fins al final
        bridge.transform.LookAt(end);

        // 2. Afegim el component visual (LineRenderer) per dibuixar la teranyina
        LineRenderer lr = bridge.AddComponent<LineRenderer>();
        lr.startWidth = bridgeWidth;
        lr.endWidth = bridgeWidth;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = new Color(0.9f, 0.9f, 0.9f, 0.8f); // Blanc una mica transparent
        lr.endColor = new Color(0.9f, 0.9f, 0.9f, 0.8f);

        // Definim els dos punts de la línia respecte al món global
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);

        // 3. Afegim la col·lisió física (BoxCollider) perquè l'aranya hi camini per sobre
        BoxCollider boxCollider = bridge.AddComponent<BoxCollider>();

        float distance = Vector3.Distance(start, end);
        // El gruix del pont físic (en X, Y) i la seva longitud total (en Z)
        boxCollider.size = new Vector3(bridgeWidth, 0.05f, distance);

        // Assignem el pont a la capa "Walkable" perquè el nostre script de terra el detecti
        bridge.layer = LayerMask.NameToLayer("Walkable");

        Debug.Log($"Pont de teranyina de {distance:F2} metres de longitud creat amb èxit!");
    }
}
