using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class PauseMenu : MonoBehaviour
{
    [Header("Arquitectura d'Inputs")]
    [SerializeField] private InputReader inputReader;

    [Header("Referències de UI")]
    [SerializeField] private GameObject pausePanel;

    private bool isPaused = false;


    private void OnEnable()
    {
        if (inputReader != null)
        {
            inputReader.pauseMenuEvent += TogglePause;
        }
    }

    private void OnDisable()
    {
        if (inputReader != null)
        {
            inputReader.pauseMenuEvent -= TogglePause;
        }
    }

    void Start()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void ResumeGame()
    {
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void PauseGame()
    {
        pausePanel.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}