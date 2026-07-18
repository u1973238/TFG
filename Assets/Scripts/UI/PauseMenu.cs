using UnityEngine;
using UnityEngine.SceneManagement; // IMPRESCINDIBLE per poder reiniciar la partida

public class PauseMenu : MonoBehaviour
{
    [Header("Referències de UI")]
    [SerializeField] private GameObject pausePanel; // El panell gris de fons amb els botons

    private bool isPaused = false;

    void Start()
    {
        // Ens assegurem que el menú comenci tancat en arrencar el joc
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    void Update()
    {
        // Escoltem si el jugador prem la tecla ESCAPAT (Escape)
        if (Input.GetKeyDown(KeyCode.Escape))
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
    }

    // Funció per treure la pausa
    public void ResumeGame()
    {
        pausePanel.SetActive(false);
        Time.timeScale = 1f; // El temps torna a córrer normalment
        isPaused = false;

        // Tornem a bloquejar i amagar el ratolí perquè l'aranya pugui girar la càmera
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Funció per posar la pausa
    public void PauseGame()
    {
        pausePanel.SetActive(true);
        Time.timeScale = 0f; // El joc es congela completament!
        isPaused = true;

        // Alliberem el ratolí i el fem visible perquè el jugador pugui clicar els botons
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Funció per al botó de Reiniciar
    public void RestartLevel()
    {
        // Alerta: Sempre s'ha de reactivar el temps abans de carregar una escena, 
        // si no, la nova escena començarà congelada a zero!
        Time.timeScale = 1f;

        // Carrega exactament la mateixa escena on estàs jugant ara mateix
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Funció per tancar el joc
    public void QuitGame()
    {
        Debug.Log("Sortint del joc... (Això només funciona en el joc exportat .exe)");
        Application.Quit(); // Tanca l'aplicació
    }
}

