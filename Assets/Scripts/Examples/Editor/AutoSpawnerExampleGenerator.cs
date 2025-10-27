using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using TMPro;

/// <summary>
/// Editor utility to generate an example scene demonstrating ActionAutoSpawner
/// Creates an automatic spawner that continuously generates falling objects
/// </summary>
public class AutoSpawnerExampleGenerator : EditorWindow
{
    [MenuItem("Tools/Examples/Generate AutoSpawner Example")]
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
        GameObject root = new GameObject("AutoSpawnerExample");
        Undo.RegisterCreatedObjectUndo(root, "Generate AutoSpawner Example");

        // Generate components
        GameObject canvas = CreateCanvas(root.transform);
        GameObject player = CreatePlayer(root.transform, blueMat);
        GameObject spawner = CreateSpawner(root.transform, pinkMat);
        GameObject ground = CreateGround(root.transform);

        // Add annotations
        CreateAnnotation(canvas.transform, new Vector2(0f, 450f), "Auto Spawner Demo\n\nObjects spawn automatically!", 46);
        CreateAnnotation(canvas.transform, new Vector2(0f, -400f), "WASD to Move | Avoid falling pink cubes", 36);

        // Position camera
        SetupCamera();

        // Select root in hierarchy
        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);

        Debug.Log("AutoSpawner example generated! Press Play and watch objects spawn!");
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
        rect.sizeDelta = new Vector2(1600f, 300f);

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

    private static GameObject CreateSpawner(Transform parent, Material spawnedObjectMat)
    {
        GameObject spawner = new GameObject("AutoSpawner");
        spawner.transform.SetParent(parent);
        spawner.transform.localPosition = new Vector3(0f, 8f, 0f);

        // Create prefab for spawning
        GameObject prefabTemplate = CreateSpawnedObjectPrefab(spawnedObjectMat);

        // Add auto spawner
        ActionAutoSpawner autoSpawner = spawner.AddComponent<ActionAutoSpawner>();

        SerializedObject so = new SerializedObject(autoSpawner);
        so.FindProperty("spawnOnStart").boolValue = true;
        so.FindProperty("minSpawnInterval").floatValue = 1.5f;
        so.FindProperty("maxSpawnInterval").floatValue = 3f;
        so.FindProperty("spawnVariance").floatValue = 3f;
        so.FindProperty("spawnAtThisTransform").boolValue = true;

        // Add prefab to spawn list
        SerializedProperty prefabs = so.FindProperty("prefabsToSpawn");
        prefabs.arraySize = 1;
        prefabs.GetArrayElementAtIndex(0).objectReferenceValue = prefabTemplate;

        so.ApplyModifiedProperties();

        // Save prefab to Assets
        string prefabPath = "Assets/SpawnedCubePrefab.prefab";
        prefabTemplate = PrefabUtility.SaveAsPrefabAsset(prefabTemplate, prefabPath);

        // Update spawner reference to saved prefab
        SerializedObject so2 = new SerializedObject(autoSpawner);
        SerializedProperty prefabs2 = so2.FindProperty("prefabsToSpawn");
        prefabs2.GetArrayElementAtIndex(0).objectReferenceValue = prefabTemplate;
        so2.ApplyModifiedProperties();

        // Add visual indicator
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        indicator.name = "SpawnerIndicator";
        indicator.transform.SetParent(spawner.transform);
        indicator.transform.localPosition = Vector3.zero;
        indicator.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

        // Yellow material
        Material indicatorMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        indicatorMat.color = Color.yellow;
        indicator.GetComponent<Renderer>().sharedMaterial = indicatorMat;

        // Remove collider from indicator
        Object.DestroyImmediate(indicator.GetComponent<Collider>());

        return spawner;
    }

    private static GameObject CreateSpawnedObjectPrefab(Material objectMat)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = "SpawnedCube";
        obj.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

        // Apply material
        obj.GetComponent<Renderer>().sharedMaterial = objectMat;

        // Add rigidbody for falling
        Rigidbody rb = obj.AddComponent<Rigidbody>();
        rb.mass = 1f;

        // Add component to destroy after time
        DestroyAfterTime destroyScript = obj.AddComponent<DestroyAfterTime>();
        SerializedObject so = new SerializedObject(destroyScript);
        so.FindProperty("lifetime").floatValue = 10f;
        so.ApplyModifiedProperties();

        return obj;
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
            mainCam.transform.position = new Vector3(0f, 8f, -10f);
            mainCam.transform.rotation = Quaternion.Euler(40f, 0f, 0f);
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
        }
    }
}

/// <summary>
/// Simple helper component to destroy objects after a set lifetime
/// </summary>
public class DestroyAfterTime : MonoBehaviour
{
    public float lifetime = 10f;
    private float elapsedTime = 0f;

    void Update()
    {
        elapsedTime += Time.deltaTime;
        if (elapsedTime >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
