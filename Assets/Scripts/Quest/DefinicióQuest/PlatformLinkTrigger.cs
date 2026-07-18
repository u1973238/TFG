using UnityEngine;

public class PlatformLinkTrigger : MonoBehaviour
{
    [Header("Configuración de la Misión")]
    [SerializeField] private string missionID = "conectar_plataformas"; // ID de la misión en tu ScriptableObject

    [Header("Efectos Visuales (Opcional)")]
    [SerializeField] private ParticleSystem successParticles; // Por si quieres poner partículas al conectar

    private bool isConnected = false;

    private void OnTriggerEnter(Collider other)
    {
        // Si este puente ya fue conectado, ignoramos cualquier otra telaraña
        if (isConnected) return;

        // DETECCIÓN: Si lo que cruza es un puente de telaraña (capa Walkable)
        if (other.gameObject.layer == LayerMask.NameToLayer("Walkable"))
        {
            isConnected = true;
            Debug.Log($"[LISTA DE MISIONES] ¡Plataforma conectada con éxito en: {name}!");

            // 1. Sumamos +1 al contador de la misión global
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.ProgressMission(missionID, 1);
            }

            // 2. Activamos efectos si los hay
            if (successParticles != null)
            {
                successParticles.Play();
            }

            // Opcional: Puedes cambiarle el color al objeto o hacer un sonido aquí
        }
    }
}
