using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

/// <summary>
/// Detects when a specific keyboard key is pressed and fires an event.
/// Common use: Ability activation, debug commands, door opening triggers, or custom input bindings.
/// </summary>
[HelpURL("https://caseyfarina.github.io/egtk-docs/")]
public class InputKeyPress : MonoBehaviour
{

    [Tooltip("Which key activates this. Uses the Input System Key list.")]
    public Key activationKey = Key.Space;

    /// <summary>
    /// Fires when the specified key is pressed down
    /// </summary>
    public UnityEvent onPressEvent;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (EGTKInput.WasKeyPressedThisFrame(activationKey))
        {
            onPressEvent?.Invoke();
        }
    }
}
