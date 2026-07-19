using UnityEngine;

public class MissionCollectible : MonoBehaviour
{
    [Header("Configuració de la Missió")]
    [SerializeField] private string targetMissionID = "CollectItems";
    [SerializeField] private int value = 1;

    [Header("Efecte Visual")]
    [SerializeField] private float rotationSpeed = 100f;

    void Update()
    {
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (MissionManager.Instance != null)
            {
                // Intentem processar la recollida. Si el gestor diu 'true' (missió activa), l'objecte es destrueix.
                if (MissionManager.Instance.ProgressMission(targetMissionID, value))
                {
                    Destroy(gameObject);
                }
                else
                {
                    Debug.LogWarning("No pots recollir aquest objecte encara. Parla primer amb l'animal!");
                }
            }
        }
    }
}
