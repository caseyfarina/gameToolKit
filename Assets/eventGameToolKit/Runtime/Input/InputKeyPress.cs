using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Detects when a specific keyboard key is pressed and fires an event.
/// Common use: Ability activation, debug commands, door opening triggers, or custom input bindings.
/// </summary>
public class InputKeyPress : MonoBehaviour
{

    public KeyCode  thisKey = KeyCode.Space;

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
        if (Input.GetKeyDown(thisKey))
        {
            onPressEvent?.Invoke();
        }
    }
}
