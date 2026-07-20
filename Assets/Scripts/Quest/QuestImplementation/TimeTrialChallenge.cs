using UnityEngine;

public class TimeTrialChallenge : MonoBehaviour
{
    [Header("Configuració del Repte")]
    [SerializeField] private string timeTrialMissionID = "cursa_vertical";
    [SerializeField] private float timeLimit = 30f;

    private float currentTime;
    private bool timerActive = false;
    private bool challengeCompleted = false;

    void Update()
    {
        if (timerActive && !challengeCompleted)
        {
            currentTime -= Time.deltaTime;

            if (currentTime <= 0)
            {
                currentTime = 0;
                timerActive = false;
                TimeTrialFailed();
            }

            DisplayTimerUI();
        }
    }

    // Aquest mètode s'executarà de forma automàtica des del MissionNPC en acceptar la cursa
    public void StartChallenge()
    {
        if (challengeCompleted) return;

        currentTime = timeLimit;
        timerActive = true;
        challengeCompleted = false;
        //Debug.Log("Cursa contrarrellotge Iniciada! Puja al cim!");

        if (MissionManager.Instance != null)
        {
            //MissionManager.Instance.ActivateMission(timeTrialMissionID);
        }
    }

    private void DisplayTimerUI()
    {
        if (MissionManager.Instance != null)
        {
            // Enviem el temps formatat (amb un decimal) al MissionManager i actualitzem la UI
            MissionManager.Instance.activeTimerText = timerActive ? $"{currentTime:F1}s" : "Temps esgotat!";
            MissionManager.Instance.UpdateQuestTrackerUI();
        }
    }


    private void TimeTrialFailed()
    {
        //Debug.LogError("No has arribat a temps! Repte fallit.");
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.activeTimerText = "Fallit!";
            MissionManager.Instance.UpdateQuestTrackerUI();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && timerActive && !challengeCompleted)
        {
            challengeCompleted = true;
            timerActive = false;
            Debug.Log("Has arribat al cim a temps! Cursa completada.");

            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.activeTimerText = "";
                MissionManager.Instance.ProgressMission(timeTrialMissionID, 1);
            }
        }
    }

}

