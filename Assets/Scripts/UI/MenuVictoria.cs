using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MenuVictoria : MonoBehaviour
{
    public static MenuVictoria Instance;

    [Header("UI de Victòria")]
    [SerializeField] private GameObject panellVictoria;
    [SerializeField] private TextMeshProUGUI textTemps; // Opcional: mostra el temps que s'ha tardat a acabar el joc

    private float tempsJugat = 0f;

    private void Update()
    {
        // Només compta el temps de joc: amb el menú, la pausa o la victòria el temps està aturat (deltaTime = 0)
        tempsJugat += Time.deltaTime;
    }

    private void Awake()
    {
        // Configurem la instància global per poder-lo cridar des de qualsevol lloc
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // En arrencar el joc ens assegurem que estigui amagat
        if (panellVictoria != null)
            panellVictoria.SetActive(false);
    }

    // Aquesta funció la cridarem quan es completin totes les missions
    public void MostrarPantallaVictoria()
    {
        if (panellVictoria != null)
            panellVictoria.SetActive(true);

        if (textTemps != null)
        {
            int minuts = Mathf.FloorToInt(tempsJugat / 60f);
            int segons = Mathf.FloorToInt(tempsJugat % 60f);
            textTemps.text = $"Temps: {minuts:00}:{segons:00}";
        }

        AudioManager.Play("victory");

        Time.timeScale = 0f; 
        Cursor.lockState = CursorLockMode.None; 
        Cursor.visible = true;

        // Amb comandament no hi ha cursor: seleccionem el primer botó perquè es pugui navegar amb el joystick
        if (Gamepad.current != null && EventSystem.current != null && panellVictoria != null)
        {
            Button firstButton = panellVictoria.GetComponentInChildren<Button>();
            if (firstButton != null) EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
        }
    }

    // Funció per al botó de Jugar de Nou
    public void ReiniciarJoc()
    {
        Time.timeScale = 1f; // Reactivem el temps!
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Funció per al botó de Sortir
    public void SortirDelJoc()
    {
        Debug.Log("Sortint del joc...");
        Application.Quit();
    }
}
