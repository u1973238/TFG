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
    public event UnityAction grappleStartedEvent;
    public event UnityAction grappleCanceledEvent;
    public event UnityAction connectEvent;

    public event UnityAction interactEvent;

    public event System.Action pauseMenuEvent;

    private GameInput gameInput;

    public bool IsSpacePressed {  get; private set; }

    // Indica si l'últim moviment de càmera ve d'un comandament (el joystick s'ha d'escalar pel temps, el ratolí no)
    public bool IsLookFromGamepad { get; private set; }

    // Mentre el joc està aturat (menú inicial, pausa o victòria) no enviem accions de joc noves,
    // així els clics als botons dels menús no llancen teranyines ni fan saltar l'aranya
    private static bool IsGameFrozen => Time.timeScale == 0f;

    // Mentre la càmera ensenya alguna cosa (per exemple, la roca que obre una palanca), l'aranya no es mou ni fa accions
    public bool GameplayBlocked { get; private set; }
    private bool ActionsBlocked => IsGameFrozen || GameplayBlocked;

    public void SetGameplayBlocked(bool blocked)
    {
        if (GameplayBlocked == blocked) return;
        GameplayBlocked = blocked;

        // En bloquejar, l'aranya s'atura; en desbloquejar, recupera el que s'estigui prement en aquell moment
        Vector2 move = !blocked && gameInput != null ? gameInput.PlayerControls.Movement.ReadValue<Vector2>() : Vector2.zero;
        moveEvent?.Invoke(move);
    }

    private void OnEnable()
    {
        GameplayBlocked = false;

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
        if (GameplayBlocked) return; // Mentre la càmera ensenya alguna cosa, l'aranya es queda quieta
        if (moveEvent != null) moveEvent.Invoke(context.ReadValue<Vector2>());
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        // Invoca l'esdeveniment de mirada amb el valor del vector només quan hi ha un esdeveniment de mirada
        IsLookFromGamepad = context.control != null && context.control.device is Gamepad;
        if (lookEvent != null) lookEvent.Invoke(context.ReadValue<Vector2>());
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        // Invoca l'esdeveniment de salt només quan l'acció comença
        if (context.phase == InputActionPhase.Started && jumpEvent != null && !ActionsBlocked)
        {
            jumpEvent.Invoke();
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        // Invoca els esdeveniments de començar i cancel·lar l'esprint segons la fase de l'acció
        if (context.phase == InputActionPhase.Started && sprintStartedEvent != null && !ActionsBlocked)
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
        if (context.phase == InputActionPhase.Started && swingStartedEvent != null && !ActionsBlocked)
        {
            swingStartedEvent.Invoke();
        }
        else if (context.phase == InputActionPhase.Canceled && swingCanceledEvent != null)
        {
            swingCanceledEvent.Invoke();
        }
    }

    public void OnGrapple(InputAction.CallbackContext context)
    {
        // Invoca els esdeveniments de començar i deixar de recollir el fil (grappling) segons la fase de l'acció
        if (context.phase == InputActionPhase.Started && grappleStartedEvent != null && !ActionsBlocked)
        {
            grappleStartedEvent.Invoke();
        }
        else if (context.phase == InputActionPhase.Canceled && grappleCanceledEvent != null)
        {
            grappleCanceledEvent.Invoke();
        }
    }

    public void OnConnect(InputAction.CallbackContext context) {
        // Invoca l'esdeveniment de connectar només quan l'acció comença
        if (context.phase == InputActionPhase.Started && connectEvent != null && !ActionsBlocked)
        {
            connectEvent.Invoke();
        }
    }
    public void OnInteract(InputAction.CallbackContext context) 
    {
        // Invoca l'esdeveniment d'interactuar només quan l'acció comença
        if (context.phase == InputActionPhase.Started && interactEvent != null && !ActionsBlocked)
        {
            interactEvent.Invoke();
        }
    }
    // Mètodes buits de suport per al compilador
    public void OnPauseMenu(InputAction.CallbackContext context) { }
    public void OnClimb(InputAction.CallbackContext context) { }
}


