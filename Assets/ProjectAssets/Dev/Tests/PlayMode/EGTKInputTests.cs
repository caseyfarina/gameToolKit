using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

/// <summary>
/// Verifies the toolkit reads the keyboard and mouse through the Input System.
///
/// This matters because students create projects in Unity 6.3, where Active Input
/// Handling defaults to "Input System Package (New)" and the legacy UnityEngine.Input
/// backend is disabled. Any component still calling the old Input class does not work in
/// their projects, so these tests drive a virtual keyboard through the real Input System
/// path — the same path a student's hardware uses.
/// </summary>
public class EGTKInputTests : InputTestFixture
{
    private Keyboard _keyboard;
    private Mouse _mouse;
    private GameObject _go;

    public override void Setup()
    {
        base.Setup();
        _keyboard = InputSystem.AddDevice<Keyboard>();
        _mouse = InputSystem.AddDevice<Mouse>();
    }

    public override void TearDown()
    {
        if (_go != null) Object.DestroyImmediate(_go);
        base.TearDown();
    }

    [UnityTest]
    public IEnumerator WasKeyPressedThisFrame_TrueOnlyOnPressFrame()
    {
        Assert.IsFalse(EGTKInput.WasKeyPressedThisFrame(Key.Space), "True before any press.");

        Press(_keyboard.spaceKey);
        yield return null;
        Assert.IsTrue(EGTKInput.WasKeyPressedThisFrame(Key.Space), "Missed the press frame.");

        yield return null;
        Assert.IsFalse(EGTKInput.WasKeyPressedThisFrame(Key.Space),
            "Still reporting a press on the following frame; it must be edge-triggered.");

        Release(_keyboard.spaceKey);
        yield return null;
    }

    [UnityTest]
    public IEnumerator IsKeyHeld_TrueWhileDown()
    {
        Press(_keyboard.eKey);
        yield return null;
        Assert.IsTrue(EGTKInput.IsKeyHeld(Key.E));

        yield return null;
        Assert.IsTrue(EGTKInput.IsKeyHeld(Key.E), "Should stay true while held.");

        Release(_keyboard.eKey);
        yield return null;
        Assert.IsFalse(EGTKInput.IsKeyHeld(Key.E));
    }

    [UnityTest]
    public IEnumerator KeyNone_NeverReportsInput()
    {
        Press(_keyboard.spaceKey);
        yield return null;

        Assert.IsFalse(EGTKInput.WasKeyPressedThisFrame(Key.None),
            "Key.None must never report a press, or an unset field fires constantly.");
        Assert.IsFalse(EGTKInput.IsKeyHeld(Key.None));

        Release(_keyboard.spaceKey);
        yield return null;
    }

    [UnityTest]
    public IEnumerator MouseButton_PressReadsThroughInputSystem()
    {
        Press(_mouse.leftButton);
        yield return null;
        Assert.IsTrue(EGTKInput.WasMouseButtonPressedThisFrame(0));

        Release(_mouse.leftButton);
        yield return null;
        Assert.IsFalse(EGTKInput.WasMouseButtonPressedThisFrame(0));
    }

    [UnityTest]
    public IEnumerator InvalidMouseButton_ReturnsFalse()
    {
        Assert.IsFalse(EGTKInput.WasMouseButtonPressedThisFrame(99));
        Assert.IsFalse(EGTKInput.IsMouseButtonHeld(-1));
        yield return null;
    }


    // The activation InputAction is private and is subscribed in OnEnable, so the object is
    // built inactive, given its binding, then enabled.
    private InputKeyPress BuildKeyPress(string name, string binding)
    {
        _go = new GameObject(name);
        _go.SetActive(false);

        InputKeyPress press = _go.AddComponent<InputKeyPress>();
        var field = typeof(InputKeyPress).GetField("activation",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(field, "InputKeyPress has no private 'activation' field.");

        var action = new InputAction("Activation", InputActionType.Button);
        action.AddBinding(binding);
        field.SetValue(press, action);

        _go.SetActive(true);
        return press;
    }

    // ---- The component students actually use -------------------------------------

    [UnityTest]
    public IEnumerator InputKeyPress_FiresOnConfiguredKey()
    {
        InputKeyPress press = BuildKeyPress("keypress", "<Keyboard>/g");

        int fired = 0;
        press.onPressEvent ??= new UnityEngine.Events.UnityEvent();
        press.onPressEvent.AddListener(() => fired++);
        yield return null;

        Press(_keyboard.gKey);
        yield return null;
        yield return null;

        Assert.AreEqual(1, fired,
            "InputKeyPress did not fire. This is the toolkit's most-used component and " +
            "must work in a project set to Input System only.");

        Release(_keyboard.gKey);
        yield return null;
    }

    [UnityTest]
    public IEnumerator InputKeyPress_IgnoresOtherKeys()
    {
        InputKeyPress press = BuildKeyPress("keypress_other", "<Keyboard>/g");

        int fired = 0;
        press.onPressEvent ??= new UnityEngine.Events.UnityEvent();
        press.onPressEvent.AddListener(() => fired++);
        yield return null;

        Press(_keyboard.hKey);
        yield return null;
        yield return null;

        Assert.AreEqual(0, fired, "Fired on the wrong key.");

        Release(_keyboard.hKey);
        yield return null;
    }
}
