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

    [Header("Colors del Punt de Mira")]
    [SerializeField] private Color inactiveColor = new Color(1, 1, 1, 0.5f); 
    [SerializeField] private Color activeColor = Color.green;                

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

