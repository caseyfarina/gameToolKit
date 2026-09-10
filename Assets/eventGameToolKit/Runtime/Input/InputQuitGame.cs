using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Quits the application. Escape quits by default, and QuitGame() can be wired to any
/// event so a button or a clicked object can quit too.
///
/// Quitting does nothing in the Editor's Play mode by design, so this component stops
/// Play mode instead — that way you can tell it worked while testing.
///
/// Common use: Exit buttons, quit menus, debug shortcuts.
/// </summary>
[HelpURL("https://caseyfarina.github.io/egtk-docs/")]
public class InputQuitGame : MonoBehaviour
{
    [Tooltip("Quit when the Escape key is pressed. Turn this off to quit only from events.")]
    [SerializeField] private bool quitOnEscape = true;

    private void Update()
    {
        // EGTKInput is null-safe: reading Keyboard.current directly throws when no
        // keyboard is attached, which happens on some devices and in test runs.
        if (quitOnEscape && EGTKInput.WasKeyPressedThisFrame(Key.Escape))
        {
            QuitGame();
        }
    }

    /// <summary>
    /// Quits the game. Wire this to any event, such as a button or a clicked object.
    /// </summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        // Application.Quit is ignored in the Editor, so stop Play mode instead. Without
        // this, a student testing a quit button sees nothing happen and assumes it is broken.
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
