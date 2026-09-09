using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

/// <summary>
/// End-to-end test that clicking a 2D sprite fires onMouseClick.
///
/// This is the whole point of 2D picking support, so it is verified through the real
/// input path — a virtual mouse moved over the sprite and clicked — rather than by
/// calling the helper directly.
///
/// It has to run in PlayMode inside Unity's own loop. Driving the editor from outside
/// cannot test this: each external command stalls the main thread, so the component
/// never sees a frame where the button went down while hovering followed by a frame
/// where it came up.
/// </summary>
public class InputMouseInteraction2DTests : InputTestFixture
{
    private GameObject _camGo;
    private Camera _cam;
    private GameObject _sprite;
    private Mouse _mouse;

    public override void Setup()
    {
        base.Setup();

        _camGo = new GameObject("TestCamera") { tag = "MainCamera" };
        _cam = _camGo.AddComponent<Camera>();
        _cam.orthographic = true;
        _cam.orthographicSize = 5f;
        _cam.transform.position = new Vector3(0f, 0f, -10f);

        _mouse = InputSystem.AddDevice<Mouse>();
    }

    public override void TearDown()
    {
        if (_sprite != null) Object.DestroyImmediate(_sprite);
        if (_camGo != null) Object.DestroyImmediate(_camGo);
        base.TearDown();
    }

    private InputMouseInteraction BuildClickableSprite(bool twoD)
    {
        _sprite = new GameObject("clickable");
        _sprite.transform.position = Vector3.zero;

        if (twoD)
        {
            BoxCollider2D box = _sprite.AddComponent<BoxCollider2D>();
            box.size = new Vector2(4f, 4f);
        }
        else
        {
            BoxCollider box = _sprite.AddComponent<BoxCollider>();
            box.size = new Vector3(4f, 4f, 4f);
        }

        InputMouseInteraction mi = _sprite.AddComponent<InputMouseInteraction>();
        mi.onMouseClick ??= new UnityEngine.Events.UnityEvent();
        mi.onMouseEnter ??= new UnityEngine.Events.UnityEvent();
        mi.onMouseExit ??= new UnityEngine.Events.UnityEvent();
        mi.onMouseDown ??= new UnityEngine.Events.UnityEvent();
        mi.onMouseUp ??= new UnityEngine.Events.UnityEvent();
        mi.onMouseHover ??= new UnityEngine.Events.UnityEvent();
        return mi;
    }

    private Vector2 ScreenCentre => new Vector2(_cam.pixelWidth / 2f, _cam.pixelHeight / 2f);

    [UnityTest]
    public IEnumerator Hovering2DSprite_FiresMouseEnter()
    {
        InputMouseInteraction mi = BuildClickableSprite(twoD: true);
        int entered = 0;
        mi.onMouseEnter.AddListener(() => entered++);

        yield return new WaitForFixedUpdate();
        Set(_mouse.position, ScreenCentre);
        yield return null;
        yield return null;

        Assert.IsTrue(mi.IsHovering, "Hover was not detected over a 2D sprite.");
        Assert.AreEqual(1, entered, "onMouseEnter did not fire for a 2D sprite.");
    }

    [UnityTest]
    public IEnumerator Clicking2DSprite_FiresMouseClick()
    {
        InputMouseInteraction mi = BuildClickableSprite(twoD: true);
        int clicks = 0;
        mi.onMouseClick.AddListener(() => clicks++);

        yield return new WaitForFixedUpdate();
        Set(_mouse.position, ScreenCentre);
        yield return null;

        Press(_mouse.leftButton);
        yield return null;
        Release(_mouse.leftButton);
        yield return null;
        yield return null;

        Assert.AreEqual(1, clicks,
            "Clicking a 2D sprite did not fire onMouseClick. This is the core of 2D " +
            "click support.");
    }

    [UnityTest]
    public IEnumerator Clicking3DCollider_StillFiresMouseClick()
    {
        // Regression gate: the 3D path must keep working after the switch to EGTKPhysics.
        _cam.orthographic = false;
        InputMouseInteraction mi = BuildClickableSprite(twoD: false);
        int clicks = 0;
        mi.onMouseClick.AddListener(() => clicks++);

        yield return new WaitForFixedUpdate();
        Set(_mouse.position, ScreenCentre);
        yield return null;

        Press(_mouse.leftButton);
        yield return null;
        Release(_mouse.leftButton);
        yield return null;
        yield return null;

        Assert.AreEqual(1, clicks, "3D clicking regressed.");
    }

    [UnityTest]
    public IEnumerator ClickingAwayFrom2DSprite_DoesNotFire()
    {
        InputMouseInteraction mi = BuildClickableSprite(twoD: true);
        int clicks = 0;
        mi.onMouseClick.AddListener(() => clicks++);

        yield return new WaitForFixedUpdate();
        Set(_mouse.position, new Vector2(5f, 5f)); // bottom-left corner, off the sprite
        yield return null;

        Press(_mouse.leftButton);
        yield return null;
        Release(_mouse.leftButton);
        yield return null;
        yield return null;

        Assert.AreEqual(0, clicks, "Fired a click while the cursor was off the sprite.");
    }

    [UnityTest]
    public IEnumerator Clicking2DSprite_TogglesAnotherObject()
    {
        // Mirrors the click-to-toggle example scene end to end.
        InputMouseInteraction mi = BuildClickableSprite(twoD: true);
        ActionToggle toggle = _sprite.AddComponent<ActionToggle>();

        GameObject lamp = new GameObject("lamp");

        // targets is a private [SerializeField]; reach it by reflection rather than
        // widening the component's API. UnityEditor is unavailable in a PlayMode assembly.
        var field = typeof(ActionToggle).GetField("targets",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(field, "ActionToggle has no private 'targets' field.");
        field.SetValue(toggle, new System.Collections.Generic.List<GameObject> { lamp });

        mi.onMouseClick.AddListener(toggle.Toggle);

        yield return new WaitForFixedUpdate();
        Set(_mouse.position, ScreenCentre);
        yield return null;

        Assert.IsTrue(lamp.activeSelf, "Lamp should start on.");

        Press(_mouse.leftButton);
        yield return null;
        Release(_mouse.leftButton);
        yield return null;
        yield return null;

        Assert.IsFalse(lamp.activeSelf, "Clicking the sprite did not toggle the lamp off.");

        Object.DestroyImmediate(lamp);
    }
}
