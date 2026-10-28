using UnityEngine;

// Animació procedural del cos de l'aranya (el model és una sola peça, sense ossos):
// bota una mica mentre camina o escala, s'aixafa en aterrar, s'estira en saltar i respira quan està quieta.
// Només toca la posició i l'escala del model; la rotació la continua controlant SpiderClimbing
public class SpiderBodyAnimator : MonoBehaviour
{
    [Header("Caminar")]
    public float stepBounce = 0.06f; // Alçada del petit bot de cada passa (metres)
    public float stepFrequency = 1.6f; // Passes per metre recorregut

    [Header("Aixafar i estirar")]
    public float landSquash = 0.28f; // Quant s'aixafa en aterrar d'una caiguda forta
    public float jumpStretch = 0.18f; // Quant s'estira en saltar
    public float springStiffness = 180f;
    public float springDamping = 14f;

    [Header("Repòs")]
    public float breathAmount = 0.02f;
    public float breathSpeed = 2f;

    private Transform mesh;
    private Vector3 baseLocalPosition;
    private Vector3 baseLocalScale;
    private Vector3 lastPosition;
    private float stepPhase;
    private float bounceWeight; // 0 = quieta, 1 = movent-se (per passar suaument d'un estat a l'altre)
    private float squash; // Positiu = estirada, negatiu = aixafada
    private float squashVelocity;

    void Start()
    {
        SpiderClimbing climbing = GetComponent<SpiderClimbing>();
        if (climbing != null) mesh = climbing.CharacterMesh;
        if (mesh == null || mesh == transform)
        {
            enabled = false; // Sense un model fill no hi ha res a animar
            return;
        }

        baseLocalPosition = mesh.localPosition;
        baseLocalScale = mesh.localScale;
        lastPosition = transform.position;
    }

    public void OnLand(float intensity)
    {
        squashVelocity -= landSquash * 12f * Mathf.Clamp01(intensity);
    }

    public void OnJump()
    {
        squashVelocity += jumpStretch * 12f;
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // Velocitat real de l'aranya (amb MovePosition, la velocitat del Rigidbody no inclou el moviment de caminar)
        Vector3 delta = transform.position - lastPosition;
        lastPosition = transform.position;
        float speed = delta.magnitude / dt;

        // Bot de les passes: com més ràpid va, més seguides
        bool moving = speed > 0.5f && speed < 30f;
        bounceWeight = Mathf.MoveTowards(bounceWeight, moving ? 1f : 0f, dt * 5f);
        if (moving) stepPhase += speed * stepFrequency * dt * Mathf.PI;
        float bounce = Mathf.Abs(Mathf.Sin(stepPhase)) * stepBounce * bounceWeight;

        // Molla que torna l'escala a la normalitat després d'aixafar-se o estirar-se
        float springForce = -springStiffness * squash - springDamping * squashVelocity;
        squashVelocity += springForce * dt;
        squash += squashVelocity * dt;
        squash = Mathf.Clamp(squash, -0.5f, 0.5f);

        // Respiració quan està quieta
        float breath = Mathf.Sin(Time.time * breathSpeed) * breathAmount * (1f - bounceWeight);

        // Conservem el volum: si s'aixafa d'alt, s'eixampla pels costats
        float vertical = 1f + squash + breath;
        float sideways = 1f / Mathf.Sqrt(Mathf.Max(0.2f, vertical));
        mesh.localScale = new Vector3(baseLocalScale.x * sideways, baseLocalScale.y * vertical, baseLocalScale.z * sideways);

        // El bot es fa en l'eix «amunt» de l'aranya (també quan escala parets i sostres)
        Vector3 localUp = mesh.parent != null ? mesh.parent.InverseTransformDirection(transform.up) : Vector3.up;
        mesh.localPosition = baseLocalPosition + localUp * bounce;
    }
}
