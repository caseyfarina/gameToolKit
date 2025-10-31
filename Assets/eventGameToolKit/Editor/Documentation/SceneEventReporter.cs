using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;

public class SceneEventReporter : EditorWindow
{
    private SceneAsset targetScene;
    private string outputFolder = "Assets/SceneReports";
    private int gameObjectLimit = 100;

    [MenuItem("Tools/Scene Event Reporter")]
    public static void ShowWindow()
    {
        GetWindow<SceneEventReporter>("Scene Event Reporter");
    }

    private void OnGUI()
    {
        GUILayout.Label("Scene UnityEvent Report Generator", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        targetScene = (SceneAsset)EditorGUILayout.ObjectField("Target Scene", targetScene, typeof(SceneAsset), false);

        EditorGUILayout.Space();

        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);

        EditorGUILayout.Space();

        gameObjectLimit = EditorGUILayout.IntField("GameObject Limit", gameObjectLimit);
        EditorGUILayout.HelpBox($"Processing will stop after {gameObjectLimit} GameObjects to prevent performance issues.", MessageType.Info);

        EditorGUILayout.Space();

        GUI.enabled = targetScene != null;
        if (GUILayout.Button("Generate Report", GUILayout.Height(30)))
        {
            GenerateReport();
        }
        GUI.enabled = true;
    }

