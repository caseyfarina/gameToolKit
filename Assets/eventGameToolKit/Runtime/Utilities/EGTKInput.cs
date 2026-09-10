using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// Internal helper for reading the keyboard and mouse through the Input System.
/// Students never see or use this class.
///
/// The toolkit targets the Input System exclusively. Unity 6.3 projects are created with
/// Active Input Handling set to "Input System Package (New)", which disables the legacy
/// backend — so the old UnityEngine.Input class does not work in a student's fresh
/// project, and every read in the package must go through the Input System.
///
/// Components that need a whole control scheme (the character controllers) use
/// InputActions and PlayerInput, which is Unity's recommended workflow. Components where
/// a student picks a single key per object in the Inspector poll the device directly
/// through this helper instead: requiring an InputAction asset per door or pickup would
/// defeat the toolkit's no-code premise, and Unity documents direct polling as supported
/// for exactly this kind of simple, single-platform case.
///
/// Every method is null-safe. Keyboard.current and Mouse.current are null when no such
/// device is present, which would otherwise throw during play.
/// </summary>
public static class EGTKInput
{
    /// <summary>
    /// True on the single frame the key is pressed down. Equivalent to the old
    /// Input.GetKeyDown.
    /// </summary>
    public static bool WasKeyPressedThisFrame(Key key)
    {
        if (key == Key.None) return false;
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return false;

        KeyControl control = keyboard[key];
        return control != null && control.wasPressedThisFrame;
    }

    /// <summary>
    /// True every frame the key is held down. Equivalent to the old Input.GetKey.
    /// </summary>
    public static bool IsKeyHeld(Key key)
    {
        if (key == Key.None) return false;
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return false;

        KeyControl control = keyboard[key];
        return control != null && control.isPressed;
    }

    /// <summary>
    /// True on the single frame the key is released. Equivalent to the old Input.GetKeyUp.
    /// </summary>
    public static bool WasKeyReleasedThisFrame(Key key)
    {
        if (key == Key.None) return false;
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return false;

        KeyControl control = keyboard[key];
        return control != null && control.wasReleasedThisFrame;
    }

    /// <summary>
    /// True on the single frame a mouse button is pressed. 0 = left, 1 = right,
    /// 2 = middle, matching the old Input.GetMouseButtonDown numbering.
    /// </summary>
    public static bool WasMouseButtonPressedThisFrame(int button)
    {
        ButtonControl control = GetMouseButton(button);
        return control != null && control.wasPressedThisFrame;
    }

    /// <summary>
    /// True every frame a mouse button is held. 0 = left, 1 = right, 2 = middle.
    /// </summary>
    public static bool IsMouseButtonHeld(int button)
    {
        ButtonControl control = GetMouseButton(button);
        return control != null && control.isPressed;
    }

    /// <summary>
    /// The mouse cursor position in screen pixels, or zero when there is no mouse.
    /// </summary>
    public static Vector2 MousePosition =>
        Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;

    private static ButtonControl GetMouseButton(int button)
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return null;

        switch (button)
        {
            case 0: return mouse.leftButton;
            case 1: return mouse.rightButton;
            case 2: return mouse.middleButton;
            default: return null;
        }
    }
}
