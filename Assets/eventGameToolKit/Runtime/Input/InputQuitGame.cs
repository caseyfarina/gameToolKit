using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

// Quits the player when the user hits escape

/// <summary>
/// Quits the application when the Escape key is pressed (works in builds, not editor).
/// Common use: Exit buttons, debug shortcuts, or quick quit functionality in standalone applications.
/// </summary>
public class InputQuitGame : MonoBehaviour
{
	
    void Update()
    {
        if(Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Application.Quit();
        }

		
    }
}
