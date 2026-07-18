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

    private GameInput gameInput;

    public bool IsSpacePressed {  get; private set; }

    private void OnEnable()
    {
        if (gameInput == null)
        {
            gameInput = new GameInput();
            gameInput.PlayerControls.SetCallbacks(this);
        }
        gameInput.PlayerControls.Enable();
    }

    private void OnDisable()
    {
        gameInput.PlayerControls.Disable();
    }

    public void OnMovement(InputAction.CallbackContext context)
    {
        if (moveEvent != null) moveEvent.Invoke(context.ReadValue<Vector2>());
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        if (lookEvent != null) lookEvent.Invoke(context.ReadValue<Vector2>());
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started && jumpEvent != null)
        {
            jumpEvent.Invoke();
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started && sprintStartedEvent != null)
        {
            sprintStartedEvent.Invoke();
        }
        else if (context.phase == InputActionPhase.Canceled && sprintCanceledEvent != null)
        {
            sprintCanceledEvent.Invoke();
        }
    }

    // <-- NOU: Callback de l'acció Swing
    public void OnSwing(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started && swingStartedEvent != null)
        {
            swingStartedEvent.Invoke();
        }
        else if (context.phase == InputActionPhase.Canceled && swingCanceledEvent != null)
        {
            swingCanceledEvent.Invoke();
        }
    }

    // Mètodes buits de suport per al compilador
    public void OnClimb(InputAction.CallbackContext context) { }
    public void OnGrapple(InputAction.CallbackContext context) { }
    public void OnConnect(InputAction.CallbackContext context) {
        if (context.phase == InputActionPhase.Started && connectEvent != null)
        {
            connectEvent.Invoke();
        }
    }
    public void OnInteract(InputAction.CallbackContext context) 
    {
        if (context.phase == InputActionPhase.Started && interactEvent != null)
        {
            interactEvent.Invoke();
        }
    }
    public void OnPauseMenu(InputAction.CallbackContext context) { }
}


