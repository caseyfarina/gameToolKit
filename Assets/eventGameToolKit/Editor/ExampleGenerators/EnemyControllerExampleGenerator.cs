using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;
using TMPro;

/// <summary>
/// Editor utility to generate an example scene demonstrating PhysicsEnemyController
/// Creates AI enemies that chase the player ball with different behaviors
/// </summary>
public class EnemyControllerExampleGenerator : EditorWindow
{
    [MenuItem("Tools/Examples/Generate EnemyController Example")]
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
        GameObject root = new GameObject("EnemyControllerExample");
        Undo.RegisterCreatedObjectUndo(root, "Generate EnemyController Example");

        // Generate components
        GameObject canvas = CreateCanvas(root.transform);
        GameObject player = CreatePlayer(root.transform, blueMat);
        GameObject enemy1 = CreateEnemy(root.transform, new Vector3(-5f, 1f, 0f), pinkMat, "Enemy_Walker", 0);
        GameObject enemy2 = CreateEnemy(root.transform, new Vector3(5f, 1f, 0f), pinkMat, "Enemy_Jumper", 2);
        GameObject ground = CreateGround(root.transform);

        // Add annotations
        CreateAnnotation(canvas.transform, new Vector2(0f, 450f), "Enemy AI Demo\n\nPink enemies will chase you!", 48);
        CreateAnnotation(canvas.transform, new Vector2(0f, -450f), "WASD to Move\nOne enemy walks, one jumps", 36);

        // Position camera
        SetupCamera();

        // Select root in hierarchy
        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);

        Debug.Log("EnemyController example generated! Press Play and watch the enemies chase you!");
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

    private static GameObject CreatePlayer(Transform parent, Material playerMat)
    {
        return ExamplePlayerBallFactory.CreatePlayerBall(parent, new Vector3(0f, 1f, 0f), playerMat);
    }

    private static GameObject CreateEnemy(Transform parent, Vector3 position, Material enemyMat, string enemyName, int jumpMode)
    {
        GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        enemy.name = enemyName;
        enemy.transform.SetParent(parent);
        enemy.transform.localPosition = position;
        enemy.tag = "Enemy";

        // Apply material
        enemy.GetComponent<Renderer>().sharedMaterial = enemyMat;

        // Add physics (will be auto-configured by PhysicsEnemyController)
        Rigidbody rb = enemy.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        // Add enemy controller
        PhysicsEnemyController controller = enemy.AddComponent<PhysicsEnemyController>();

        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("playerTag").stringValue = "Player";
        so.FindProperty("moveSpeed").floatValue = 3f;
        so.FindProperty("detectionRange").floatValue = 15f;
        so.FindProperty("jumpMode").enumValueIndex = jumpMode; // 0 = None, 2 = OnCollision
        so.FindProperty("jumpForce").floatValue = 5f;
        so.ApplyModifiedProperties();

        return enemy;
    }

    private static GameObject CreateGround(Transform parent)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(parent);
        ground.transform.localPosition = Vector3.zero;
        ground.transform.localScale = new Vector3(3f, 1f, 3f);

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
            mainCam.transform.position = new Vector3(0f, 10f, -10f);
            mainCam.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
        }
    }
}
