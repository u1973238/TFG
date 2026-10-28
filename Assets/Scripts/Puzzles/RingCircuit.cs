using System.Collections.Generic;
using UnityEngine;

// Circuit aeri de cèrcols: s'han de travessar EN ORDRE i sense tocar terra.
// El cèrcol següent fa pampallugues perquè el jugador sàpiga cap on anar; si l'aranya toca terra a mig circuit,
// tots els cèrcols tornen a aparèixer i el comptador de la missió torna a zero
public class RingCircuit : MonoBehaviour
{
    [Header("Missió")]
    [SerializeField] private MissionData mission;

    [Header("Cèrcols (en ordre)")]
    [SerializeField] private List<MissionCollectible> rings = new List<MissionCollectible>();

    [Header("Regles")]
    [SerializeField] private bool resetOnGround = true; // Tocar terra a mig circuit el reinicia
    [SerializeField] private float groundGraceTime = 0.25f; // Temps a terra que es perdona (un toc molt curt no compta)

    [Header("Indicador del cèrcol següent")]
    [SerializeField] private float pulseAmount = 0.15f;
    [SerializeField] private float pulseSpeed = 4f;

    private int nextIndex = 0;
    private float groundedTime = 0f;
    private SpiderLocomotion player;

    void Start()
    {
        foreach (MissionCollectible ring in rings)
        {
            if (ring != null) ring.Circuit = this;
        }

        player = FindAnyObjectByType<SpiderLocomotion>();

        if (mission != null && mission.requiredAmount != rings.Count)
        {
            Debug.LogWarning($"[RingCircuit] La missió «{mission.title}» demana {mission.requiredAmount} però el circuit té {rings.Count} cèrcols.", this);
        }
    }

    // Un cèrcol avisa que l'aranya l'ha travessat
    public void OnRingReached(MissionCollectible ring)
    {
        int index = rings.IndexOf(ring);
        if (index != nextIndex) return; // No és el que toca: no compta

        // Si la missió encara no està activa (no s'ha parlat amb l'NPC), no comença el circuit
        if (MissionManager.Instance == null || !MissionManager.Instance.ProgressMission(mission, 1)) return;

        ring.Collect(true);
        nextIndex++;
    }

    void Update()
    {
        if (nextIndex >= rings.Count || mission == null) return;

        // El cèrcol que toca fa pampallugues (només quan la missió està activa)
        MissionCollectible next = rings[nextIndex];
        if (next != null && mission.isActive && !next.IsCollected)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            next.transform.localScale = next.OriginalScale * pulse;
        }

        // Tocar terra a mig circuit el reinicia
        bool started = nextIndex > 0;
        if (resetOnGround && started && player != null && player.enabled && player.IsGrounded)
        {
            groundedTime += Time.deltaTime;
            if (groundedTime > groundGraceTime) ResetCircuit();
        }
        else
        {
            groundedTime = 0f;
        }
    }

    private void ResetCircuit()
    {
        nextIndex = 0;
        groundedTime = 0f;
        foreach (MissionCollectible ring in rings)
        {
            if (ring != null) ring.ResetCollectible();
        }

        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.ResetProgress(mission);
            MissionManager.Instance.ShowNotification("Has tocat terra! El circuit torna a començar.");
        }
        AudioManager.Play("fail");
    }
}
