using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// S'executa després de ThirdPersonCamera, que bloqueja el cursor al seu Start, perquè el menú el pugui alliberar
[DefaultExecutionOrder(100)]
public class MainMenu : MonoBehaviour
{
    [Header("UI Component")]
    [SerializeField] private GameObject mainMenuPanel; // El panell del menú d'inici
    [SerializeField] private GameObject controlsPanel; // El panell informatiu amb els controls
    [SerializeField] private Canvas hudCanvas; // El HUD del joc (missions i punt de mira): no es veu mentre hi ha el menú d'inici

    void Start()
    {
        // En arrencar l'escena, mostrem el menú d'inici i congelem el joc
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }

        if (hudCanvas != null)
        {
            hudCanvas.enabled = false;
        }

        if (controlsPanel != null)
        {
            controlsPanel.SetActive(false);
        }

        Time.timeScale = 0f; // Congelem la física i el temps del joc
        Cursor.lockState = CursorLockMode.None; // Alliberem el ratolí
        Cursor.visible = true; // Fem visible el cursor per poder clicar

        SelectFirstButton(mainMenuPanel);
    }

    // Aquest mètode es cridarà quan el jugador premi el botó 'Start'
    public void StartGame()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false); // Amaguem el menú d'inici
        }

        if (controlsPanel != null)
        {
            controlsPanel.SetActive(false);
        }

        if (hudCanvas != null)
        {
            hudCanvas.enabled = true;
        }

        Time.timeScale = 1f; // Reactivem el temps del joc
        Cursor.lockState = CursorLockMode.Locked; // Bloquegem el ratolí al centre per controlar la càmera
        Cursor.visible = false; // Amaguem el cursor
    }

    // Aquest mètode es cridarà quan el jugador premi el botó 'Controls'
    public void ShowControls()
    {
        if (controlsPanel != null)
        {
            controlsPanel.SetActive(true);
        }

        SelectFirstButton(controlsPanel);
    }

    // Aquest mètode es cridarà quan el jugador premi el botó 'Tornar' del panell de controls
    public void HideControls()
    {
        if (controlsPanel != null)
        {
            controlsPanel.SetActive(false);
        }

        SelectFirstButton(mainMenuPanel);
    }

    // Amb comandament no hi ha cursor: seleccionem el primer botó perquè es pugui navegar amb el joystick
    private void SelectFirstButton(GameObject panel)
    {
        if (Gamepad.current == null || EventSystem.current == null || panel == null) return;

        Button firstButton = panel.GetComponentInChildren<Button>();
        if (firstButton != null) EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
    }

    // Aquest mètode es cridarà quan el jugador premi el botó 'Quit'
    public void QuitGame()
    {
        Debug.Log("Sortint del joc...");
        Application.Quit(); // Tanca l'executable del joc
    }
}


