using UnityEngine;

public class TimeTrialChallenge : MonoBehaviour
{
    [Header("Configuració del Repte")]
    [SerializeField] private string timeTrialMissionID = "Cursa_vertical";
    [SerializeField] private float timeLimit = 30f;

    private float currentTime;
    private bool timerActive = false;
    private bool challengeCompleted = false;
    private int lastDisplayedTenths = -2; // Últimes dècimes mostrades; així només refresquem el text quan canvia

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
        lastDisplayedTenths = -2;
        AudioManager.Play("race_start");
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
            // Només refresquem el Quest Tracker quan canvia el valor mostrat (10 cops per segon en lloc de cada fotograma)
            int tenths = timerActive ? Mathf.RoundToInt(currentTime * 10f) : -1;
            if (tenths == lastDisplayedTenths) return;
            lastDisplayedTenths = tenths;

            // Enviem el temps formatat (amb un decimal) al MissionManager i actualitzem la UI
            MissionManager.Instance.activeTimerText = timerActive ? $"{tenths / 10f:F1}s" : "Temps esgotat!";
            MissionManager.Instance.UpdateQuestTrackerUI();
        }
    }


    private void TimeTrialFailed()
    {
        //Debug.LogError("No has arribat a temps! Repte fallit.");
        AudioManager.Play("fail");
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
            timerActive = false;

            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.activeTimerText = "";

                // Només donem la cursa per completada si el gestor accepta el progrés de la missió
                challengeCompleted = MissionManager.Instance.ProgressMission(timeTrialMissionID, 1);
            }
        }
    }

}

