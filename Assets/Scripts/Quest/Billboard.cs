using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform mainCameraTransform;

    void Start()
    {
        // Busquem la càmera principal de l'escena només començar
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        if (mainCameraTransform != null)
        {
            // Forcem que l'objecte es mantingui completament paral·lel al pla de la càmera.
            // Això evita que el text s'inclini de forma estranya si puges a una paret.
            transform.LookAt(transform.position + mainCameraTransform.forward);
        }
    }
}
