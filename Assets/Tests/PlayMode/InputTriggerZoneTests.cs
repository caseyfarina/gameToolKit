using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

/// <summary>
/// Behavioral gate for InputTriggerZone.
///
/// Every test here passed against the original 3D-only implementation before 2D
/// support was added. They must keep passing afterwards — that is what makes them a
/// regression gate rather than a description of whatever the code currently does.
///
/// Each behavior is asserted twice: once with 3D colliders, once with 2D. Unity
/// dispatches OnTriggerEnter and OnTriggerEnter2D independently, so the same component
/// must serve both.
/// </summary>
public class InputTriggerZoneTests
{
    private GameObject _zone;
    private GameObject _mover;
    private int _enterCount;
    private int _exitCount;
    private int _stayCount;

    [TearDown]
    public void TearDown()
    {
        if (_zone != null) Object.Destroy(_zone);
        if (_mover != null) Object.Destroy(_mover);
        _enterCount = _exitCount = _stayCount = 0;
    }

    // The tag filter and stay settings are private [SerializeField]s. Tests reach them
    // by reflection rather than widening the component's API for testing's sake.
    private static void SetPrivate(object target, string field, object value)
    {
        FieldInfo f = target.GetType().GetField(field,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(f, $"InputTriggerZone has no private field '{field}'. " +
                          "If it was renamed, update these tests.");
        f.SetValue(target, value);
    }

    private InputTriggerZone BuildZone(bool twoD)
    {
        _zone = new GameObject("zone");
        if (twoD)
        {
            BoxCollider2D box = _zone.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(4f, 4f);
        }
        else
        {
            BoxCollider box = _zone.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(4f, 4f, 4f);
        }

        InputTriggerZone zone = _zone.AddComponent<InputTriggerZone>();

        // AddComponent at runtime leaves serialized UnityEvent fields null; the editor
        // would have populated them. The component itself uses ?.Invoke() so it copes,
        // but the tests need real instances to attach listeners to.
        zone.onTriggerEnterEvent ??= new UnityEvent();
        zone.onTriggerExitEvent ??= new UnityEvent();
        zone.onTriggerStayEvent ??= new UnityEvent();

        zone.onTriggerEnterEvent.AddListener(() => _enterCount++);
        zone.onTriggerExitEvent.AddListener(() => _exitCount++);
        zone.onTriggerStayEvent.AddListener(() => _stayCount++);
        return zone;
    }

    // A trigger needs at least one Rigidbody in the pair, so the mover carries it.
    private void BuildMover(bool twoD, string tag, Vector3 startPosition)
    {
        _mover = new GameObject("mover") { tag = tag };
        _mover.transform.position = startPosition;

        if (twoD)
        {
            _mover.AddComponent<BoxCollider2D>();
            Rigidbody2D body = _mover.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
        }
        else
        {
            _mover.AddComponent<BoxCollider>();
            Rigidbody body = _mover.AddComponent<Rigidbody>();
            body.useGravity = false;
        }
    }

    private static IEnumerator Settle()
    {
        // Two physics steps: one to register the overlap, one for the callback to land.
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
    }

    [UnityTest]
    public IEnumerator Enter_Fires_ForTaggedObject_3D()
    {
        BuildZone(false);
        BuildMover(false, "Player", new Vector3(20f, 0f, 0f));
        yield return Settle();

        Assert.AreEqual(0, _enterCount, "Fired before the object was inside.");

        _mover.transform.position = Vector3.zero;
        yield return Settle();

        Assert.AreEqual(1, _enterCount);
    }

    [UnityTest]
    public IEnumerator Enter_Fires_ForTaggedObject_2D()
    {
        BuildZone(true);
        BuildMover(true, "Player", new Vector3(20f, 0f, 0f));
        yield return Settle();

        Assert.AreEqual(0, _enterCount, "Fired before the object was inside.");

        _mover.transform.position = Vector3.zero;
        yield return Settle();

        Assert.AreEqual(1, _enterCount, "OnTriggerEnter2D did not reach the component.");
    }

    [UnityTest]
    public IEnumerator Enter_DoesNotFire_ForUntaggedObject_3D()
    {
        BuildZone(false);
        BuildMover(false, "Untagged", Vector3.zero);
        yield return Settle();

        Assert.AreEqual(0, _enterCount);
    }

    [UnityTest]
    public IEnumerator Enter_DoesNotFire_ForUntaggedObject_2D()
    {
        BuildZone(true);
        BuildMover(true, "Untagged", Vector3.zero);
        yield return Settle();

        Assert.AreEqual(0, _enterCount);
    }

    [UnityTest]
    public IEnumerator Exit_Fires_WhenObjectLeaves_3D()
    {
        BuildZone(false);
        BuildMover(false, "Player", Vector3.zero);
        yield return Settle();

        _mover.transform.position = new Vector3(20f, 0f, 0f);
        yield return Settle();

        Assert.AreEqual(1, _exitCount);
    }

    [UnityTest]
    public IEnumerator Exit_Fires_WhenObjectLeaves_2D()
    {
        BuildZone(true);
        BuildMover(true, "Player", Vector3.zero);
        yield return Settle();

        _mover.transform.position = new Vector3(20f, 0f, 0f);
        yield return Settle();

        Assert.AreEqual(1, _exitCount);
    }

    [UnityTest]
    public IEnumerator IsObjectInTrigger_TracksOccupancy_3D()
    {
        InputTriggerZone zone = BuildZone(false);
        BuildMover(false, "Player", Vector3.zero);
        yield return Settle();

        Assert.IsTrue(zone.IsObjectInTrigger, "Reported empty while occupied.");

        _mover.transform.position = new Vector3(20f, 0f, 0f);
        yield return Settle();

        Assert.IsFalse(zone.IsObjectInTrigger, "Reported occupied after the object left.");
    }

    [UnityTest]
    public IEnumerator IsObjectInTrigger_TracksOccupancy_2D()
    {
        InputTriggerZone zone = BuildZone(true);
        BuildMover(true, "Player", Vector3.zero);
        yield return Settle();

        Assert.IsTrue(zone.IsObjectInTrigger, "Reported empty while occupied.");

        _mover.transform.position = new Vector3(20f, 0f, 0f);
        yield return Settle();

        Assert.IsFalse(zone.IsObjectInTrigger, "Reported occupied after the object left.");
    }

    [UnityTest]
    public IEnumerator IsObjectInTrigger_GoesFalse_WhenOccupantDestroyed_3D()
    {
        InputTriggerZone zone = BuildZone(false);
        BuildMover(false, "Player", Vector3.zero);
        yield return Settle();

        Assert.IsTrue(zone.IsObjectInTrigger);

        Object.DestroyImmediate(_mover);
        _mover = null;
        yield return Settle();

        Assert.IsFalse(zone.IsObjectInTrigger,
            "A destroyed occupant still counted. Collectibles that destroy themselves " +
            "would leave the zone permanently occupied.");
    }

    [UnityTest]
    public IEnumerator IsObjectInTrigger_GoesFalse_WhenOccupantDestroyed_2D()
    {
        InputTriggerZone zone = BuildZone(true);
        BuildMover(true, "Player", Vector3.zero);
        yield return Settle();

        Assert.IsTrue(zone.IsObjectInTrigger);

        Object.DestroyImmediate(_mover);
        _mover = null;
        yield return Settle();

        Assert.IsFalse(zone.IsObjectInTrigger);
    }

    [UnityTest]
    public IEnumerator Stay_Fires_AtInterval_WhenEnabled_3D()
    {
        InputTriggerZone zone = BuildZone(false);
        SetPrivate(zone, "enableStayEvent", true);
        SetPrivate(zone, "stayInterval", 0.3f);
        BuildMover(false, "Player", Vector3.zero);

        yield return new WaitForSeconds(1.0f);

        Assert.GreaterOrEqual(_stayCount, 2,
            $"Expected repeated stay events at a 0.3s interval, got {_stayCount}.");
    }

    [UnityTest]
    public IEnumerator Stay_Fires_AtInterval_WhenEnabled_2D()
    {
        InputTriggerZone zone = BuildZone(true);
        SetPrivate(zone, "enableStayEvent", true);
        SetPrivate(zone, "stayInterval", 0.3f);
        BuildMover(true, "Player", Vector3.zero);

        yield return new WaitForSeconds(1.0f);

        Assert.GreaterOrEqual(_stayCount, 2,
            $"Expected repeated stay events at a 0.3s interval, got {_stayCount}.");
    }

    [UnityTest]
    public IEnumerator Stay_DoesNotFire_WhenDisabled_3D()
    {
        InputTriggerZone zone = BuildZone(false);
        SetPrivate(zone, "enableStayEvent", false);
        BuildMover(false, "Player", Vector3.zero);

        yield return new WaitForSeconds(0.8f);

        Assert.AreEqual(0, _stayCount);
    }
}
