using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Behavioral tests for CharacterController2D.
///
/// These run inside Unity's own frame loop. Driving the controller from outside (via
/// the editor command server) does not work for frame-sensitive logic, because each
/// external call stalls the main thread and the next Time.deltaTime absorbs the delay,
/// draining the coyote and jump-buffer timers before FixedUpdate can act on them.
/// </summary>
public class CharacterController2DTests
{
    private GameObject _ground;
    private GameObject _player;
    private CharacterController2D _controller;
    private Rigidbody2D _body;

    private const int GroundLayer = 6; // "Ground" — exists in this project's layer list

    [TearDown]
    public void TearDown()
    {
        if (_player != null) Object.Destroy(_player);
        if (_ground != null) Object.Destroy(_ground);
    }

    private static void SetPrivate(object target, string field, object value)
    {
        FieldInfo f = target.GetType().GetField(field,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(f, $"CharacterController2D has no private field '{field}'.");
        f.SetValue(target, value);
    }

    private static T GetPrivate<T>(object target, string field)
    {
        FieldInfo f = target.GetType().GetField(field,
            BindingFlags.Instance | BindingFlags.NonPublic);
        return (T)f.GetValue(target);
    }

    private void Build(CharacterController2D.MovementStyle style)
    {
        _ground = new GameObject("ground") { layer = GroundLayer };
        _ground.transform.position = new Vector3(0f, -1f, 0f);
        BoxCollider2D groundBox = _ground.AddComponent<BoxCollider2D>();
        groundBox.size = new Vector2(50f, 1f);

        _player = new GameObject("player");
        _player.transform.position = new Vector3(0f, 0.5f, 0f);
        CapsuleCollider2D capsule = _player.AddComponent<CapsuleCollider2D>();
        capsule.size = new Vector2(0.7f, 0.95f);

        _body = _player.AddComponent<Rigidbody2D>();
        _controller = _player.AddComponent<CharacterController2D>();
        _controller.movementStyle = style;

        SetPrivate(_controller, "groundLayer", (LayerMask)(1 << GroundLayer));
        SetPrivate(_controller, "groundCheckOffset", 0.5f);
        SetPrivate(_controller, "groundCheckSize", new Vector2(0.6f, 0.2f));
    }

    private void SetMove(Vector2 v) => SetPrivate(_controller, "_moveInput", v);

    [UnityTest]
    public IEnumerator Platformer_FallsAndLandsOnGround()
    {
        Build(CharacterController2D.MovementStyle.Platformer);
        _player.transform.position = new Vector3(0f, 4f, 0f);

        yield return new WaitForSeconds(1.5f);

        Assert.IsTrue(_controller.IsGrounded, "Never landed on the ground collider.");
        Assert.Less(_player.transform.position.y, 4f, "Did not fall at all.");
    }

    [UnityTest]
    public IEnumerator Platformer_MovesRightAtMoveSpeed()
    {
        Build(CharacterController2D.MovementStyle.Platformer);
        yield return new WaitForSeconds(0.3f);

        float startX = _player.transform.position.x;
        SetMove(Vector2.right);
        yield return new WaitForSeconds(0.6f);

        Assert.Greater(_player.transform.position.x, startX + 0.5f,
            "Holding right did not move the character.");
        Assert.AreEqual(8f, _body.linearVelocity.x, 0.5f,
            "Did not reach the configured move speed of 8.");
    }

    [UnityTest]
    public IEnumerator Platformer_StopsWhenInputReleased()
    {
        Build(CharacterController2D.MovementStyle.Platformer);
        yield return new WaitForSeconds(0.3f);

        SetMove(Vector2.right);
        yield return new WaitForSeconds(0.4f);
        SetMove(Vector2.zero);
        yield return new WaitForSeconds(0.5f);

        Assert.AreEqual(0f, _body.linearVelocity.x, 0.2f,
            "Character kept sliding after input stopped.");
    }

    [UnityTest]
    public IEnumerator Platformer_JumpRaisesCharacter()
    {
        Build(CharacterController2D.MovementStyle.Platformer);
        yield return new WaitForSeconds(0.4f);

        Assert.IsTrue(_controller.IsGrounded, "Must be grounded before jumping.");
        float restY = _player.transform.position.y;

        // Queue a jump the same way OnJump does, without needing an InputValue.
        SetPrivate(_controller, "_jumpBufferCounter", 0.15f);

        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        Assert.Greater(_body.linearVelocity.y, 1f,
            "Jump did not produce upward velocity.");

        yield return new WaitForSeconds(0.25f);
        Assert.Greater(_player.transform.position.y, restY + 0.3f,
            "Character did not actually rise after jumping.");
    }

    [UnityTest]
    public IEnumerator Platformer_JumpFiresOnJumpEvent()
    {
        Build(CharacterController2D.MovementStyle.Platformer);
        int jumps = 0;
        _controller.onJump ??= new UnityEngine.Events.UnityEvent();
        _controller.onJump.AddListener(() => jumps++);

        yield return new WaitForSeconds(0.4f);
        SetPrivate(_controller, "_jumpBufferCounter", 0.15f);
        yield return new WaitForSeconds(0.2f);

        Assert.AreEqual(1, jumps, "onJump did not fire exactly once.");
    }

    [UnityTest]
    public IEnumerator Platformer_CannotJumpWhileAirborne()
    {
        Build(CharacterController2D.MovementStyle.Platformer);
        _player.transform.position = new Vector3(0f, 8f, 0f);
        yield return new WaitForSeconds(0.4f);

        Assert.IsFalse(_controller.IsGrounded, "Should still be falling.");
        SetPrivate(_controller, "_coyoteCounter", 0f);
        SetPrivate(_controller, "_jumpBufferCounter", 0.15f);

        float velBefore = _body.linearVelocity.y;
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        Assert.LessOrEqual(_body.linearVelocity.y, velBefore + 0.5f,
            "Jumped in mid-air with no coyote time remaining.");
    }

    [UnityTest]
    public IEnumerator TopDown_HasNoGravityAndMovesVertically()
    {
        Build(CharacterController2D.MovementStyle.TopDown);
        yield return new WaitForSeconds(0.2f);

        Assert.AreEqual(0f, _body.gravityScale, 0.001f,
            "Top-Down style must disable gravity.");

        float startY = _player.transform.position.y;
        SetMove(Vector2.up);
        yield return new WaitForSeconds(0.5f);

        Assert.Greater(_player.transform.position.y, startY + 0.5f,
            "Top-Down character did not move upward.");
    }

    [UnityTest]
    public IEnumerator TeleportTo_MovesAndClearsMomentum()
    {
        Build(CharacterController2D.MovementStyle.Platformer);
        yield return new WaitForSeconds(0.3f);

        SetMove(Vector2.right);
        yield return new WaitForSeconds(0.4f);

        _controller.TeleportTo(new Vector3(15f, 3f, 0f));
        yield return new WaitForFixedUpdate();

        Assert.AreEqual(15f, _player.transform.position.x, 0.6f, "Did not teleport.");
        Assert.AreEqual(0f, _body.linearVelocity.x, 0.6f,
            "Momentum survived the teleport; the character would keep sliding.");
    }

    [UnityTest]
    public IEnumerator ImplementsITeleportableCharacter()
    {
        Build(CharacterController2D.MovementStyle.Platformer);
        yield return null;

        ITeleportableCharacter asInterface = _player.GetComponent<ITeleportableCharacter>();
        Assert.NotNull(asInterface,
            "Checkpoints and teleporters find controllers through this interface.");
    }

    [UnityTest]
    public IEnumerator JumpBufferDoesNotDecayUnbounded()
    {
        Build(CharacterController2D.MovementStyle.Platformer);
        yield return new WaitForSeconds(1.5f);

        float buffer = GetPrivate<float>(_controller, "_jumpBufferCounter");
        Assert.GreaterOrEqual(buffer, -1f,
            $"Jump buffer decayed to {buffer}. It should be clamped rather than " +
            "counting down forever.");
    }
}
