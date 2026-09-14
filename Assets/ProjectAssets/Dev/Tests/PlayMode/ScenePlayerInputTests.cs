using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Guards two failure modes that both compile, load, pass SceneSmokeTests, log nothing at
/// all, and still leave a character that does not move.
///
/// 1. Several <see cref="PlayerInput"/> components in one scene. PlayerInput is built for
///    local multiplayer, so when a scene holds more than one, Unity deliberately gives each
///    a DIFFERENT control scheme. With EGTK_InputSystem_Actions (Keyboard&amp;Mouse, Gamepad,
///    Touch, Joystick, XR) the first takes Keyboard&amp;Mouse and the second falls through to
///    Touch, paired to a Touchscreen — perfectly valid, and deaf to every key press.
///
/// 2. A ground check that cannot reach the ground. The player controllers return early from
///    movement while airborne, so a ground probe that never touches the floor freezes the
///    character completely. PhysicsBallPlayerController probes a sphere at only 0.1 below
///    the transform, which under a 0.5-radius ball needs groundCheckRadius >= 0.4 to reach.
///
/// Both were found by a human pressing keys and watching nothing happen. Neither is visible
/// to any other check in this project.
/// </summary>
// Named to sort after the Input* fixtures for the same reason SceneSmokeTests is: loading
// real scenes mutates Input System device pairing globally, which corrupts the virtual
// devices that InputTestFixture-based tests rely on.
public class ScenePlayerInputTests
{
    private static readonly string[] ScenesWithPlayers =
    {
        "Assets/eventGameToolKit/ExampleScenes/Example3D_Physics.unity",
        "Assets/eventGameToolKit/ExampleScenes/Example3D_PlatformPairing.unity",
    };

    [UnityTest]
    public IEnumerator SceneHasExactlyOneActivePlayerInput(
        [ValueSource(nameof(ScenesWithPlayers))] string scenePath)
    {
        yield return LoadAndSettle(scenePath);

        PlayerInput[] active = ActivePlayerInputs();

        Assert.That(active.Length, Is.EqualTo(1),
            $"{scenePath} has {active.Length} active PlayerInput components " +
            $"({string.Join(", ", active.Select(p => p.gameObject.name))}). Unity gives a " +
            "second one a different control scheme, so it ignores the keyboard entirely. " +
            "Ship one active character and leave the others disabled in the scene.");
    }

    [UnityTest]
    public IEnumerator TheActivePlayerInputListensToTheKeyboard(
        [ValueSource(nameof(ScenesWithPlayers))] string scenePath)
    {
        yield return LoadAndSettle(scenePath);

        foreach (PlayerInput pi in ActivePlayerInputs())
        {
            string devices = pi.devices.Count == 0
                ? "<none>"
                : string.Join(" | ", pi.devices.Select(d => d.displayName));

            Assert.That(pi.devices.Any(d => d is Keyboard), Is.True,
                $"'{pi.gameObject.name}' in {scenePath} is paired with [{devices}] on scheme " +
                $"'{pi.currentControlScheme}' rather than the keyboard, so it ignores every " +
                "key press.");
        }
    }

    /// <summary>
    /// The active character must actually be standing on something. Both player controllers
    /// return early from movement when airborne, so a ground probe that cannot reach the
    /// floor is indistinguishable from broken input — the character simply never moves.
    /// </summary>
    [UnityTest]
    public IEnumerator TheActiveCharacterFindsTheGround()
    {
        yield return LoadAndSettle(
            "Assets/eventGameToolKit/ExampleScenes/Example3D_Physics.unity");

        // Let the rigidbody settle onto the ground before sampling.
        for (int i = 0; i < 40; i++) yield return new WaitForFixedUpdate();

        MonoBehaviour[] characters = Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Where(m => m.GetType().GetMethod("OnMove") != null)
            .ToArray();

        Assert.That(characters, Is.Not.Empty, "Expected an active input-driven character.");

        foreach (MonoBehaviour c in characters)
        {
            FieldInfo grounded = c.GetType().GetField(
                "isGrounded", BindingFlags.Instance | BindingFlags.NonPublic);

            if (grounded == null) continue; // controller reports grounding differently

            Assert.That((bool)grounded.GetValue(c), Is.True,
                $"'{c.gameObject.name}' ({c.GetType().Name}) never registers as grounded, so " +
                "its movement returns early every frame and it cannot move or jump. Check " +
                "groundCheckRadius against the character's actual size, and that the ground " +
                "is on the layer named by groundLayer.");
        }
    }

    private static PlayerInput[] ActivePlayerInputs() =>
        Object.FindObjectsByType<PlayerInput>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Where(pi => pi.isActiveAndEnabled)
            .ToArray();

    private static IEnumerator LoadAndSettle(string scenePath)
    {
        yield return SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);

        // PlayerInput pairs devices in OnEnable; give it a frame to settle.
        yield return null;
        yield return null;
    }
}
