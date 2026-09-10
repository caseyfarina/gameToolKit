using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

/// <summary>
/// Detects when a key is pressed and fires an event.
/// Common use: Ability activation, debug commands, door opening triggers.
///
/// The key is set by the Activation binding in the Inspector: press the + button, then
/// Listen, then the key you want. Because it is an Input System action rather than a
/// fixed key, the same component also accepts a gamepad button or any other control, and
/// you can add several bindings to one action.
/// </summary>
[HelpURL("https://caseyfarina.github.io/egtk-docs/")]
public class InputKeyPress : MonoBehaviour
{

    [Tooltip("What activates this. Press + then Listen, then the key or button you want.")]
    [SerializeField] private InputAction activation = new InputAction("Activation", InputActionType.Button);

    /// <summary>
    /// Fires when the activation binding is pressed
    /// </summary>
    public UnityEvent onPressEvent;

    // Gives a newly added component a working default binding, so it does something
    // before the student touches it. Only runs when the component is first added.
    private void Reset()
    {
        activation = new InputAction("Activation", InputActionType.Button);
        activation.AddBinding("<Keyboard>/space");
    }

    private void OnEnable()
    {
        activation.performed += OnActivated;
        activation.Enable();
    }

    private void OnDisable()
    {
        activation.performed -= OnActivated;
        activation.Disable();
    }

    private void OnActivated(InputAction.CallbackContext context) => onPressEvent?.Invoke();

    /// <summary>
    /// Fires the event directly, without any input. Useful for testing.
    /// </summary>
    public void TriggerPress() => onPressEvent?.Invoke();
}