    private void GenerateReport()
    {
        if (targetScene == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a scene to analyze.", "OK");
            return;
        }

        string scenePath = AssetDatabase.GetAssetPath(targetScene);

        // Ensure output folder exists
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        // Load scene additively
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

        try
        {
            // Generate report
            StringBuilder report = new StringBuilder();
            report.AppendLine($"SCENE: {scene.name}");
            report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine();

            GameObject[] rootObjects = scene.GetRootGameObjects();
            List<GameObject> allObjects = new List<GameObject>();

            // Collect all GameObjects in hierarchy
            foreach (GameObject root in rootObjects)
            {
                CollectGameObjects(root, allObjects);

                if (allObjects.Count >= gameObjectLimit)
                    break;
            }

            int processedCount = Mathf.Min(allObjects.Count, gameObjectLimit);
            report.AppendLine($"GameObjects Processed: {processedCount} / {gameObjectLimit} (limit)");

            if (allObjects.Count > gameObjectLimit)
            {
                report.AppendLine($"WARNING: Scene contains {allObjects.Count} GameObjects. Only processing first {gameObjectLimit}.");
            }

            report.AppendLine();
            report.AppendLine("================================================================================");
            report.AppendLine();

            // Process each GameObject
            for (int i = 0; i < processedCount; i++)
            {
                ProcessGameObject(allObjects[i], report);
            }

            // Save report
            string fileName = $"{scene.name}_EventReport_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            string filePath = Path.Combine(outputFolder, fileName);
            File.WriteAllText(filePath, report.ToString());

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Success",
                $"Report generated successfully!\n\nProcessed: {processedCount} GameObjects\nSaved to: {filePath}",
                "OK");

            // Ping the file in the project window
            var reportAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(filePath);
            EditorGUIUtility.PingObject(reportAsset);
        }
        finally
        {
            // Close the scene
            EditorSceneManager.CloseScene(scene, false);
        }
    }

    private void CollectGameObjects(GameObject obj, List<GameObject> collection)
    {
        if (collection.Count >= gameObjectLimit)
            return;

        collection.Add(obj);

        foreach (Transform child in obj.transform)
        {
            CollectGameObjects(child.gameObject, collection);

            if (collection.Count >= gameObjectLimit)
                return;
        }
    }

    private void ProcessGameObject(GameObject obj, StringBuilder report)
    {
        Component[] components = obj.GetComponents<Component>();
        bool hasUnityEvents = false;
        StringBuilder objectReport = new StringBuilder();

        // Check if this GameObject has any configured UnityEvents
        foreach (Component component in components)
        {
            if (component == null) continue;

            StringBuilder componentReport = new StringBuilder();
            bool componentHasEvents = ProcessComponent(component, componentReport);

            if (componentHasEvents)
            {
                if (!hasUnityEvents)
                {
                    // First event found - write GameObject header
                    string path = GetGameObjectPath(obj);
                    string activeStatus = obj.activeInHierarchy ? "ACTIVE" : "INACTIVE";
                    objectReport.AppendLine($"=== {obj.name} ({path}) [{activeStatus}] ===");
                    hasUnityEvents = true;
                }

                objectReport.Append(componentReport);
            }
        }

        // Only add to report if GameObject has events
        if (hasUnityEvents)
        {
            report.Append(objectReport);
            report.AppendLine();
        }
    }

    private bool ProcessComponent(Component component, StringBuilder componentReport)
    {
        SerializedObject so = new SerializedObject(component);
        SerializedProperty prop = so.GetIterator();
        bool hasEvents = false;

        // Iterate through all serialized properties
        while (prop.NextVisible(true))
        {
            // Check for UnityEvent properties
            if (prop.propertyType == SerializedPropertyType.Generic &&
                prop.type.Contains("UnityEvent"))
            {
                SerializedProperty persistentCalls = prop.FindPropertyRelative("m_PersistentCalls.m_Calls");

                if (persistentCalls != null && persistentCalls.isArray && persistentCalls.arraySize > 0)
                {
                    if (!hasEvents)
                    {
                        // First event in this component - write component header
                        componentReport.AppendLine($"Component: {component.GetType().Name}");
                        hasEvents = true;
                    }

                    componentReport.AppendLine($"  {prop.displayName}:");

                    // List all listeners
                    for (int i = 0; i < persistentCalls.arraySize; i++)
                    {
                        SerializedProperty call = persistentCalls.GetArrayElementAtIndex(i);

                        var target = call.FindPropertyRelative("m_Target").objectReferenceValue;
                        var methodName = call.FindPropertyRelative("m_MethodName").stringValue;
                        var mode = (PersistentListenerMode)call.FindPropertyRelative("m_Mode").enumValueIndex;

                        // FIX 4: Check for missing target and highlight
                        if (target == null)
                        {
                            componentReport.AppendLine($"    !!! MISSING TARGET !!! -> Calls method '{methodName}'");
                        }
                        else if (!string.IsNullOrEmpty(methodName))
                        {
                            string argumentInfo = GetArgumentInfo(call, mode, target);
                            componentReport.AppendLine($"    → {target.name}.{methodName}(){argumentInfo}");
                        }
                    }

                    componentReport.AppendLine();
                }
            }
        }

        return hasEvents;
    }

    // FIX 3: Added target parameter to get object type for more descriptive reporting
    private string GetArgumentInfo(SerializedProperty call, PersistentListenerMode mode, UnityEngine.Object target)
    {
        SerializedProperty arguments = call.FindPropertyRelative("m_Arguments");

        switch (mode)
        {
            case PersistentListenerMode.Int:
                int intArg = arguments.FindPropertyRelative("m_IntArgument").intValue;
                return $" [int: {intArg}]";

            case PersistentListenerMode.Float:
                float floatArg = arguments.FindPropertyRelative("m_FloatArgument").floatValue;
                return $" [float: {floatArg}]";

            case PersistentListenerMode.String:
                string strArg = arguments.FindPropertyRelative("m_StringArgument").stringValue;
                return $" [string: \"{strArg}\"]";

            case PersistentListenerMode.Bool:
                bool boolArg = arguments.FindPropertyRelative("m_BoolArgument").boolValue;
                return $" [bool: {boolArg}]";

            case PersistentListenerMode.Object:
                var objArg = arguments.FindPropertyRelative("m_ObjectArgument").objectReferenceValue;
                if (objArg != null)
                {
                    // FIX 3: Include the type name for clarity
                    return $" [object: {objArg.name} ({objArg.GetType().Name})]";
                }
                break;
        }

        return "";
    }

    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform current = obj.transform.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}
