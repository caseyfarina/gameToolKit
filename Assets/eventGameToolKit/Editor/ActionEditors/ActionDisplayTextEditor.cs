using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Custom editor for ActionDisplayText that provides a visual preview in the editor
/// </summary>
[CustomEditor(typeof(ActionDisplayText))]
public class ActionDisplayTextEditor : Editor
{
    private bool showPreview = false;
    private GameObject previewCanvas;
    private TextMeshProUGUI previewText;

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
        EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((ActionDisplayText)target), typeof(ActionDisplayText), false);
        GUI.enabled = true;

        EditorGUILayout.Space();

        // Get properties
        SerializedProperty defaultTextProp = serializedObject.FindProperty("defaultText");
        SerializedProperty textPositionProp = serializedObject.FindProperty("textPosition");
        SerializedProperty textSizeProp = serializedObject.FindProperty("textSize");
        SerializedProperty fontSizeProp = serializedObject.FindProperty("fontSize");
        SerializedProperty textAlignmentProp = serializedObject.FindProperty("textAlignment");
        SerializedProperty textColorProp = serializedObject.FindProperty("textColor");
        SerializedProperty fontProp = serializedObject.FindProperty("font");
        SerializedProperty timeOnScreenProp = serializedObject.FindProperty("timeOnScreen");
        SerializedProperty useFadingProp = serializedObject.FindProperty("useFading");
        SerializedProperty fadeDurationProp = serializedObject.FindProperty("fadeDuration");
        SerializedProperty useTypewriterProp = serializedObject.FindProperty("useTypewriter");
        SerializedProperty charactersPerSecondProp = serializedObject.FindProperty("charactersPerSecond");

        // Draw properties without duplicate headers (headers are in the script)
        EditorGUILayout.PropertyField(defaultTextProp);

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(textPositionProp);
        EditorGUILayout.PropertyField(textSizeProp);
        EditorGUILayout.PropertyField(fontSizeProp);
        EditorGUILayout.PropertyField(textAlignmentProp);
        EditorGUILayout.PropertyField(textColorProp);
        bool visualChanged = EditorGUI.EndChangeCheck();

        EditorGUILayout.PropertyField(fontProp);

        EditorGUILayout.PropertyField(timeOnScreenProp);
        EditorGUILayout.PropertyField(useFadingProp);

        if (useFadingProp.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(fadeDurationProp);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.PropertyField(useTypewriterProp);

        if (useTypewriterProp.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(charactersPerSecondProp);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.PropertyField(serializedObject.FindProperty("onTextDisplayStart"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onTextDisplayComplete"));

        serializedObject.ApplyModifiedProperties();

        // Update preview if visual properties changed and preview is visible
        if (visualChanged && showPreview)
        {
            UpdatePreview();
        }

        // Preview Controls
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Editor Preview", EditorStyles.boldLabel);

        string defaultText = serializedObject.FindProperty("defaultText").stringValue;

        if (string.IsNullOrEmpty(defaultText))
        {
            EditorGUILayout.HelpBox("Enter Default Text to enable preview.", MessageType.Info);
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
            EditorGUILayout.HelpBox("Preview is visible in the Scene view. Use Text Position and Text Size to adjust placement.", MessageType.Info);
        }

        EditorGUILayout.Space();
    }

    private void ShowPreview()
    {
        if (previewCanvas != null)
        {
            HidePreview();
        }

        ActionDisplayText displayText = (ActionDisplayText)target;

        // Get serialized properties
        string defaultText = serializedObject.FindProperty("defaultText").stringValue;
        Vector2 textPosition = serializedObject.FindProperty("textPosition").vector2Value;
        Vector2 textSize = serializedObject.FindProperty("textSize").vector2Value;
        float fontSize = serializedObject.FindProperty("fontSize").floatValue;
        TextAlignmentOptions textAlignment = (TextAlignmentOptions)serializedObject.FindProperty("textAlignment").enumValueIndex;
        Color textColor = serializedObject.FindProperty("textColor").colorValue;
        TMP_FontAsset font = serializedObject.FindProperty("font").objectReferenceValue as TMP_FontAsset;

        // Create Canvas
        previewCanvas = new GameObject("PreviewCanvas_Text");
        previewCanvas.hideFlags = HideFlags.DontSave;

        Canvas canvas = previewCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // High sort order so it's visible

        CanvasScaler scaler = previewCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        previewCanvas.AddComponent<GraphicRaycaster>();

        // Create TextMeshProUGUI
        GameObject textObj = new GameObject("PreviewText");
        textObj.hideFlags = HideFlags.DontSave;
        textObj.transform.SetParent(previewCanvas.transform);

        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = textPosition;
        rect.sizeDelta = textSize;

        previewText = textObj.AddComponent<TextMeshProUGUI>();
        previewText.text = defaultText;
        previewText.fontSize = fontSize;
        previewText.alignment = textAlignment;
        previewText.color = new Color(textColor.r, textColor.g, textColor.b, 0.8f); // Slightly transparent for preview

        if (font != null)
        {
            previewText.font = font;
        }

        // Force scene view to repaint
        SceneView.RepaintAll();
    }

    private void UpdatePreview()
    {
        if (previewCanvas == null || previewText == null)
        {
            return;
        }

        // Update text properties
        string defaultText = serializedObject.FindProperty("defaultText").stringValue;
        Vector2 textPosition = serializedObject.FindProperty("textPosition").vector2Value;
        Vector2 textSize = serializedObject.FindProperty("textSize").vector2Value;
        float fontSize = serializedObject.FindProperty("fontSize").floatValue;
        TextAlignmentOptions textAlignment = (TextAlignmentOptions)serializedObject.FindProperty("textAlignment").enumValueIndex;
        Color textColor = serializedObject.FindProperty("textColor").colorValue;
        TMP_FontAsset font = serializedObject.FindProperty("font").objectReferenceValue as TMP_FontAsset;

        RectTransform rect = previewText.GetComponent<RectTransform>();
        rect.anchoredPosition = textPosition;
        rect.sizeDelta = textSize;

        previewText.text = defaultText;
        previewText.fontSize = fontSize;
        previewText.alignment = textAlignment;
        previewText.color = new Color(textColor.r, textColor.g, textColor.b, 0.8f);

        if (font != null)
        {
            previewText.font = font;
        }

        SceneView.RepaintAll();
    }

    private void HidePreview()
    {
        if (previewCanvas != null)
        {
            DestroyImmediate(previewCanvas);
            previewCanvas = null;
            previewText = null;
            SceneView.RepaintAll();
        }
    }
}
