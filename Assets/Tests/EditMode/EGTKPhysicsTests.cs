using NUnit.Framework;
using UnityEngine;

public class EGTKPhysicsTests
{
    private GameObject _go;

    [TearDown]
    public void TearDown()
    {
        if (_go != null) Object.DestroyImmediate(_go);
    }

    [Test]
    public void Is2D_ReturnsTrue_WhenCollider2DPresent()
    {
        _go = new GameObject("test2d");
        _go.AddComponent<BoxCollider2D>();
        Assert.IsTrue(EGTKPhysics.Is2D(_go));
    }

    [Test]
    public void Is2D_ReturnsFalse_WhenOnly3DColliderPresent()
    {
        _go = new GameObject("test3d");
        _go.AddComponent<BoxCollider>();
        Assert.IsFalse(EGTKPhysics.Is2D(_go));
    }

    [Test]
    public void Is2D_ReturnsFalse_WhenNoColliderPresent()
    {
        _go = new GameObject("bare");
        Assert.IsFalse(EGTKPhysics.Is2D(_go));
    }

    [Test]
    public void Is2D_HonorsExplicitOverride()
    {
        _go = new GameObject("override");
        _go.AddComponent<BoxCollider>();
        Assert.IsTrue(EGTKPhysics.Is2D(_go, PhysicsMode.TwoD));
        Assert.IsFalse(EGTKPhysics.Is2D(_go, PhysicsMode.ThreeD));
    }

    // Unity refuses to mix 2D and 3D physics components on one GameObject, so
    // detection can never be ambiguous. This test pins that engine behavior: if a
    // future Unity version allows the mix, this fails and Is2D needs a tiebreak rule.
    [Test]
    public void Unity_RefusesToMix2DAnd3DPhysicsOnOneObject()
    {
        _go = new GameObject("ambiguous");
        _go.AddComponent<BoxCollider>();
        BoxCollider2D added2D = _go.AddComponent<BoxCollider2D>();

        Assert.IsNull(added2D, "Unity allowed a Collider2D beside a 3D Collider. " +
                               "EGTKPhysics.Is2D now needs an explicit tiebreak rule.");
        Assert.IsFalse(EGTKPhysics.Is2D(_go));
    }

    [Test]
    public void Is2D_UsesRigidbody2D_WhenNoColliderPresent()
    {
        _go = new GameObject("rb2d");
        _go.AddComponent<Rigidbody2D>();
        Assert.IsTrue(EGTKPhysics.Is2D(_go));
    }

    [Test]
    public void Is2D_ReturnsFalse_ForNullObject()
    {
        Assert.IsFalse(EGTKPhysics.Is2D(null));
    }

    [Test]
    public void TryAddImpulse_ReturnsTrue_ForRigidbody2D()
    {
        _go = new GameObject("body2d");
        _go.AddComponent<Rigidbody2D>();
        Assert.IsTrue(EGTKPhysics.TryAddImpulse(_go, Vector3.up));
    }

    [Test]
    public void TryAddImpulse_ReturnsTrue_ForRigidbody3D()
    {
        _go = new GameObject("body3d");
        _go.AddComponent<Rigidbody>();
        Assert.IsTrue(EGTKPhysics.TryAddImpulse(_go, Vector3.up));
    }

    [Test]
    public void TryAddImpulse_ReturnsFalse_WhenNoBody()
    {
        _go = new GameObject("nobody");
        // Logs a "no body" warning; warnings do not fail tests.
        Assert.IsFalse(EGTKPhysics.TryAddImpulse(_go, Vector3.up));
    }

    [Test]
    public void TryAddImpulse_ReturnsFalse_ForNullTarget()
    {
        Assert.IsFalse(EGTKPhysics.TryAddImpulse(null, Vector3.up));
    }

    [Test]
    public void TryAddForce_ReturnsTrue_ForRigidbody2D()
    {
        _go = new GameObject("force2d");
        _go.AddComponent<Rigidbody2D>();
        Assert.IsTrue(EGTKPhysics.TryAddForce(_go, Vector3.right));
    }

    [Test]
    public void TryAddForce_ReturnsFalse_ForNullTarget()
    {
        Assert.IsFalse(EGTKPhysics.TryAddForce(null, Vector3.right));
    }
}
