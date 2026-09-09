using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Tests for EGTKPhysics.PickAtScreenPoint, the screen-point hit test behind every
/// click/hover component.
///
/// The 3D cases are a regression gate: they exercise the same raycast the mouse
/// components used before 2D support, so they must pass both before and after those
/// components are switched over to the helper.
///
/// These are PlayMode rather than EditMode tests because they need a live camera and a
/// stepped physics world.
/// </summary>
public class EGTKPhysicsPickingTests
{
    private GameObject _camGo;
    private Camera _cam;
    private GameObject _target;

    [SetUp]
    public void SetUp()
    {
        _camGo = new GameObject("TestCamera") { tag = "MainCamera" };
        _cam = _camGo.AddComponent<Camera>();
        _cam.transform.position = new Vector3(0f, 0f, -10f);
        _cam.transform.rotation = Quaternion.identity;
    }

    [TearDown]
    public void TearDown()
    {
        if (_target != null) Object.DestroyImmediate(_target);
        if (_camGo != null) Object.DestroyImmediate(_camGo);
    }

    private Vector2 ScreenCentre => new Vector2(_cam.pixelWidth / 2f, _cam.pixelHeight / 2f);

    // ---- 3D: the regression gate -------------------------------------------------

    [UnityTest]
    public IEnumerator Picks3DColliderUnderCursor()
    {
        _cam.orthographic = false;

        _target = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _target.transform.position = Vector3.zero;
        yield return new WaitForFixedUpdate();

        GameObject hit = EGTKPhysics.PickAtScreenPoint(
            ScreenCentre, Mathf.Infinity, ~0, is2D: false);

        Assert.AreEqual(_target, hit, "Did not pick the 3D collider at screen centre.");
    }

    [UnityTest]
    public IEnumerator Returns3DNull_WhenNothingUnderCursor()
    {
        _cam.orthographic = false;

        _target = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _target.transform.position = new Vector3(500f, 500f, 0f);
        yield return new WaitForFixedUpdate();

        GameObject hit = EGTKPhysics.PickAtScreenPoint(
            ScreenCentre, Mathf.Infinity, ~0, is2D: false);

        Assert.IsNull(hit, "Reported a hit with nothing under the cursor.");
    }

    [UnityTest]
    public IEnumerator Respects3DLayerMask()
    {
        _cam.orthographic = false;

        _target = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _target.transform.position = Vector3.zero;
        _target.layer = 6; // Ground
        yield return new WaitForFixedUpdate();

        GameObject hit = EGTKPhysics.PickAtScreenPoint(
            ScreenCentre, Mathf.Infinity, 1 << 7, is2D: false);

        Assert.IsNull(hit, "Picked an object outside the supplied layer mask.");
    }

    // ---- 2D: the new behaviour ---------------------------------------------------

    [UnityTest]
    public IEnumerator Picks2DColliderUnderCursor()
    {
        _cam.orthographic = true;
        _cam.orthographicSize = 5f;

        _target = new GameObject("sprite2d");
        _target.transform.position = Vector3.zero;
        BoxCollider2D box = _target.AddComponent<BoxCollider2D>();
        box.size = new Vector2(4f, 4f);
        yield return new WaitForFixedUpdate();

        GameObject hit = EGTKPhysics.PickAtScreenPoint(
            ScreenCentre, Mathf.Infinity, ~0, is2D: true);

        Assert.AreEqual(_target, hit,
            "Did not pick the Collider2D at screen centre. Clicking sprites will not work.");
    }

    [UnityTest]
    public IEnumerator Picks2DTriggerCollider()
    {
        _cam.orthographic = true;
        _cam.orthographicSize = 5f;

        _target = new GameObject("sprite2d_trigger");
        _target.transform.position = Vector3.zero;
        BoxCollider2D box = _target.AddComponent<BoxCollider2D>();
        box.size = new Vector2(4f, 4f);
        box.isTrigger = true;
        yield return new WaitForFixedUpdate();

        GameObject hit = EGTKPhysics.PickAtScreenPoint(
            ScreenCentre, Mathf.Infinity, ~0, is2D: true);

        Assert.AreEqual(_target, hit,
            "Trigger colliders must be clickable; the 3D path allows them too.");
    }

    [UnityTest]
    public IEnumerator Returns2DNull_WhenNothingUnderCursor()
    {
        _cam.orthographic = true;
        _cam.orthographicSize = 5f;

        _target = new GameObject("sprite2d_far");
        _target.transform.position = new Vector3(500f, 500f, 0f);
        _target.AddComponent<BoxCollider2D>();
        yield return new WaitForFixedUpdate();

        GameObject hit = EGTKPhysics.PickAtScreenPoint(
            ScreenCentre, Mathf.Infinity, ~0, is2D: true);

        Assert.IsNull(hit, "Reported a hit with nothing under the cursor.");
    }

    [UnityTest]
    public IEnumerator Respects2DLayerMask()
    {
        _cam.orthographic = true;
        _cam.orthographicSize = 5f;

        _target = new GameObject("sprite2d_layer") { layer = 6 }; // Ground
        _target.transform.position = Vector3.zero;
        BoxCollider2D box = _target.AddComponent<BoxCollider2D>();
        box.size = new Vector2(4f, 4f);
        yield return new WaitForFixedUpdate();

        GameObject hit = EGTKPhysics.PickAtScreenPoint(
            ScreenCentre, Mathf.Infinity, 1 << 7, is2D: true);

        Assert.IsNull(hit, "Picked a sprite outside the supplied layer mask.");
    }

    [UnityTest]
    public IEnumerator Picks2DOffCentreSprite()
    {
        _cam.orthographic = true;
        _cam.orthographicSize = 5f;

        _target = new GameObject("sprite2d_offset");
        _target.transform.position = new Vector3(2f, 1f, 0f);
        BoxCollider2D box = _target.AddComponent<BoxCollider2D>();
        box.size = new Vector2(1f, 1f);
        yield return new WaitForFixedUpdate();

        Vector2 screenPos = _cam.WorldToScreenPoint(new Vector3(2f, 1f, 0f));
        GameObject hit = EGTKPhysics.PickAtScreenPoint(
            screenPos, Mathf.Infinity, ~0, is2D: true);

        Assert.AreEqual(_target, hit,
            "Screen-to-world conversion is off; only sprites at screen centre would be clickable.");
    }

    [UnityTest]
    public IEnumerator ReturnsNull_WhenNoMainCamera()
    {
        Object.DestroyImmediate(_camGo);
        _camGo = null;
        yield return null;

        GameObject hit = EGTKPhysics.PickAtScreenPoint(
            new Vector2(100f, 100f), Mathf.Infinity, ~0, is2D: true);

        Assert.IsNull(hit, "Must fail safe when there is no Main Camera.");
    }
}
