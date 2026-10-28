using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    [Header("UI Component")]
    [SerializeField] private TextMeshProUGUI questTrackerText; 

    [Header("Base de Dades de Missions")]
    public List<MissionData> allMissions; 

    [System.NonSerialized] public string activeTimerText = ""; 

    [Header("Notificacions")]
    [SerializeField] private float notificationDuration = 3f; // Segons que es mostra «Nova missió» o «Missió completada»

    // Esdeveniments perquè altres scripts reaccionin a les missions sense haver-les de consultar a cada fotograma
    public event System.Action<MissionData> MissionActivated;
    public event System.Action<MissionData> MissionCompleted; // Objectiu complert (pot faltar el lliurament)
    public event System.Action<MissionData> MissionDelivered; // Lliurada a l'NPC: missió acabada del tot

    private string notificationText = "";
    private float notificationEndTime;

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
            m.isDelivered = false;
        }

        PlayerUpgrades.Reset(allMissions);
    }

    void Start()
    {
        UpdateQuestTrackerUI();
    }

    void Update()
    {
        // Quan s'acaba el temps de la notificació, la traiem del Quest Tracker
        if (notificationText != "" && Time.unscaledTime >= notificationEndTime)
        {
            notificationText = "";
            UpdateQuestTrackerUI();
        }
    }

    public void ShowNotification(string text)
    {
        notificationText = text;
        notificationEndTime = Time.unscaledTime + notificationDuration;
        UpdateQuestTrackerUI();
    }

    // --- Cadena de missions ---

    // Una missió es pot acceptar si totes les que demana ja s'han lliurat
    public bool CanAccept(MissionData mission)
    {
        return mission != null && GetMissingRequirements(mission).Count == 0;
    }

    public List<MissionData> GetMissingRequirements(MissionData mission)
    {
        List<MissionData> missing = new List<MissionData>();
        if (mission == null || mission.requiredMissions == null) return missing;
        foreach (MissionData required in mission.requiredMissions)
        {
            if (required != null && !required.isDelivered) missing.Add(required);
        }
        return missing;
    }

    // --- Activar, avançar i lliurar ---

    public void ActivateMission(string id)
    {
        MissionData m = allMissions.Find(x => x.missionID == id);
        if (m != null && !m.isActive && !m.isCompleted && CanAccept(m))
        {
            m.isActive = true;
            //Debug.Log($"Missió activada: {m.title}");
            ShowNotification($"Nova missió: {m.title}");
            AudioManager.Play("mission_start");
            MissionActivated?.Invoke(m);
            UpdateQuestTrackerUI();
        }
    }

    // Versions que reben directament l'asset de la missió (més segures: no hi pot haver errors d'escriptura en l'id)
    public void ActivateMission(MissionData mission)
    {
        if (mission != null) ActivateMission(mission.missionID);
    }

    public bool ProgressMission(MissionData mission, int amount)
    {
        return mission != null && ProgressMission(mission.missionID, amount);
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
                //Debug.Log($"Missió COMPLETADA: {m.title}");
                AudioManager.Play("mission_complete");
                MissionCompleted?.Invoke(m);

                if (m.requiresHandIn)
                {
                    ShowNotification($"Objectiu complert: {m.title}! Torna a parlar amb qui te l'ha demanat.");
                }
                else
                {
                    DeliverMission(m);
                }
            }

            UpdateQuestTrackerUI();
            return true; // Retornem true: Progrés acceptat!
        }

        return false; // Retornem false: Missió bloquejada o inactiva!
    }

    // Lliura la missió a l'NPC: s'acaba del tot i dona la recompensa. Retorna el text de la recompensa (buit si no n'hi ha)
    public bool DeliverMission(MissionData m, out string rewardText)
    {
        rewardText = "";
        if (m == null || !m.isCompleted || m.isDelivered) return false;

        m.isDelivered = true;
        rewardText = PlayerUpgrades.Apply(m.reward, m.rewardValue);

        string message = $"Missió lliurada: {m.title}!";
        if (rewardText != "") message += $"\nRecompensa: {rewardText}";
        ShowNotification(message);

        MissionDelivered?.Invoke(m);
        UpdateQuestTrackerUI();
        ComprovarVictoriaGlobal();
        return true;
    }

    public bool DeliverMission(MissionData m)
    {
        return DeliverMission(m, out _);
    }

    // Torna el comptador a zero (per exemple, si es toca terra durant el circuit de cèrcols)
    public void ResetProgress(MissionData mission)
    {
        MissionData m = mission != null ? allMissions.Find(x => x.missionID == mission.missionID) : null;
        if (m == null || m.isCompleted) return;

        m.currentAmount = 0;
        UpdateQuestTrackerUI();
    }

    private void ComprovarVictoriaGlobal()
    {
        if (allMissions == null || allMissions.Count == 0) return;
        foreach (var m in allMissions)
        {
            // Si trobem ALMENYS UNA missió que encara no està lliurada, sortim
            if (!m.isDelivered) return;
        }
        // Si el bucle acaba i TOTES les missions estan lliurades:
        //Debug.Log("TOTES LES MISSIONS COMPLETADES");

        if (MenuVictoria.Instance != null)
        {
            MenuVictoria.Instance.MostrarPantallaVictoria();
        }
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
                if (m.isDelivered)
                {
                    textVisual += $"<s>• {m.title} (Fet!)</s>\n";
                }
                else if (m.isCompleted)
                {
                    // Objectiu complert: cal tornar a l'NPC
                    textVisual += $"<color=#FFD54A>• {m.title}: torna-hi per lliurar-la</color>\n";
                }
                else
                {
                    // Mostrem el títol i el progrés normal
                    textVisual += $"• {m.title}: {m.currentAmount} / {m.requiredAmount}";

                    // Si és la cursa vertical i tenim el rellotge actiu, l'afegim al costat!
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

        // Si hi ha una notificació recent, la mostrem destacada a dalt de tot
        if (notificationText != "")
        {
            textVisual = $"<color=#FFD54A><b>{notificationText}</b></color>\n" + textVisual;
        }

        questTrackerText.text = textVisual;
    }

}
