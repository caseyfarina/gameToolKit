using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Custom inspector for PhysicsForceZone.
/// Shows conditional fields and a play-mode zone readout with per-object force status.
/// </summary>
[CustomEditor(typeof(PhysicsForceZone))]
public class PhysicsForceZoneEditor : Editor
{
    private SerializedProperty targetTagProp;
    private SerializedProperty forceDirectionProp;
    private SerializedProperty randomDirectionOffsetProp;
    private SerializedProperty forceSpaceProp;
    private SerializedProperty minForceProp;
    private SerializedProperty maxForceProp;
    private SerializedProperty forceModeProp;
    private SerializedProperty applyToFirstProp;
    private SerializedProperty oneForcePerStayProp;
    private SerializedProperty applyOnEnterProp;
    private SerializedProperty applyOnExitProp;
    private SerializedProperty onForceAppliedProp;
    private SerializedProperty onForceAppliedToObjectProp;

    private void OnEnable()
    {
        targetTagProp               = serializedObject.FindProperty("targetTag");
        forceDirectionProp          = serializedObject.FindProperty("forceDirection");
        randomDirectionOffsetProp   = serializedObject.FindProperty("randomDirectionOffset");
        forceSpaceProp              = serializedObject.FindProperty("forceSpace");
        minForceProp                = serializedObject.FindProperty("minForce");
        maxForceProp                = serializedObject.FindProperty("maxForce");
        forceModeProp               = serializedObject.FindProperty("forceMode");
        applyToFirstProp            = serializedObject.FindProperty("applyToFirst");
        oneForcePerStayProp         = serializedObject.FindProperty("oneForcePerStay");
        applyOnEnterProp            = serializedObject.FindProperty("applyOnEnter");
        applyOnExitProp             = serializedObject.FindProperty("applyOnExit");
        onForceAppliedProp          = serializedObject.FindProperty("onForceApplied");
        onForceAppliedToObjectProp  = serializedObject.FindProperty("onForceAppliedToObject");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var zone = (PhysicsForceZone)target;

        // ── Tag Filter ───────────────────────────
        EditorGUILayout.LabelField("Tag Filter", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(targetTagProp, new GUIContent("Target Tag"));

        EditorGUILayout.Space();

        // ── Force Direction ──────────────────────
        EditorGUILayout.LabelField("Force Direction", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(forceDirectionProp,        new GUIContent("Direction"));
        EditorGUILayout.PropertyField(randomDirectionOffsetProp, new GUIContent("Random Offset (per axis ±)"));
        EditorGUILayout.PropertyField(forceSpaceProp,            new GUIContent("Space"));

        EditorGUILayout.Space();

        // ── Force Magnitude ──────────────────────
        EditorGUILayout.LabelField("Force Magnitude", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Force Range", GUILayout.Width(EditorGUIUtility.labelWidth));
        EditorGUILayout.PropertyField(minForceProp, GUIContent.none);
        EditorGUILayout.LabelField("to", GUILayout.Width(20));
        EditorGUILayout.PropertyField(maxForceProp, GUIContent.none);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.PropertyField(forceModeProp, new GUIContent("Force Mode"));

        EditorGUILayout.Space();

        // ── Targeting ───────────────────────────
        EditorGUILayout.LabelField("Targeting", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(applyToFirstProp,    new GUIContent("Apply To First Only"));
        EditorGUILayout.PropertyField(oneForcePerStayProp, new GUIContent("One Force Per Stay"));

        if (oneForcePerStayProp.boolValue)
            EditorGUILayout.HelpBox("Each object can only be forced once per stay. Tracking resets automatically when the object leaves the zone.", MessageType.None);

        EditorGUILayout.Space();

        // ── Auto Apply ──────────────────────────
        EditorGUILayout.LabelField("Auto Apply", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(applyOnEnterProp, new GUIContent("Apply On Enter"));
        EditorGUILayout.PropertyField(applyOnExitProp,  new GUIContent("Apply On Exit"));

        EditorGUILayout.Space();

        // ── Play-mode zone readout ───────────────
        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField("Zone Status", EditorStyles.boldLabel);

            List<Rigidbody> inZone = zone.GetObjectsInZone();
            int count = inZone.Count;

            EditorGUILayout.LabelField($"Objects in zone: {count}");

            if (count > 0)
            {
                EditorGUI.indentLevel++;
                foreach (var rb in inZone)
                {
                    if (rb == null) continue;
                    bool forced = zone.HasBeenForced(rb);

                    EditorGUILayout.BeginHorizontal();
                    GUIStyle dot = new GUIStyle(EditorStyles.miniLabel);
                    dot.normal.textColor = forced ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.3f, 0.9f, 0.3f);
                    EditorGUILayout.LabelField(forced ? "● (forced)" : "● (ready)", dot, GUILayout.Width(80));
                    EditorGUILayout.LabelField(rb.gameObject.name, EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply Force"))
                zone.ApplyForce();
            if (GUILayout.Button("Reset Tracking"))
                zone.ResetTracking();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            Repaint(); // keep readout live
        }
        else
        {
            EditorGUILayout.HelpBox("Enter Play Mode to see objects in zone and test force application.", MessageType.Info);
        }

        // ── Events ───────────────────────────────
        EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(onForceAppliedProp,         new GUIContent("On Force Applied (count)"));
        EditorGUILayout.PropertyField(onForceAppliedToObjectProp, new GUIContent("On Force Applied To Object"));

        serializedObject.ApplyModifiedProperties();
    }
}
