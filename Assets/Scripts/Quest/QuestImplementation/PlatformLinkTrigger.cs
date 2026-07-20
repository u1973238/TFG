using UnityEngine;

public class PlatformLinkTrigger : MonoBehaviour
{
    [Header("Configuración de la Misión")]
    [SerializeField] private string missionID = "conectar_plataformas"; 

    private bool isConnected = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isConnected) return;

        // Deteció si es una trenyina
        if (other.gameObject.layer == LayerMask.NameToLayer("Climbing"))
        {
            isConnected = true;
            //Debug.Log($"[LISTA DE MISIONES] ¡Plataforma conectada con éxito en: {name}!");

            // +1 al contador
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.ProgressMission(missionID, 1);
            }
        }
    }
}
