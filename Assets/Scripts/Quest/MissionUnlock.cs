using System.Collections.Generic;
using UnityEngine;

// Fa aparèixer (o desaparèixer) objectes de l'escena quan es lliura una missió, o quan s'han lliurat totes.
// Serveix per encadenar el món: per exemple, després d'ajudar un animal apareix un pont cap a una zona nova,
// un NPC nou o el repte final. Els objectes a activar s'han de deixar desactivats a l'escena
public class MissionUnlock : MonoBehaviour
{
    [Header("Condició")]
    [SerializeField] private MissionData mission; // Missió que s'ha de lliurar
    [SerializeField] private bool whenAllMissionsDelivered = false; // Si és cert, espera que s'hagin lliurat totes (ignora «Mission»)

    [Header("Què passa")]
    [SerializeField] private List<GameObject> activateObjects = new List<GameObject>();
    [SerializeField] private List<GameObject> deactivateObjects = new List<GameObject>();
    [SerializeField] private string notification = ""; // Text opcional que es mostra al Quest Tracker (ex: «S'ha obert un camí nou!»)

    private bool done = false;

    void Start()
    {
        if (MissionManager.Instance != null) MissionManager.Instance.MissionDelivered += OnMissionDelivered;
    }

    void OnDestroy()
    {
        if (MissionManager.Instance != null) MissionManager.Instance.MissionDelivered -= OnMissionDelivered;
    }

    private void OnMissionDelivered(MissionData delivered)
    {
        if (done) return;

        bool conditionMet = whenAllMissionsDelivered ? AllDelivered() : delivered == mission;
        if (!conditionMet) return;

        done = true;
        foreach (GameObject target in activateObjects) if (target != null) target.SetActive(true);
        foreach (GameObject target in deactivateObjects) if (target != null) target.SetActive(false);

        if (notification != "" && MissionManager.Instance != null) MissionManager.Instance.ShowNotification(notification);
        AudioManager.Play("mission_start");
    }

    private bool AllDelivered()
    {
        foreach (MissionData m in MissionManager.Instance.allMissions)
        {
            if (m != null && !m.isDelivered) return false;
        }
        return true;
    }
}
