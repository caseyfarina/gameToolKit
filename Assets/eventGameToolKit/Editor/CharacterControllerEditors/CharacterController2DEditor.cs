using UnityEditor;
using UnityEngine;

/// <summary>
/// Hides whichever movement-style section does not apply, so a student setting up a
/// top-down game never sees jump and ground-check fields, and vice versa.
/// </summary>
[CustomEditor(typeof(CharacterController2D))]
public class CharacterController2DEditor : Editor
{
    private static readonly string[] PlatformerOnly =
    {
        "jumpHeight", "coyoteTime", "jumpBufferTime", "gravityScale",
        "maxFallSpeed", "groundLayer", "groundCheckSize", "groundCheckOffset"
    };

    private static readonly string[] TopDownOnly = { "faceMovementDirection" };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty style = serializedObject.FindProperty("movementStyle");
        bool isPlatformer =
            style.enumValueIndex == (int)CharacterController2D.MovementStyle.Platformer;

        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;
        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (prop.name == "m_Script") continue;

            if (!isPlatformer && System.Array.IndexOf(PlatformerOnly, prop.name) >= 0) continue;
            if (isPlatformer && System.Array.IndexOf(TopDownOnly, prop.name) >= 0) continue;

            EditorGUILayout.PropertyField(prop, true);
        }

        serializedObject.ApplyModifiedProperties();

        if (isPlatformer)
        {
            CharacterController2D controller = (CharacterController2D)target;
            SerializedProperty mask = serializedObject.FindProperty("groundLayer");
            if (mask.intValue == 0)
            {
                EditorGUILayout.HelpBox(
                    "Ground Layer is empty, so the character will never detect the floor and " +
                    "jumping will not work. Set it to the layer your ground objects use.",
                    MessageType.Warning);
            }

            if (controller.GetComponent<Collider2D>() == null)
            {
                EditorGUILayout.HelpBox(
                    "No Collider2D on this GameObject. Add one (Capsule Collider 2D suits " +
                    "characters) or the player will fall through the level.",
                    MessageType.Warning);
            }
        }
    }
}
