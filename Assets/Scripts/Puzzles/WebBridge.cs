using UnityEngine;

// Pont de teranyina creat pel WebConnector. Dibuixa els fils (que es despleguen en crear-se),
// tremola quan algú hi cau a sobre i fa de llit elàstic: si l'aranya hi cau des de prou amunt, surt rebotada
public class WebBridge : MonoBehaviour
{
    private LineRenderer[] lines;
    private Vector3[][] basePoints;
    private Vector3 origin;
    private Vector3 end;
    private float sag;
    private float growDuration;
    private float createdTime;

    private bool bouncy;
    private float bounceThreshold;
    private float bounciness;
    private float maxBounceSpeed;

    // Tremolor: s'enfonsa pel mig i torna a lloc oscil·lant
    private float wobbleAmplitude = 0f;
    private float wobbleTime = 0f;
    private const float WobbleFrequency = 9f;
    private const float WobbleDamping = 4f;

    private bool needsRedraw = true;

    // Els dos extrems del pont (on s'han enganxat els fils)
    public Vector3 StartPoint => origin;
    public Vector3 EndPoint => end;

    public void Init(LineRenderer[] bridgeLines, Vector3[][] points, Vector3 startPoint, Vector3 endPoint, float sagAmount, float duration,
                     bool isBouncy, float threshold, float bounceFactor, float maxSpeed)
    {
        lines = bridgeLines;
        basePoints = points;
        origin = startPoint;
        end = endPoint;
        sag = sagAmount;
        growDuration = Mathf.Max(0.01f, duration);
        createdTime = Time.time;

        bouncy = isBouncy;
        bounceThreshold = threshold;
        bounciness = bounceFactor;
        maxBounceSpeed = maxSpeed;

        for (int l = 0; l < lines.Length; l++) lines[l].positionCount = basePoints[l].Length;
        Redraw();
    }

    void Update()
    {
        if (lines == null) return;

        bool growing = Time.time - createdTime < growDuration;
        bool wobbling = wobbleAmplitude > 0.001f;

        if (wobbling)
        {
            wobbleTime += Time.deltaTime;
            wobbleAmplitude *= Mathf.Exp(-WobbleDamping * Time.deltaTime);
        }

        // Només recalculem els punts quan cal (mentre es desplega o tremola)
        if (growing || wobbling || needsRedraw) Redraw();
    }

    private void Redraw()
    {
        float progress = Mathf.Clamp01((Time.time - createdTime) / growDuration);
        float eased = 1f - (1f - progress) * (1f - progress);
        float wobble = wobbleAmplitude * Mathf.Cos(wobbleTime * WobbleFrequency * 2f * Mathf.PI / 3f);
        Vector3 down = -transform.up;

        for (int l = 0; l < lines.Length; l++)
        {
            Vector3[] points = basePoints[l];
            for (int i = 0; i < points.Length; i++)
            {
                float t = points.Length > 1 ? (float)i / (points.Length - 1) : 0f;
                Vector3 point = Vector3.LerpUnclamped(origin, points[i], eased);
                point += down * Mathf.Sin(t * Mathf.PI) * wobble;
                lines[l].SetPosition(i, point);
            }
        }

        needsRedraw = progress < 1f || Mathf.Abs(wobble) > 0.001f;
    }

    // Punt del mig del pont, per on es camina: t = 0 a l'inici i t = 1 al final.
    // Segueix la corba dels fils i el tremolor del moment, perquè qui hi camina es mogui amb la teranyina
    public Vector3 PointAt(float t)
    {
        t = Mathf.Clamp01(t);
        float hang = 4f * t * (1f - t) * sag;
        float wobble = wobbleAmplitude * Mathf.Cos(wobbleTime * WobbleFrequency * 2f * Mathf.PI / 3f);
        return Vector3.Lerp(origin, end, t) - transform.up * (hang + Mathf.Sin(t * Mathf.PI) * wobble);
    }

    // Fa tremolar el pont (per exemple, a cada pas d'un animal que el creua)
    public void Wobble(float amount)
    {
        if (amount <= wobbleAmplitude) return;
        wobbleAmplitude = amount;
        wobbleTime = 0f;
        needsRedraw = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Rigidbody body = collision.rigidbody;
        if (body == null) return;

        // Velocitat amb què ha arribat (abans del xoc)
        float impactSpeed = Mathf.Abs(Vector3.Dot(collision.relativeVelocity, transform.up));
        wobbleAmplitude = Mathf.Clamp(impactSpeed * 0.04f, 0.05f, 0.8f);
        wobbleTime = 0f;
        needsRedraw = true;

        // Llit elàstic: només per a l'aranya, si el pont és prou horitzontal, si cau des de sobre i prou de pressa
        bool isPlayer = collision.collider.CompareTag("Player");
        bool horizontalEnough = Vector3.Dot(transform.up, Vector3.up) > 0.7f;
        bool fromAbove = body.position.y > transform.position.y;
        if (bouncy && isPlayer && horizontalEnough && fromAbove && impactSpeed > bounceThreshold)
        {
            float bounceSpeed = Mathf.Min(impactSpeed * bounciness + 2f, maxBounceSpeed);
            Vector3 velocity = body.linearVelocity;
            velocity.y = bounceSpeed;
            body.linearVelocity = velocity;

            wobbleAmplitude = 0.9f;
            AudioManager.Play("bounce");
            Effects.Puff(collision.GetContact(0).point, Vector3.up, Color.white, 14);
        }
    }
}
