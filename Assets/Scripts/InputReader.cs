using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "InputReader", menuName = "Game/Input Reader")]
public class InputReader : ScriptableObject, GameInput.IPlayerControlsActions
{
    public event UnityAction<Vector2> moveEvent;
    public event UnityAction<Vector2> lookEvent;
    public event UnityAction jumpEvent;
    public event UnityAction sprintStartedEvent;
    public event UnityAction sprintCanceledEvent;

    public event UnityAction swingStartedEvent;
    public event UnityAction swingCanceledEvent;
    public event UnityAction connectEvent;

    public event UnityAction interactEvent;

    public event System.Action pauseMenuEvent;

    private GameInput gameInput;

    public bool IsSpacePressed {  get; private set; }

    private void OnEnable()
    {
        // Inicialitza el GameInput si encara no s'ha fet
        if (gameInput == null)
        {
            gameInput = new GameInput();
            gameInput.PlayerControls.SetCallbacks(this);
        }
        gameInput.PlayerControls.Enable();
    }

    private void OnDisable()
    {
        // Desactiva els controls quan l'objecte ScriptableObject es desactiva
        gameInput.PlayerControls.Disable();
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        // Invoca l'esdeveniment de pausa només quan l'acció comença
        if (context.started)
        {
            pauseMenuEvent?.Invoke();
        }
    }

    public void OnMovement(InputAction.CallbackContext context)
    {
        // Invoca l'esdeveniment de moviment amb el valor del vector només quan hi ha un esdeveniment de moviment
        if (moveEvent != null) moveEvent.Invoke(context.ReadValue<Vector2>());
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        // Invoca l'esdeveniment de mirada amb el valor del vector només quan hi ha un esdeveniment de mirada
        if (lookEvent != null) lookEvent.Invoke(context.ReadValue<Vector2>());
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        // Invoca l'esdeveniment de salt només quan l'acció comença
        if (context.phase == InputActionPhase.Started && jumpEvent != null)
        {
            jumpEvent.Invoke();
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        // Invoca els esdeveniments de començar i cancel·lar l'esprint segons la fase de l'acció
        if (context.phase == InputActionPhase.Started && sprintStartedEvent != null)
        {
            sprintStartedEvent.Invoke();
        }
        else if (context.phase == InputActionPhase.Canceled && sprintCanceledEvent != null)
        {
            sprintCanceledEvent.Invoke();
        }
    }

    public void OnSwing(InputAction.CallbackContext context)
    {
        // Invoca els esdeveniments de començar i cancel·lar el balanceig segons la fase de l'acció
        if (context.phase == InputActionPhase.Started && swingStartedEvent != null)
        {
            swingStartedEvent.Invoke();
        }
        else if (context.phase == InputActionPhase.Canceled && swingCanceledEvent != null)
        {
            swingCanceledEvent.Invoke();
        }
    }

    public void OnConnect(InputAction.CallbackContext context) {
        // Invoca l'esdeveniment de connectar només quan l'acció comença
        if (context.phase == InputActionPhase.Started && connectEvent != null)
        {
            connectEvent.Invoke();
        }
    }
    public void OnInteract(InputAction.CallbackContext context) 
    {
        // Invoca l'esdeveniment d'interactuar només quan l'acció comença
        if (context.phase == InputActionPhase.Started && interactEvent != null)
        {
            interactEvent.Invoke();
        }
    }
    // Mètodes buits de suport per al compilador
    public void OnPauseMenu(InputAction.CallbackContext context) { }
    public void OnClimb(InputAction.CallbackContext context) { }
    public void OnGrapple(InputAction.CallbackContext context) { }
}


