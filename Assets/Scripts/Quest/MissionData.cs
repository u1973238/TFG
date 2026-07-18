using UnityEngine;

[CreateAssetMenu(fileName = "NovaMissio", menuName = "Missions/MissionData")]
public class MissionData : ScriptableObject
{
    public string missionID; // Identificador únic (ex: "recollir_pomes")
    public string title;     // Títol que veurà el jugador (ex: "La xarxa de salvament")
    [TextArea] public string description; // Descripció llarga de la missió

    public int requiredAmount; // Quants objectes/accions demana la missió (ex: 4)

    // Variables temporals en temps d'execució (no es guarden en el disc)
    [System.NonSerialized] public int currentAmount = 0;
    [System.NonSerialized] public bool isActive = false;
    [System.NonSerialized] public bool isCompleted = false;
}
