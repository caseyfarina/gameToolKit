using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

[CustomEditor(typeof(ActionPlatformAnimator))]
public class ActionPlatformAnimatorEditor : Editor
{
    private ReorderableList waypointList;
    private SerializedProperty waypointsProp;

    private void OnEnable()
    {
        waypointsProp = serializedObject.FindProperty("waypoints");

        waypointList = new ReorderableList(serializedObject, waypointsProp, true, true, true, true);

        waypointList.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Waypoints");
        };

        waypointList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            SerializedProperty element = waypointList.serializedProperty.GetArrayElementAtIndex(index);
            SerializedProperty transformProp = element.FindPropertyRelative("transform");
            SerializedProperty pauseTimeProp = element.FindPropertyRelative("pauseTime");

            rect.y += 2;
            float labelWidth = 80f;
            float fieldHeight = EditorGUIUtility.singleLineHeight;

            // Transform field
            EditorGUI.LabelField(new Rect(rect.x, rect.y, labelWidth, fieldHeight), $"WP {index}");
            EditorGUI.PropertyField(
                new Rect(rect.x + labelWidth, rect.y, rect.width - labelWidth - 100, fieldHeight),
                transformProp,
                GUIContent.none
            );

            // Pause time field
            EditorGUI.LabelField(new Rect(rect.x + rect.width - 95, rect.y, 40, fieldHeight), "Pause");
            EditorGUI.PropertyField(
                new Rect(rect.x + rect.width - 50, rect.y, 50, fieldHeight),
                pauseTimeProp,
                GUIContent.none
            );
        };

        waypointList.onAddCallback = (ReorderableList list) =>
        {
            ActionPlatformAnimator animator = (ActionPlatformAnimator)target;

            int index = list.serializedProperty.arraySize;
            list.serializedProperty.arraySize++;
            list.index = index;

            SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);
            SerializedProperty transformProp = element.FindPropertyRelative("transform");
            SerializedProperty pauseTimeProp = element.FindPropertyRelative("pauseTime");

            // Find or create waypoints container as sibling
            Transform parent = animator.transform.parent;
            Transform waypointsContainer = null;

            string containerName = animator.gameObject.name + "_Waypoints";

            if (parent != null)
            {
                waypointsContainer = parent.Find(containerName);
            }
            else
            {
                GameObject foundContainer = GameObject.Find(containerName);
                if (foundContainer != null)
                {
                    waypointsContainer = foundContainer.transform;
                }
            }

            if (waypointsContainer == null)
            {
                GameObject containerObj = new GameObject(containerName);
                containerObj.transform.SetParent(parent);
                containerObj.transform.position = animator.transform.position;
                containerObj.transform.rotation = Quaternion.identity;
                waypointsContainer = containerObj.transform;
                Undo.RegisterCreatedObjectUndo(containerObj, "Create Waypoints Container");
            }

            // Create waypoint GameObject as child of container
            GameObject waypointObj = new GameObject($"Waypoint_{index}");
            waypointObj.transform.SetParent(waypointsContainer);
            waypointObj.transform.position = animator.transform.position;
            waypointObj.transform.rotation = Quaternion.identity;

            transformProp.objectReferenceValue = waypointObj.transform;
            pauseTimeProp.floatValue = 0f;

            Undo.RegisterCreatedObjectUndo(waypointObj, "Add Waypoint");
        };

        waypointList.onRemoveCallback = (ReorderableList list) =>
        {
            SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(list.index);
            SerializedProperty transformProp = element.FindPropertyRelative("transform");

            // Destroy GameObject if it exists
            if (transformProp.objectReferenceValue != null)
            {
                Transform t = (Transform)transformProp.objectReferenceValue;
                ActionPlatformAnimator animator = (ActionPlatformAnimator)target;

                // Check if it's in the waypoints container
                string containerName = animator.gameObject.name + "_Waypoints";
                if (t.parent != null && t.parent.name == containerName)
                {
                    Undo.DestroyObjectImmediate(t.gameObject);
                }
            }

            ReorderableList.defaultBehaviours.DoRemoveButton(list);
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        ActionPlatformAnimator animator = (ActionPlatformAnimator)target;

        // Draw waypoint list
        waypointList.DoLayoutList();

        EditorGUILayout.Space();

        // Draw other properties
        SerializedProperty totalAnimationTimeProp = serializedObject.FindProperty("totalAnimationTime");
        SerializedProperty easingCurveProp = serializedObject.FindProperty("easingCurve");
        SerializedProperty modeProp = serializedObject.FindProperty("mode");
        SerializedProperty playOnStartProp = serializedObject.FindProperty("playOnStart");
        SerializedProperty onWaypointReachedProp = serializedObject.FindProperty("onWaypointReached");
        SerializedProperty onCycleCompleteProp = serializedObject.FindProperty("onCycleComplete");

        EditorGUILayout.LabelField("Animation Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(totalAnimationTimeProp);
        EditorGUILayout.PropertyField(easingCurveProp);
        EditorGUILayout.PropertyField(modeProp);
        EditorGUILayout.PropertyField(playOnStartProp);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(onWaypointReachedProp);
        EditorGUILayout.PropertyField(onCycleCompleteProp);

        // Runtime controls
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
