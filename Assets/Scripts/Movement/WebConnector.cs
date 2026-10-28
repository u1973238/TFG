using System.Collections.Generic;
using UnityEngine;

public class WebConnector : MonoBehaviour
{
    [Header("Referències de l'Arquitectura")]
    [SerializeField] private InputReader inputReader;

    [Header("Configuració dels Ponts")]
    public LayerMask anchorLayers;
    public float maxConnectDistance = 30f;
    public float bridgeWidth = 0.4f; // Amplada per on es pot caminar (el col·lider)

    [Header("Aspecte dels Ponts")]
    public int strandCount = 4; // Fils paral·lels que formen el pont
    public float strandWidth = 0.035f; // Gruix de cada fil
    public float sagAmount = 0.02f; // Quant es pengen els fils al mig (proporció de la llargada)
    public float crossThreadSpacing = 0.6f; // Distància entre els fils en zig-zag que uneixen els fils principals
    public Color webColor = new Color(0.95f, 0.95f, 0.95f, 0.9f);

    [Header("Llit Elàstic")]
    public bool bouncyBridges = true; // Si l'aranya cau sobre un pont des de prou amunt, surt rebotada
    public float bounceThreshold = 8f; // Velocitat de caiguda mínima per rebotar (m/s, uns 3 metres d'alçada)
    public float bounciness = 0.85f; // Quina part de la velocitat de caiguda es recupera en rebotar
    public float maxBounceSpeed = 25f;

    [Header("Límits per a Trencaclosques (0 = sense límit)")]
    public float maxBridgeLength = 0f; // Llargada màxima d'un pont
    public int maxBridges = 0; // Nombre màxim de ponts a la vegada; si se'n fa un de més, desapareix el més antic

    [Header("Previsualització del Pont")]
    public Color previewValidColor = new Color(1f, 1f, 1f, 0.6f); // El segon punt és vàlid
    public Color previewInvalidColor = new Color(1f, 0.35f, 0.35f, 0.6f); // No s'apunta a cap superfície vàlida

    private Vector3? firstAnchorPoint = null;
    private LineRenderer previewLine;
    private readonly List<GameObject> bridges = new List<GameObject>();

    private static Material webMaterial;

    // Material compartit per a tots els fils (el fil del swing, els ponts i la previsualització)
    public static Material GetWebMaterial()
    {
        if (webMaterial == null) webMaterial = new Material(Shader.Find("Sprites/Default"));
        return webMaterial;
    }

    // Permet que altres scripts (per exemple, el punt de mira) sàpiguen si hi ha un pont a mig fer
    public bool HasPendingAnchor => firstAnchorPoint != null;
    public bool IsAimingAtValidAnchor { get; private set; }

    void Start()
    {
        // Ens subscrivim al clic dret de l'InputReader
        inputReader.connectEvent += HandleConnectionAttempt;

        CreatePreviewLine();
    }

    void Update()
    {
        UpdatePreviewLine();
    }

    private void CreatePreviewLine()
    {
        // Línia fina que mostra el fil pendent entre el primer ancoratge i el punt on s'apunta
        GameObject previewObject = new GameObject("WebBridge_Preview");
        previewObject.transform.SetParent(transform, false);

        previewLine = previewObject.AddComponent<LineRenderer>();
        previewLine.positionCount = 2;
        previewLine.widthMultiplier = strandWidth;
        previewLine.sharedMaterial = GetWebMaterial();
        previewLine.enabled = false;
    }

