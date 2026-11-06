using System.Collections;
using UnityEngine;

public class MissionCollectible : MonoBehaviour
{
    [Header("Configuració de la Missió")]
    [SerializeField] private string targetMissionID = "CollectItems";
    [SerializeField] private int value = 1;

    [Header("Efecte Visual")]
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float collectAnimationTime = 0.3f; // Durada de l'efecte de recollida abans de desaparèixer

    private bool collected = false;
    private Vector3 originalScale;
    private Quaternion originalRotation;

    // Si forma part d'un circuit (cèrcols en ordre), és el circuit qui decideix si compta
    public RingCircuit Circuit { get; set; }
    public string TargetMissionID => targetMissionID;

    void Awake()
    {
        originalScale = transform.localScale;
        originalRotation = transform.localRotation;
    }

    void Update()
    {
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;

        if (other.CompareTag("Player"))
        {
            if (Circuit != null)
            {
                Circuit.OnRingReached(this);
                return;
            }

            if (MissionManager.Instance != null)
            {
                // Intentem processar la recollida. Si el gestor diu 'true' (missió activa), l'objecte es destrueix.
                if (MissionManager.Instance.ProgressMission(targetMissionID, value))
                {
                    Collect(false);
                }
                else
                {
                    ExplainWhyNot();
                }
            }
        }
    }

    // Si no es pot recollir, diem per què (abans no passava res i semblava que l'objecte no funcionava)
    private void ExplainWhyNot()
    {
        MissionData mission = MissionManager.Instance.allMissions.Find(m => m != null && m.missionID == targetMissionID);
        if (mission == null)
        {
            Debug.LogWarning($"[MissionCollectible] {name}: cap missió de «All Missions» té l'ID «{targetMissionID}». Revisa «Target Mission ID».", this);
        }
        else if (!mission.isActive && !mission.isCompleted)
        {
            MissionManager.Instance.ShowNotification("Encara no pots agafar-ho: primer parla amb l'animal que necessita ajuda.");
        }
    }

    // Recull l'objecte amb l'efecte. Si es pot tornar a fer servir (circuit), s'amaga en lloc de destruir-se
    public void Collect(bool keepForReuse)
    {
        collected = true;
        AudioManager.Play("collect");
        Effects.Puff(transform.position, Vector3.up, new Color(1f, 0.85f, 0.3f, 0.9f), 12, 0.3f, 2f);
        StartCoroutine(CollectAnimation(keepForReuse));
    }

    // Torna a fer aparèixer l'objecte (el circuit de cèrcols es reinicia)
    public void ResetCollectible()
    {
        StopAllCoroutines();
        collected = false;
        transform.localScale = originalScale;
        transform.localRotation = originalRotation;
        foreach (Collider objectCollider in GetComponentsInChildren<Collider>(true)) objectCollider.enabled = true;
        gameObject.SetActive(true);
    }

    public Vector3 OriginalScale => originalScale;
    public bool IsCollected => collected;

    private IEnumerator CollectAnimation(bool keepForReuse)
    {
        // Desactivem els col·liders perquè no es pugui tornar a recollir mentre s'anima
        foreach (Collider objectCollider in GetComponentsInChildren<Collider>())
        {
            objectCollider.enabled = false;
        }

        Vector3 startScale = transform.localScale;
        float elapsed = 0f;

        // Fem servir temps sense escalar: si aquest objecte completa l'última missió, el joc s'atura però l'efecte s'acaba igual
        while (elapsed < collectAnimationTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / collectAnimationTime);

            // Primer creix una mica i després s'encongeix fins a desaparèixer mentre gira ràpid
            float scale = t < 0.3f ? Mathf.Lerp(1f, 1.3f, t / 0.3f) : Mathf.Lerp(1.3f, 0f, (t - 0.3f) / 0.7f);
            transform.localScale = startScale * scale;
            transform.Rotate(Vector3.up * 720f * Time.unscaledDeltaTime);
            yield return null;
        }

        if (keepForReuse) gameObject.SetActive(false);
        else Destroy(gameObject);
    }
}
