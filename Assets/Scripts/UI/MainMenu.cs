using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [Header("UI Component")]
    [SerializeField] private GameObject mainMenuPanel; // El panell del menú d'inici

    void Start()
    {
        // En arrencar l'escena, mostrem el menú d'inici i congelem el joc
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }

        Time.timeScale = 0f; // Congelem la física i el temps del joc
        Cursor.lockState = CursorLockMode.None; // Alliberem el ratolí
        Cursor.visible = true; // Fem visible el cursor per poder clicar
    }

    // Aquest mètode es cridarà quan el jugador premi el botó 'Start'
    public void StartGame()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false); // Amaguem el menú d'inici
        }

        Time.timeScale = 1f; // Reactivem el temps del joc
        Cursor.lockState = CursorLockMode.Locked; // Bloquegem el ratolí al centre per controlar la càmera
        Cursor.visible = false; // Amaguem el cursor
    }

    // Aquest mètode es cridarà quan el jugador premi el botó 'Quit'
    public void QuitGame()
    {
        Debug.Log("Sortint del joc...");
        Application.Quit(); // Tanca l'executable del joc
    }
}


