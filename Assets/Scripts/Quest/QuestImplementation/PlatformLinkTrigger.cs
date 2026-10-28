using System.Collections;
using UnityEngine;

// Zona de rescat: un volum invisible al buit entre dues plataformes. Quan un pont de teranyina el travessa,
// l'animal atrapat queda connectat i suma +1 a la missió. Es poden posar tantes zones com animals atrapats
// (posa a la missió «Required Amount» = nombre de zones). Opcionalment, l'animal camina pel pont fins a un lloc segur
public class PlatformLinkTrigger : MonoBehaviour
{
    [Header("Configuración de la Misión")]
    [SerializeField] private string missionID = "conectar_plataformas"; 

    [Header("Animal rescatat (opcional)")]
    [SerializeField] private Transform rescuedAnimal; // L'animal atrapat
    [SerializeField] private Transform safePoint; // On ha d'anar quan el pont estigui fet (a l'altra banda)
    [SerializeField] private float walkSpeed = 2.5f;
    [SerializeField] private float hopHeight = 0.3f;

    private bool isConnected = false;
    private bool bridgeDetected = false;

    void Update()
    {
        // Si ja hi ha un pont però la missió encara no estava activa, ho tornem a intentar fins que el gestor accepti el progrés
        if (bridgeDetected && !isConnected && MissionManager.Instance != null)
        {
            isConnected = MissionManager.Instance.ProgressMission(missionID, 1);
            if (isConnected) OnRescued();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Deteció si es una trenyina
        if (other.gameObject.layer == LayerMask.NameToLayer("Climbing"))
        {
            bridgeDetected = true;
        }
    }

    private void OnRescued()
    {
        if (MissionManager.Instance != null) MissionManager.Instance.ShowNotification("Animal rescatat!");
        if (rescuedAnimal != null && safePoint != null) StartCoroutine(WalkToSafety());
    }

    // L'animal creua el pont fent petits salts fins al punt segur
    private IEnumerator WalkToSafety()
    {
        Vector3 start = rescuedAnimal.position;
        Vector3 end = safePoint.position;
        float distance = Vector3.Distance(start, end);
        float duration = Mathf.Max(0.5f, distance / walkSpeed);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float hop = Mathf.Abs(Mathf.Sin(t * distance * Mathf.PI)) * hopHeight;
            rescuedAnimal.position = Vector3.Lerp(start, end, t) + Vector3.up * hop;
            yield return null;
        }

        rescuedAnimal.position = end;
        Effects.Puff(end, Vector3.up, new Color(1f, 0.85f, 0.3f, 0.9f), 12, 0.3f, 2f);
        AudioManager.Play("collect");
    }

    private void OnDrawGizmosSelected()
    {
        // A l'editor es veu el camí que farà l'animal
        if (rescuedAnimal == null || safePoint == null) return;
        Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.8f);
        Gizmos.DrawLine(rescuedAnimal.position, safePoint.position);
        Gizmos.DrawWireSphere(safePoint.position, 0.5f);
    }
}