    private void UpdatePreviewLine()
    {
        IsAimingAtValidAnchor = false;

        if (firstAnchorPoint == null)
        {
            if (previewLine != null) previewLine.enabled = false;
            return;
        }

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); // Centre de la pantalla
        Vector3 endPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, maxConnectDistance, anchorLayers))
        {
            endPoint = hit.point;
            IsAimingAtValidAnchor = IsValidBridge(firstAnchorPoint.Value, hit.point);
        }
        else
        {
            endPoint = ray.GetPoint(Mathf.Min(maxConnectDistance, 30f));
        }

        Color previewColor = IsAimingAtValidAnchor ? previewValidColor : previewInvalidColor;
        previewLine.startColor = previewColor;
        previewLine.endColor = previewColor;
        previewLine.SetPosition(0, firstAnchorPoint.Value);
        previewLine.SetPosition(1, endPoint);
        previewLine.enabled = true;
    }

    void OnDestroy()
    {
        inputReader.connectEvent -= HandleConnectionAttempt;
    }

    // Un pont és vàlid si no és massa curt ni (si hi ha límit) massa llarg
    private bool IsValidBridge(Vector3 start, Vector3 end)
    {
        float distance = Vector3.Distance(start, end);
        return distance > 1f && (maxBridgeLength <= 0f || distance <= maxBridgeLength);
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
                AudioManager.Play("web_attach", 0.7f);
                Effects.Puff(hit.point, hit.normal, new Color(1f, 1f, 1f, 0.8f), 8, 0.25f, 1.5f);
                //Debug.Log($"Primer ancoratge col·locat a: {firstAnchorPoint}");
            }
            else
            {
                // 2. SEGON CLIC: Connectem amb el destí i generem el pont
                Vector3 secondAnchorPoint = hit.point;

                // Evitem crear ponts si els dos clics són massa propers (o massa lluny, si hi ha límit)
                if (IsValidBridge(firstAnchorPoint.Value, secondAnchorPoint))
                {
                    GenerateWebBridge(firstAnchorPoint.Value, secondAnchorPoint);
                    Effects.Puff(secondAnchorPoint, hit.normal, new Color(1f, 1f, 1f, 0.8f), 8, 0.25f, 1.5f);
                }
                else
                {
                    AudioManager.Play("web_release", 0.6f);
                }

                // Resetejem l'estat per poder fer una altra connexió independent
                firstAnchorPoint = null;
            }
        }
        else if (firstAnchorPoint != null)
        {
            // Si es clica sense apuntar a cap superfície vàlida, es cancel·la el pont a mig fer
            firstAnchorPoint = null;
            AudioManager.Play("web_release", 0.6f);
        }
    }

    private void GenerateWebBridge(Vector3 start, Vector3 end)
    {
        // Creem un GameObject buit que contindrà la física i gràfics del pont
        GameObject bridge = new GameObject("WebBridge_Dynamic");

        // El col·loquem al centre exacte entre els dos punts d'ancoratge
        Vector3 centerPoint = (start + end) / 2f;
        bridge.transform.position = centerPoint;

        // Orientem el pont des de l'inici fins al final
        bridge.transform.LookAt(end);

        float distance = Vector3.Distance(start, end);

        // El pont es veu com una teranyina: uns quants fils prims que es pengen una mica, units per fils en zig-zag
        Vector3 right = bridge.transform.right;
        Vector3 up = bridge.transform.up;
        float sag = Mathf.Min(distance * sagAmount, 0.3f);
        int segments = Mathf.Clamp(Mathf.CeilToInt(distance / 0.5f), 4, 40);

        Vector3[][] strands = new Vector3[Mathf.Max(2, strandCount)][];
        for (int s = 0; s < strands.Length; s++)
        {
            // Els fils es reparteixen per l'amplada del pont; els dels costats queden una mica més amunt, com una hamaca
            float side = Mathf.Lerp(-0.45f, 0.45f, (float)s / (strands.Length - 1));
            float lift = Mathf.Abs(side) * 0.15f;

            strands[s] = new Vector3[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float hang = 4f * t * (1f - t) * sag;
                strands[s][i] = Vector3.Lerp(start, end, t) + right * side * bridgeWidth + up * (lift - hang);
            }
        }

        // Fils en zig-zag entre els dos fils exteriors
        int crossPoints = Mathf.Max(2, Mathf.CeilToInt(distance / crossThreadSpacing));
        Vector3[] zigzag = new Vector3[crossPoints + 1];
        for (int i = 0; i <= crossPoints; i++)
        {
            Vector3[] strand = (i % 2 == 0) ? strands[0] : strands[strands.Length - 1];
            float t = (float)i / crossPoints;
            zigzag[i] = strand[Mathf.RoundToInt(t * segments)];
        }

        LineRenderer[] lines = new LineRenderer[strands.Length + 1];
        Vector3[][] linePoints = new Vector3[strands.Length + 1][];
        for (int s = 0; s < strands.Length; s++)
        {
            lines[s] = CreateWebLine(bridge.transform, "Fil_" + s, strandWidth);
            linePoints[s] = strands[s];
        }
        lines[strands.Length] = CreateWebLine(bridge.transform, "Fil_ZigZag", strandWidth * 0.6f);
        linePoints[strands.Length] = zigzag;

        // El component WebBridge dibuixa els fils, els fa desplegar-se, tremolar i rebotar
        WebBridge webBridge = bridge.AddComponent<WebBridge>();
        webBridge.Init(lines, linePoints, start, Mathf.Clamp(0.2f + distance * 0.01f, 0.2f, 0.6f),
                       bouncyBridges, bounceThreshold, bounciness, maxBounceSpeed);
        AudioManager.Play("bridge");

        // Si hi ha un màxim de ponts, el més antic desapareix
        bridges.RemoveAll(existing => existing == null);
        bridges.Add(bridge);
        if (maxBridges > 0 && bridges.Count > maxBridges + PlayerUpgrades.ExtraBridges)
        {
            Destroy(bridges[0]);
            bridges.RemoveAt(0);
        }

        // 3. Afegim la BoxCollider perquè l'aranya hi camini per sobre
        BoxCollider boxCollider = bridge.AddComponent<BoxCollider>();

        // El gruix del pont físic (en X, Y) i la seva longitud total (en Z)
        boxCollider.size = new Vector3(bridgeWidth, 0.05f, distance);

        // Assignem el pont a la capa "Climbing" perquè el nostre script de terra el detecti
        bridge.layer = LayerMask.NameToLayer("Climbing");

        //Debug.Log($"Pont de teranyina de {distance:F2} metres de longitud creat amb èxit!");
    }

    private LineRenderer CreateWebLine(Transform parent, string lineName, float width)
    {
        GameObject lineObject = new GameObject(lineName);
        lineObject.transform.SetParent(parent, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.widthMultiplier = width;
        line.numCapVertices = 2;
        line.sharedMaterial = GetWebMaterial();
        line.startColor = webColor;
        line.endColor = webColor;
        line.positionCount = 0;
        return line;
    }
}
