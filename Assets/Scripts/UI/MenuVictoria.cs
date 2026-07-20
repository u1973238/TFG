using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuVictoria : MonoBehaviour
{
    public static MenuVictoria Instance;

    [Header("UI de Victòria")]
    [SerializeField] private GameObject panellVictoria;

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

        Time.timeScale = 0f; 
        Cursor.lockState = CursorLockMode.None; 
        Cursor.visible = true;
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
