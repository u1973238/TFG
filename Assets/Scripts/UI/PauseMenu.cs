using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
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
        // Si hi ha una pantalla de Controls o Opcions oberta, Esc fa de «Tornar»
        if (OverlayPanel.CloseTop()) return;

        // Si el joc ja està aturat per una altra pantalla (menú inicial o victòria), no obrim la pausa
        if (!isPaused && Time.timeScale == 0f) return;

        AudioManager.Play("ui_click", 0.8f, 0f);

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
        OverlayPanel.CloseAllSilently();
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

        SelectFirstButton(pausePanel);
    }

    // Amb comandament no hi ha cursor: seleccionem el primer botó perquè es pugui navegar amb el joystick
    private void SelectFirstButton(GameObject panel)
    {
        if (Gamepad.current == null || EventSystem.current == null || panel == null) return;

        Button firstButton = panel.GetComponentInChildren<Button>();
        if (firstButton != null) EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
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