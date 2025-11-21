using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

/// <summary>
/// Custom editor for ActionDisplayImage that provides a visual preview in the editor
/// </summary>
[CustomEditor(typeof(ActionDisplayImage))]
public class ActionDisplayImageEditor : Editor
{
    private bool showPreview = false;
    private GameObject previewCanvas;
    private Image previewImage;

    private void OnEnable()
    {
        // Clean up any existing preview when editor is enabled
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private void OnDisable()
    {
        // Clean up preview when editor is disabled
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        HidePreview();
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // Hide preview when entering play mode
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            HidePreview();
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw default script field
        GUI.enabled = false;
        EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((ActionDisplayImage)target), typeof(ActionDisplayImage), false);
        GUI.enabled = true;

        EditorGUILayout.Space();

        // Get properties
        SerializedProperty defaultImageProp = serializedObject.FindProperty("defaultImage");
        SerializedProperty imagePositionProp = serializedObject.FindProperty("imagePosition");
        SerializedProperty imageSizeProp = serializedObject.FindProperty("imageSize");
        SerializedProperty timeOnScreenProp = serializedObject.FindProperty("timeOnScreen");
        SerializedProperty useFadingProp = serializedObject.FindProperty("useFading");
        SerializedProperty fadeDurationProp = serializedObject.FindProperty("fadeDuration");
        SerializedProperty useScalingProp = serializedObject.FindProperty("useScaling");
        SerializedProperty startScaleProp = serializedObject.FindProperty("startScale");
        SerializedProperty targetScaleProp = serializedObject.FindProperty("targetScale");
        SerializedProperty scaleDurationProp = serializedObject.FindProperty("scaleDuration");

        // Draw properties without duplicate headers (headers are in the script)
        EditorGUILayout.PropertyField(defaultImageProp);

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(imagePositionProp);
        EditorGUILayout.PropertyField(imageSizeProp);
        bool positionOrSizeChanged = EditorGUI.EndChangeCheck();

        EditorGUILayout.PropertyField(timeOnScreenProp);
        EditorGUILayout.PropertyField(useFadingProp);

        if (useFadingProp.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(fadeDurationProp);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.PropertyField(useScalingProp);

        if (useScalingProp.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(startScaleProp);
            EditorGUILayout.PropertyField(targetScaleProp);
            EditorGUILayout.PropertyField(scaleDurationProp);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.PropertyField(serializedObject.FindProperty("onImageDisplayStart"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onImageDisplayComplete"));

        serializedObject.ApplyModifiedProperties();

        // Update preview if position/size changed and preview is visible
        if (positionOrSizeChanged && showPreview)
        {
            UpdatePreview();
        }

        // Preview Controls
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Editor Preview", EditorStyles.boldLabel);

        Sprite defaultImage = serializedObject.FindProperty("defaultImage").objectReferenceValue as Sprite;

        if (defaultImage == null)
        {
            EditorGUILayout.HelpBox("Assign a Default Image to enable preview.", MessageType.Info);
            GUI.enabled = false;
        }

        EditorGUILayout.BeginHorizontal();

        if (!showPreview)
        {
            if (GUILayout.Button("Show Preview", GUILayout.Height(30)))
            {
                showPreview = true;
                ShowPreview();
            }
        }
        else
        {
            if (GUILayout.Button("Hide Preview", GUILayout.Height(30)))
            {
                showPreview = false;
                HidePreview();
            }
        }

        EditorGUILayout.EndHorizontal();

        GUI.enabled = true;

        if (showPreview)
        {
            EditorGUILayout.HelpBox("Preview is visible in the Scene view. Use Image Position and Image Size to adjust placement.", MessageType.Info);
        }

        EditorGUILayout.Space();
    }

    private void ShowPreview()
    {
        if (previewCanvas != null)
        {
            HidePreview();
        }

        ActionDisplayImage displayImage = (ActionDisplayImage)target;

        // Get serialized properties
        Sprite defaultImage = serializedObject.FindProperty("defaultImage").objectReferenceValue as Sprite;
        Vector2 imagePosition = serializedObject.FindProperty("imagePosition").vector2Value;
        Vector2 imageSize = serializedObject.FindProperty("imageSize").vector2Value;

        // Create Canvas
        previewCanvas = new GameObject("PreviewCanvas_Image");
        previewCanvas.hideFlags = HideFlags.DontSave;

        Canvas canvas = previewCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // High sort order so it's visible

        CanvasScaler scaler = previewCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        previewCanvas.AddComponent<GraphicRaycaster>();

        // Create Image
        GameObject imageObj = new GameObject("PreviewImage");
        imageObj.hideFlags = HideFlags.DontSave;
        imageObj.transform.SetParent(previewCanvas.transform);

        RectTransform rect = imageObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = imagePosition;
        rect.sizeDelta = imageSize;

        previewImage = imageObj.AddComponent<Image>();
        previewImage.sprite = defaultImage;
        previewImage.color = new Color(1f, 1f, 1f, 0.8f); // Slightly transparent for preview

        // Force scene view to repaint
        SceneView.RepaintAll();
    }

    private void UpdatePreview()
    {
        if (previewCanvas == null || previewImage == null)
        {
            return;
        }

        // Update image position and size
        Vector2 imagePosition = serializedObject.FindProperty("imagePosition").vector2Value;
        Vector2 imageSize = serializedObject.FindProperty("imageSize").vector2Value;
        Sprite defaultImage = serializedObject.FindProperty("defaultImage").objectReferenceValue as Sprite;

        RectTransform rect = previewImage.GetComponent<RectTransform>();
        rect.anchoredPosition = imagePosition;
        rect.sizeDelta = imageSize;
        previewImage.sprite = defaultImage;

        SceneView.RepaintAll();
    }

    private void HidePreview()
    {
        if (previewCanvas != null)
        {
            DestroyImmediate(previewCanvas);
            previewCanvas = null;
            previewImage = null;
            SceneView.RepaintAll();
        }
    }
}
