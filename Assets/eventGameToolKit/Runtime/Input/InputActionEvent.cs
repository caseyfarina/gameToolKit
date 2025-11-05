using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Triggers UnityEvents when a specific Input Action is performed, allowing students to wire input events visually.
/// Use this to connect any button press, joystick movement, or other input to game events without writing code.
/// Example: Wire Jump action to spawn objects, play sounds, or trigger animations.
/// </summary>
public class InputActionEvent : MonoBehaviour
{
    [Header("Input Action Reference")]
    [Tooltip("The Input Action to listen for (drag from Input Actions asset)")]
    [SerializeField] private InputActionReference actionReference;

    [Header("Event Trigger Settings")]
    [Tooltip("Fire event when action is performed (button pressed, trigger pulled, etc.)")]
    [SerializeField] private bool triggerOnPerformed = true;
    [Tooltip("Fire event when action starts (initial input detected)")]
    [SerializeField] private bool triggerOnStarted = false;
    [Tooltip("Fire event when action is canceled (button released, input stopped)")]
    [SerializeField] private bool triggerOnCanceled = false;

    [Header("Events")]
    /// <summary>
    /// Fires when the input action is performed (based on trigger settings above)
    /// </summary>
    public UnityEvent onActionTriggered;

    /// <summary>
    /// Fires when the input action is performed, passing the input value as a float (for axes, triggers, pressure-sensitive buttons)
    /// </summary>
    public UnityEvent<float> onActionTriggeredWithValue;

    /// <summary>
    /// Fires when the input action is performed, passing the input value as Vector2 (for joysticks, D-pads, WASD movement)
    /// </summary>
    public UnityEvent<Vector2> onActionTriggeredWithVector2;

    private InputAction _action;

    private void OnEnable()
    {
        if (actionReference != null)
        {
            _action = actionReference.action;
            if (_action != null)
            {
                // Subscribe to events based on settings
                if (triggerOnStarted)
                    _action.started += OnActionEvent;
                if (triggerOnPerformed)
                    _action.performed += OnActionEvent;
                if (triggerOnCanceled)
                    _action.canceled += OnActionEvent;

                // Enable the action if it's not already enabled
                if (!_action.enabled)
                    _action.Enable();
            }
            else
            {
                Debug.LogWarning($"InputActionEvent on {gameObject.name}: Input Action Reference is not set or action is null.", this);
            }
        }
        else
        {
            Debug.LogWarning($"InputActionEvent on {gameObject.name}: Input Action Reference is not assigned in Inspector.", this);
        }
    }

    private void OnDisable()
    {
        if (_action != null)
        {
            // Unsubscribe from events
            if (triggerOnStarted)
                _action.started -= OnActionEvent;
            if (triggerOnPerformed)
                _action.performed -= OnActionEvent;
            if (triggerOnCanceled)
                _action.canceled -= OnActionEvent;
        }
    }

    private void OnActionEvent(InputAction.CallbackContext context)
    {
        // Fire the basic event
        onActionTriggered?.Invoke();

        // Try to read the value and fire typed events
        TryFireValueEvents(context);
    }

    private void TryFireValueEvents(InputAction.CallbackContext context)
    {
        // Try to read as Vector2 (joystick, WASD, D-pad)
        if (context.action.expectedControlType == "Vector2")
        {
            Vector2 value = context.ReadValue<Vector2>();
            onActionTriggeredWithVector2?.Invoke(value);
        }
        // Try to read as float (trigger, axis, pressure-sensitive button)
        else if (context.action.expectedControlType == "Axis" ||
                 context.action.expectedControlType == "Analog")
        {
            float value = context.ReadValue<float>();
            onActionTriggeredWithValue?.Invoke(value);
        }
        // Button type - send 1.0 for pressed, 0.0 for released
        else if (context.action.expectedControlType == "Button")
        {
            float value = context.ReadValueAsButton() ? 1.0f : 0.0f;
            onActionTriggeredWithValue?.Invoke(value);
        }
    }

    /// <summary>
    /// Manually trigger the action event (useful for testing or external triggering)
    /// </summary>
    public void TriggerEvent()
    {
        onActionTriggered?.Invoke();
    }

    /// <summary>
    /// Enable the input action listening
    /// </summary>
    public void EnableAction()
    {
        if (_action != null && !_action.enabled)
        {
            _action.Enable();
        }
    }

    /// <summary>
    /// Disable the input action listening
    /// </summary>
    public void DisableAction()
    {
        if (_action != null && _action.enabled)
        {
            _action.Disable();
        }
    }

    /// <summary>
    /// Check if the action is currently enabled
    /// </summary>
    public bool IsActionEnabled()
    {
        return _action != null && _action.enabled;
    }
}
