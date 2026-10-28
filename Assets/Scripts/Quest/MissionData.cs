using System.Collections.Generic;
using UnityEngine;

// Recompenses que dona una missió en lliurar-la a l'NPC (milloren les habilitats de l'aranya)
public enum MissionReward
{
    None = 0,
    LongerWeb = 1,       // El fil del Swing i del Grappling arriba més lluny
    FasterClimbing = 2,  // Escala més ràpid
    HigherJump = 3,      // Salta més amunt
    FasterRunning = 4,   // Camina i corre més ràpid
    StrongerSwing = 5,   // Impulsa més el balanceig amb WASD
    StrongerGrapple = 6, // Recull el fil més ràpid
    ExtraBridge = 7,     // Pot tenir un pont més a la vegada (si hi ha límit de ponts)
    UnlockGrapple = 8,   // Aprèn el Grappling (fins llavors no el pot fer servir)
}

[CreateAssetMenu(fileName = "NovaMissio", menuName = "Missions/MissionData")]
public class MissionData : ScriptableObject
{
    public string missionID; // Identificador únic (ex: "recollir_pomes")
    public string title;     // Títol que veurà el jugador (ex: "La xarxa de salvament")
    [TextArea] public string description; // Descripció llarga de la missió

    public int requiredAmount; // Quants objectes/accions demana la missió (ex: 4)

    [Header("Lliurament i recompensa")]
    public bool requiresHandIn = true; // Si és cert, en complir l'objectiu cal tornar a parlar amb l'NPC per acabar-la
    public MissionReward reward = MissionReward.None;
    public float rewardValue = 0.25f; // Quant millora (0.25 = un 25% més); per a ExtraBridge, quants ponts

    [Header("Cadena de missions")]
    public List<MissionData> requiredMissions = new List<MissionData>(); // Missions que s'han de lliurar abans de poder acceptar aquesta

    // Variables temporals en temps d'execució (no es guarden en el disc)
    [System.NonSerialized] public int currentAmount = 0;
    [System.NonSerialized] public bool isActive = false;
    [System.NonSerialized] public bool isCompleted = false; // L'objectiu està complert
    [System.NonSerialized] public bool isDelivered = false; // S'ha lliurat a l'NPC (missió acabada del tot)

    // Ja s'ha complert l'objectiu però encara cal tornar a l'NPC
    public bool IsReadyToDeliver => isCompleted && !isDelivered;
}
