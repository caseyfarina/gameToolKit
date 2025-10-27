using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor;

/// <summary>
/// Factory class for creating properly configured player ball objects for example scenes.
/// Ensures all necessary components are present and properly wired.
/// </summary>
public static class ExamplePlayerBallFactory
{
    /// <summary>
    /// Creates a fully functional player ball with physics, input, and rendering
    /// </summary>
    public static GameObject CreatePlayerBall(Transform parent, Vector3 position, Material ballMaterial)
    {
        // Create the ball
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        player.name = "Player";
        player.transform.SetParent(parent);
        player.transform.localPosition = position;
        player.tag = "Player";

        // Apply material
        if (ballMaterial != null)
        {
            player.GetComponent<Renderer>().sharedMaterial = ballMaterial;
        }

        // Add physics
        Rigidbody rb = player.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 0.5f;

        // Add player controller
        PhysicsBallPlayerController controller = player.AddComponent<PhysicsBallPlayerController>();

        // Configure controller via SerializedObject
        SerializedObject controllerSO = new SerializedObject(controller);
        controllerSO.FindProperty("moveForce").floatValue = 10f;
        controllerSO.FindProperty("maxVelocity").floatValue = 20f;
        controllerSO.FindProperty("jumpForce").floatValue = 7f;
        controllerSO.FindProperty("groundCheckRadius").floatValue = 0.6f;
        controllerSO.FindProperty("groundLayer").intValue = -1; // Everything
        controllerSO.ApplyModifiedProperties();

        // Add PlayerInput component for Input System
        PlayerInput playerInput = player.AddComponent<PlayerInput>();

        // Load the InputActions asset
        InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");

        if (inputActions != null)
        {
            SerializedObject inputSO = new SerializedObject(playerInput);
            inputSO.FindProperty("m_Actions").objectReferenceValue = inputActions;
            inputSO.FindProperty("m_DefaultControlScheme").stringValue = "";
            inputSO.FindProperty("m_DefaultActionMap").stringValue = "Player";
            inputSO.FindProperty("m_NotificationBehavior").enumValueIndex = 2; // Invoke Unity Events
            inputSO.ApplyModifiedProperties();

            Debug.Log("PlayerInput configured with InputSystem_Actions");
        }
        else
        {
            Debug.LogWarning("Could not find InputSystem_Actions.inputactions - player input may not work! Please ensure the Input Actions asset exists.");
        }

        return player;
    }

    /// <summary>
    /// Creates a simple player ball without Input System (for testing or non-interactive scenes)
    /// </summary>
    public static GameObject CreateStaticPlayerBall(Transform parent, Vector3 position, Material ballMaterial)
    {
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        player.name = "Player";
        player.transform.SetParent(parent);
        player.transform.localPosition = position;
        player.tag = "Player";

        if (ballMaterial != null)
        {
            player.GetComponent<Renderer>().sharedMaterial = ballMaterial;
        }

        Rigidbody rb = player.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 0.5f;

        return player;
    }
}
