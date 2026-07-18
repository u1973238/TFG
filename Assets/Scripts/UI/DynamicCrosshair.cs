using UnityEngine;
using UnityEngine.UI; // IMPRESCINDIBLE per poder controlar imatges de la UI

public class DynamicCrosshair : MonoBehaviour
{
    [Header("Referències de Càmera i UI")]
    [SerializeField] private Transform cam;           // La teva Main Camera
    [SerializeField] private Image crosshairImage;    // L'objecte de la imatge del punt de mira al Canvas

    [Header("Configuració de Detecció")]
    [SerializeField] private float maxDistance = 25f;  // Posa la mateixa distància màxima que tingui el teu swing
    [SerializeField] private LayerMask swingableLayers;// La capa o capes vàlides per llançar teranyines

    [Header("Colors del Punt de Mira")]
    [SerializeField] private Color inactiveColor = new Color(1, 1, 1, 0.5f); // Blanc una mica transparent
    [SerializeField] private Color activeColor = Color.green;                // Verd brillant quan es pot llançar

    void Update()
    {
        if (cam == null || crosshairImage == null) return;

        RaycastHit hit;
        Ray ray = new Ray(cam.position, cam.forward);

        // Llançem un raig invisible frame a frame cap on mira el jugador
        if (Physics.Raycast(ray, out hit, maxDistance, swingableLayers))
        {
            // SI TOQUEM UNA CAPA VÀLIDA: El punt es posa verd (Pots llançar!)
            crosshairImage.color = activeColor;

            // OPCIONAL: Pots fer que el punt de mira es faci una mica més gran quan és vàlid
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

