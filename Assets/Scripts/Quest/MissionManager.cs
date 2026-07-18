using System.Collections.Generic;
using UnityEngine;
using TMPro; // Requerit per controlar el Quest Tracker de la UI

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    [Header("UI Component")]
    [SerializeField] private TextMeshProUGUI questTrackerText; // El text del Quest Tracker de la teva pantalla

    [Header("Base de Dades de Missions")]
    public List<MissionData> allMissions; // Llista amb totes les missions del joc

    [System.NonSerialized] public string activeTimerText = ""; // Text temporal per mostrar el cronòmetre de la cursa

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Netejem els estats cada vegada que s'encén el joc per evitar dades heretades
        foreach (var m in allMissions)
        {
            m.currentAmount = 0;
            m.isActive = false;
            m.isCompleted = false;
        }
    }

    void Start()
    {
        UpdateQuestTrackerUI();
    }

    public void ActivateMission(string id)
    {
        MissionData m = allMissions.Find(x => x.missionID == id);
        if (m != null && !m.isActive && !m.isCompleted)
        {
            m.isActive = true;
            Debug.Log($"Missió activada: {m.title}");
            UpdateQuestTrackerUI();
        }
    }

    public bool ProgressMission(string id, int amount)
    {
        MissionData m = allMissions.Find(x => x.missionID == id);

        // NOMÉS si la missió existeix, està activa i NO està completada, acceptem el progrés
        if (m != null && m.isActive && !m.isCompleted)
        {
            m.currentAmount += amount;

            if (m.currentAmount >= m.requiredAmount)
            {
                m.currentAmount = m.requiredAmount;
                m.isCompleted = true;
                Debug.Log($"Missió COMPLETADA: {m.title}");
            }

            UpdateQuestTrackerUI();
            return true; // Retornem true: Progrés acceptat!
        }

        return false; // Retornem false: Missió bloquejada o inactiva!
    }


    public void UpdateQuestTrackerUI()
    {
        if (questTrackerText == null) return;

        string textVisual = "";
        bool hiHaMissionsActives = false;

        foreach (var m in allMissions)
        {
            if (m.isActive)
            {
                hiHaMissionsActives = true;
                if (m.isCompleted)
                {
                    textVisual += $"<s>• {m.title} (Fet!)</s>\n";
                }
                else
                {
                    // Mostrem el títol i el progrés normal
                    textVisual += $"• {m.title}: {m.currentAmount} / {m.requiredAmount}";

                    // NOU: Si és la cursa vertical i tenim el rellotge actiu, l'afegim al costat!
                    if (m.missionID == "Cursa_vertical" && !string.IsNullOrEmpty(activeTimerText))
                    {
                        textVisual += $" ({activeTimerText})";
                    }

                    textVisual += "\n";
                }
            }
        }

        if (!hiHaMissionsActives)
        {
            textVisual = "Explora l'illa i ajuda els animals.";
        }

        questTrackerText.text = textVisual;
    }

}
