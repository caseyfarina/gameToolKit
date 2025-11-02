using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Editor utility to generate an example scene demonstrating CharacterControllerCC
/// Creates a 3D character with WASD movement and jump using the Cameron model
/// </summary>
public class CharacterControllerExampleGenerator : EditorWindow
{
    [MenuItem("Tools/Examples/Generate CharacterController Example")]
    public static void GenerateExample()
    {
        // Clear selection
        Selection.activeGameObject = null;

        // Load Cameron model
        GameObject cameronPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Samples/Cinemachine/3.1.2/Shared Assets/Cameron/Model/Cameron_Model.fbx");

        if (cameronPrefab == null)
        {
            Debug.LogError("Could not find Cameron model! Please ensure Cinemachine samples are imported.");
            return;
        }

        // Create root container
        GameObject root = new GameObject("CharacterControllerExample");
        Undo.RegisterCreatedObjectUndo(root, "Generate CharacterController Example");

        // Generate components
        GameObject canvas = CreateCanvas(root.transform);
        GameObject character = CreateCharacter(root.transform, cameronPrefab);
        GameObject ground = CreateGround(root.transform);
        GameObject obstacle1 = CreateObstacle(root.transform, new Vector3(5f, 0.5f, 0f));
        GameObject obstacle2 = CreateObstacle(root.transform, new Vector3(-5f, 0.5f, 3f));

        // Add annotations
        CreateAnnotation(canvas.transform, new Vector2(0f, 450f), "Character Controller Demo", 56);
        CreateAnnotation(canvas.transform, new Vector2(0f, -450f), "WASD to Move | Space to Jump", 40);

        // Position camera
        SetupCamera(character);

        // Select root in hierarchy
        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);

        Debug.Log("CharacterController example generated! Press Play and use WASD + Space to move.");
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
        rect.sizeDelta = new Vector2(1400f, 200f);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;

        return textObj;
    }

    private static GameObject CreateCharacter(Transform parent, GameObject modelPrefab)
    {
        // Create character root
        GameObject character = new GameObject("Character");
        character.transform.SetParent(parent);
        character.transform.localPosition = new Vector3(0f, 1f, 0f);
        character.tag = "Player";

        // Add CharacterController (auto-added by CharacterControllerCC but explicit is clearer)
        CharacterController cc = character.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.5f;
        cc.center = new Vector3(0f, 1f, 0f);

        // Add CharacterControllerCC (Unity 6 Third Person Controller style)
        CharacterControllerCC controller = character.AddComponent<CharacterControllerCC>();

        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("moveSpeed").floatValue = 5f;
        so.FindProperty("jumpHeight").floatValue = 1.5f;
        so.FindProperty("gravity").floatValue = -15f;
        so.FindProperty("groundedOffset").floatValue = -0.14f;
        so.FindProperty("groundedRadius").floatValue = 0.5f;
        so.ApplyModifiedProperties();

        // Instantiate Cameron model as child
        GameObject model = Object.Instantiate(modelPrefab);
        model.name = "CameronModel";
        model.transform.SetParent(character.transform);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;

        return character;
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
        mat.color = new Color(0.4f, 0.4f, 0.4f);
        renderer.sharedMaterial = mat;

        return ground;
    }

    private static GameObject CreateObstacle(Transform parent, Vector3 position)
    {
        GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstacle.name = "Obstacle";
        obstacle.transform.SetParent(parent);
        obstacle.transform.localPosition = position;
        obstacle.transform.localScale = new Vector3(1f, 1f, 1f);

        // Load pink material
        Material pinkMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/pink.mat");
        if (pinkMat != null)
        {
            obstacle.GetComponent<Renderer>().sharedMaterial = pinkMat;
        }

        return obstacle;
    }

    private static void SetupCamera(GameObject target)
    {
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.position = new Vector3(0f, 5f, -10f);
            mainCam.transform.rotation = Quaternion.Euler(20f, 0f, 0f);
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.4f, 0.6f, 0.8f);
        }
    }
}
