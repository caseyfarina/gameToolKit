using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;
using TMPro;

/// <summary>
/// Editor utility to generate an example scene demonstrating PhysicsBumper
/// Creates bouncy bumper pads that launch a ball with visual and physics feedback
/// </summary>
public class PhysicsBumperExampleGenerator : EditorWindow
{
    [MenuItem("Tools/Examples/Generate PhysicsBumper Example")]
    public static void GenerateExample()
    {
        // Clear selection
        Selection.activeGameObject = null;

        // Load materials
        Material pinkMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/pink.mat");
        Material blueMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/blue.mat");

        if (pinkMat == null || blueMat == null)
        {
            Debug.LogError("Could not find pink.mat or blue.mat in Assets/Materials/!");
            return;
        }

        // Create root container
        GameObject root = new GameObject("PhysicsBumperExample");
        Undo.RegisterCreatedObjectUndo(root, "Generate PhysicsBumper Example");

        // Generate components
        GameObject canvas = CreateCanvas(root.transform);
        GameObject ball = CreateBall(root.transform, blueMat);
        GameObject bumper1 = CreateBumper(root.transform, new Vector3(-3f, 0.5f, 0f), pinkMat, "Bumper Left");
        GameObject bumper2 = CreateBumper(root.transform, new Vector3(3f, 0.5f, 0f), pinkMat, "Bumper Right");
        GameObject bumper3 = CreateBumper(root.transform, new Vector3(0f, 0.5f, -3f), pinkMat, "Bumper Back");
        GameObject ground = CreateGround(root.transform);

        // Add annotations
        CreateAnnotation(canvas.transform, new Vector2(0f, 400f), "PhysicsBumper Demo\n\nRoll the ball into the pink bumpers!\nThey will launch it away with force", 48);
        CreateAnnotation(canvas.transform, new Vector2(0f, -400f), "Use WASD to roll the ball\nBumpers have cooldown and emission effects", 32);

        // Position camera
        SetupCamera();

        // Select root in hierarchy
        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);

        Debug.Log("PhysicsBumper example generated! Press Play and roll into the pink bumpers.");
    }

    private static GameObject CreateCanvas(Transform parent)
    {
        // Create EventSystem if needed
        if (GameObject.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Create Canvas
        GameObject canvasObj = new GameObject("Canvas");
        canvasObj.transform.SetParent(parent);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        return canvasObj;
    }

    private static GameObject CreateAnnotation(Transform parent, Vector2 position, string text, float fontSize)
    {
        GameObject textObj = new GameObject("Annotation");
        textObj.transform.SetParent(parent);

        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(1400f, 300f);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;

        return textObj;
    }

    private static GameObject CreateBall(Transform parent, Material ballMat)
    {
        return ExamplePlayerBallFactory.CreatePlayerBall(parent, new Vector3(0f, 2f, 0f), ballMat);
    }

    private static GameObject CreateBumper(Transform parent, Vector3 position, Material bumperMat, string bumperName)
    {
        GameObject bumper = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bumper.name = bumperName;
        bumper.transform.SetParent(parent);
        bumper.transform.localPosition = position;
        bumper.transform.localScale = new Vector3(1f, 0.5f, 1f);

        // Apply material
        Renderer renderer = bumper.GetComponent<Renderer>();
        Material matInstance = new Material(bumperMat);
        renderer.sharedMaterial = matInstance;

        // Add PhysicsBumper
        PhysicsBumper physicsBumper = bumper.AddComponent<PhysicsBumper>();

        // Configure via SerializedObject
        SerializedObject so = new SerializedObject(physicsBumper);
        so.FindProperty("forceAmount").floatValue = 15f;
        so.FindProperty("forceMode").enumValueIndex = (int)ForceMode.Impulse;
        so.FindProperty("forceDirection").enumValueIndex = 1; // Radial
        so.FindProperty("cooldownDuration").floatValue = 0.5f;
        so.FindProperty("useScaleAnimation").boolValue = true;
        so.FindProperty("targetScale").vector3Value = new Vector3(1.2f, 1.2f, 1.2f);
        so.FindProperty("animationDuration").floatValue = 0.2f;
        so.FindProperty("useEmissionEffect").boolValue = true;
        so.FindProperty("emissionPropertyMode").enumValueIndex = 0; // Color
        so.FindProperty("emissionColorProperty").stringValue = "_EmissionColor";
        so.FindProperty("targetEmissionColor").colorValue = new Color(2f, 0.5f, 0.5f, 1f);
        so.ApplyModifiedProperties();

        return bumper;
    }

    private static GameObject CreateGround(Transform parent)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(parent);
        ground.transform.localPosition = Vector3.zero;
        ground.transform.localScale = new Vector3(2f, 1f, 2f);

        // Gray material
        Renderer renderer = ground.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(0.3f, 0.3f, 0.3f);
        renderer.sharedMaterial = mat;

        return ground;
    }

    private static void SetupCamera()
    {
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.position = new Vector3(0f, 8f, -8f);
            mainCam.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
        }
    }
}
