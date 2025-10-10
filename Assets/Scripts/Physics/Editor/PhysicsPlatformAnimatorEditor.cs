using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(PhysicsPlatformAnimator))]
public class PhysicsPlatformAnimatorEditor : Editor
{
    private SerializedProperty waypointsProp;
    private SerializedProperty totalAnimationTimeProp;
    private SerializedProperty easingCurveProp;
    private SerializedProperty modeProp;
    private SerializedProperty playOnStartProp;
    private SerializedProperty onWaypointReachedProp;
    private SerializedProperty onCycleCompleteProp;

    private int lastWaypointCount = 0;

    private void OnEnable()
    {
        waypointsProp = serializedObject.FindProperty("waypoints");
        totalAnimationTimeProp = serializedObject.FindProperty("totalAnimationTime");
        easingCurveProp = serializedObject.FindProperty("easingCurve");
        modeProp = serializedObject.FindProperty("mode");
        playOnStartProp = serializedObject.FindProperty("playOnStart");
        onWaypointReachedProp = serializedObject.FindProperty("onWaypointReached");
        onCycleCompleteProp = serializedObject.FindProperty("onCycleComplete");

        lastWaypointCount = waypointsProp.arraySize;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        PhysicsPlatformAnimator animator = (PhysicsPlatformAnimator)target;

        EditorGUILayout.LabelField("Waypoint Configuration", EditorStyles.boldLabel);

        int newSize = EditorGUILayout.IntField("Number of Waypoints", waypointsProp.arraySize);

        // Detect if a waypoint was added
        if (newSize > waypointsProp.arraySize)
        {
            int elementsToAdd = newSize - waypointsProp.arraySize;
            for (int i = 0; i < elementsToAdd; i++)
            {
                waypointsProp.InsertArrayElementAtIndex(waypointsProp.arraySize);
                int newIndex = waypointsProp.arraySize - 1;

                // Create a new GameObject for this waypoint
                GameObject waypointObj = new GameObject($"Waypoint {newIndex}");
                waypointObj.transform.SetParent(animator.transform);
                waypointObj.transform.localPosition = Vector3.zero;
                waypointObj.transform.localRotation = Quaternion.identity;

                // Set the transform reference
                SerializedProperty newElement = waypointsProp.GetArrayElementAtIndex(newIndex);
                SerializedProperty transformProp = newElement.FindPropertyRelative("transform");
                SerializedProperty pauseTimeProp = newElement.FindPropertyRelative("pauseTime");
                SerializedProperty normalizedTimeProp = newElement.FindPropertyRelative("normalizedTime");

                transformProp.objectReferenceValue = waypointObj.transform;
                pauseTimeProp.floatValue = 0f;

                // Auto-calculate normalized time
                if (waypointsProp.arraySize > 1)
                {
                    normalizedTimeProp.floatValue = (float)newIndex / (waypointsProp.arraySize - 1);
                }
                else
                {
                    normalizedTimeProp.floatValue = 0f;
                }

                Undo.RegisterCreatedObjectUndo(waypointObj, "Create Waypoint");
            }
        }
        else if (newSize < waypointsProp.arraySize)
        {
            // Remove waypoints
            int elementsToRemove = waypointsProp.arraySize - newSize;
            for (int i = 0; i < elementsToRemove; i++)
            {
                int indexToRemove = waypointsProp.arraySize - 1;
                SerializedProperty elementToRemove = waypointsProp.GetArrayElementAtIndex(indexToRemove);
                SerializedProperty transformProp = elementToRemove.FindPropertyRelative("transform");

                // Destroy the GameObject if it's a child
                if (transformProp.objectReferenceValue != null)
                {
                    Transform t = (Transform)transformProp.objectReferenceValue;
                    if (t.parent == animator.transform)
                    {
                        Undo.DestroyObjectImmediate(t.gameObject);
                    }
                }

                waypointsProp.DeleteArrayElementAtIndex(indexToRemove);
            }
        }

        EditorGUILayout.Space();

        // Display waypoint array with custom layout
        for (int i = 0; i < waypointsProp.arraySize; i++)
        {
            SerializedProperty element = waypointsProp.GetArrayElementAtIndex(i);
            SerializedProperty transformProp = element.FindPropertyRelative("transform");
            SerializedProperty pauseTimeProp = element.FindPropertyRelative("pauseTime");
            SerializedProperty normalizedTimeProp = element.FindPropertyRelative("normalizedTime");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Waypoint {i}", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(transformProp, new GUIContent("Transform"));
            EditorGUILayout.PropertyField(pauseTimeProp, new GUIContent("Pause Time"));
            EditorGUILayout.PropertyField(normalizedTimeProp, new GUIContent("Normalized Time"));

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Animation Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(totalAnimationTimeProp);
        EditorGUILayout.PropertyField(easingCurveProp);
        EditorGUILayout.PropertyField(modeProp);
        EditorGUILayout.PropertyField(playOnStartProp);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(onWaypointReachedProp);
        EditorGUILayout.PropertyField(onCycleCompleteProp);

        // Add buttons for runtime control
        if (Application.isPlaying)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Play"))
            {
                animator.Play();
            }
            if (GUILayout.Button("Pause"))
            {
                animator.Pause();
            }
            if (GUILayout.Button("Stop"))
            {
                animator.Stop();
            }
            EditorGUILayout.EndHorizontal();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
