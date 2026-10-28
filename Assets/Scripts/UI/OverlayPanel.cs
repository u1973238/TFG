using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Pantalla que s'obre per sobre d'una altra (Controls i Opcions) i, en tancar-la, torna a la pantalla d'on venia.
// Es pot obrir des del menú principal o des de la pausa
public class OverlayPanel : MonoBehaviour
{
    private static readonly List<OverlayPanel> openPanels = new List<OverlayPanel>();

    private GameObject returnPanel;

    // Obre aquesta pantalla i amaga la que hi havia (es crida des dels botons)
    public void OpenFrom(GameObject fromPanel)
    {
        returnPanel = fromPanel;
        if (returnPanel != null) returnPanel.SetActive(false);

        gameObject.SetActive(true);
        openPanels.Remove(this);
        openPanels.Add(this);
        SelectFirst(gameObject);
    }

    // Tanca aquesta pantalla i torna a l'anterior (botó «Tornar»)
    public void Close()
    {
        gameObject.SetActive(false);
        openPanels.Remove(this);

        if (returnPanel != null)
        {
            returnPanel.SetActive(true);
            SelectFirst(returnPanel);
        }
    }

    // Tanca la pantalla de més amunt, si n'hi ha alguna. Retorna cert si n'ha tancat una (Esc o el botó B fan de «Tornar»)
    public static bool CloseTop()
    {
        if (openPanels.Count == 0) return false;
        openPanels[openPanels.Count - 1].Close();
        return true;
    }

    // Tanca totes les pantalles sense tornar a l'anterior (per exemple, en continuar la partida)
    public static void CloseAllSilently()
    {
        foreach (OverlayPanel panel in openPanels)
        {
            if (panel != null) panel.gameObject.SetActive(false);
        }
        openPanels.Clear();
    }

    // Amb comandament no hi ha cursor: seleccionem el primer element perquè es pugui navegar amb el joystick
    public static void SelectFirst(GameObject panel)
    {
        if (Gamepad.current == null || EventSystem.current == null || panel == null) return;

        Selectable first = panel.GetComponentInChildren<Selectable>();
        if (first != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
    }

    void Update()
    {
        // El botó B del comandament també fa de «Tornar»
        bool isTop = openPanels.Count > 0 && openPanels[openPanels.Count - 1] == this;
        if (isTop && Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
        {
            Close();
        }
    }

    void OnDestroy()
    {
        openPanels.Remove(this);
    }
}
