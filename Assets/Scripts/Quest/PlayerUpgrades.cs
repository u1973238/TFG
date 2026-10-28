using System.Collections.Generic;

// Millores de l'aranya que s'aconsegueixen lliurant missions. Els scripts de moviment hi multipliquen els seus valors,
// així les missions tenen una recompensa que es nota en jugar. Es reinicien cada vegada que comença la partida
public static class PlayerUpgrades
{
    public static float WebRangeMultiplier { get; private set; } = 1f;
    public static float ClimbSpeedMultiplier { get; private set; } = 1f;
    public static float JumpMultiplier { get; private set; } = 1f;
    public static float RunSpeedMultiplier { get; private set; } = 1f;
    public static float SwingMultiplier { get; private set; } = 1f;
    public static float GrappleMultiplier { get; private set; } = 1f;
    public static int ExtraBridges { get; private set; } = 0;
    public static bool GrappleUnlocked { get; private set; } = true;

    // Torna als valors inicials. Si alguna missió ensenya el Grappling, comença bloquejat
    public static void Reset(IEnumerable<MissionData> missions)
    {
        WebRangeMultiplier = 1f;
        ClimbSpeedMultiplier = 1f;
        JumpMultiplier = 1f;
        RunSpeedMultiplier = 1f;
        SwingMultiplier = 1f;
        GrappleMultiplier = 1f;
        ExtraBridges = 0;
        GrappleUnlocked = true;

        if (missions == null) return;
        foreach (MissionData mission in missions)
        {
            if (mission != null && mission.reward == MissionReward.UnlockGrapple) GrappleUnlocked = false;
        }
    }

    // Aplica la recompensa i retorna el text que veurà el jugador
    public static string Apply(MissionReward reward, float value)
    {
        switch (reward)
        {
            case MissionReward.LongerWeb:
                WebRangeMultiplier += value;
                return "el teu fil arriba més lluny";
            case MissionReward.FasterClimbing:
                ClimbSpeedMultiplier += value;
                return "ara escales més ràpid";
            case MissionReward.HigherJump:
                JumpMultiplier += value;
                return "ara saltes més amunt";
            case MissionReward.FasterRunning:
                RunSpeedMultiplier += value;
                return "ara corres més ràpid";
            case MissionReward.StrongerSwing:
                SwingMultiplier += value;
                return "ara et balanceges amb més força";
            case MissionReward.StrongerGrapple:
                GrappleMultiplier += value;
                return "ara recolls el fil més ràpid";
            case MissionReward.ExtraBridge:
                ExtraBridges += UnityEngine.Mathf.Max(1, UnityEngine.Mathf.RoundToInt(value));
                return "pots fer més ponts de teranyina alhora";
            case MissionReward.UnlockGrapple:
                GrappleUnlocked = true;
                return "has après el Grappling (Q / LT)!";
            default:
                return "";
        }
    }
}
