using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor for ActionDialogueSequence that shows/hides animation settings based on selected animation type
/// </summary>
[CustomEditor(typeof(ActionDialogueSequence))]
public class ActionDialogueSequenceEditor : Editor
{
    private bool showPreview = false;
    private int previewLineIndex = 0;

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
        EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((ActionDialogueSequence)target), typeof(ActionDialogueSequence), false);
        GUI.enabled = true;

        // Dialogue Content header
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Dialogue Content", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("dialogueLines"));

        // Playback Settings header
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Playback Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("playOnStart"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("loop"));

        // Animation Settings header
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Animation Settings", EditorStyles.boldLabel);

        // Image Animation
        SerializedProperty imageAnimProp = serializedObject.FindProperty("imageAnimation");
        EditorGUILayout.PropertyField(imageAnimProp);

        ActionDialogueSequence.ImageAnimation imageAnim = (ActionDialogueSequence.ImageAnimation)imageAnimProp.enumValueIndex;

        // Show image animation settings based on selection
        if (imageAnim != ActionDialogueSequence.ImageAnimation.None)
        {
            EditorGUI.indentLevel++;

            // Context-aware label for "in" animation
            if (imageAnim == ActionDialogueSequence.ImageAnimation.SlideUpFromBottom ||
                imageAnim == ActionDialogueSequence.ImageAnimation.SlideInFromSide)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("imageFadeInDuration"), new GUIContent("Slide Duration"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("slideDistance"), new GUIContent("Slide Distance"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("imageSlideEasing"), new GUIContent("Slide Easing"));
            }
            else if (imageAnim == ActionDialogueSequence.ImageAnimation.PopIn)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("imageFadeInDuration"), new GUIContent("Pop Duration"));
            }
            else if (imageAnim == ActionDialogueSequence.ImageAnimation.FadeIn)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("imageFadeInDuration"), new GUIContent("Fade In Duration"));
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("imageFadeOutDuration"), new GUIContent("Fade Out Duration"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(5);

        // Text Animation
        SerializedProperty textAnimProp = serializedObject.FindProperty("textAnimation");
        EditorGUILayout.PropertyField(textAnimProp);

        ActionDialogueSequence.TextAnimation textAnim = (ActionDialogueSequence.TextAnimation)textAnimProp.enumValueIndex;

        // Show text animation settings based on selection
        if (textAnim != ActionDialogueSequence.TextAnimation.None)
        {
            EditorGUI.indentLevel++;

            if (textAnim == ActionDialogueSequence.TextAnimation.TypeOn)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("charactersPerSecond"), new GUIContent("Characters Per Second"));
            }
            else if (textAnim == ActionDialogueSequence.TextAnimation.FadeIn)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("textFadeInDuration"), new GUIContent("Fade In Duration"));
            }
            else if (textAnim == ActionDialogueSequence.TextAnimation.SlideUpFromBottom)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("imageFadeInDuration"), new GUIContent("Slide Duration"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("textSlideDistance"), new GUIContent("Slide Distance"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("textSlideEasing"), new GUIContent("Slide Easing"));
            }

            // Fade out is common for all text animations
            EditorGUILayout.PropertyField(serializedObject.FindProperty("textFadeOutDuration"), new GUIContent("Fade Out Duration"));

            EditorGUI.indentLevel--;
        }

        // Visual Settings header
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Visual Settings", EditorStyles.boldLabel);

        // Preview Controls
        ActionDialogueSequence dialogue = (ActionDialogueSequence)target;
        int lineCount = serializedObject.FindProperty("dialogueLines").arraySize;

        if (lineCount > 0)
        {
            // Preview line selector
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Preview Line:", GUILayout.Width(80));

            // Clamp preview index to valid range
            previewLineIndex = Mathf.Clamp(previewLineIndex, 0, lineCount - 1);

            int newPreviewIndex = EditorGUILayout.IntSlider(previewLineIndex, 0, lineCount - 1);

            // If preview is showing and index changed, update preview
            if (newPreviewIndex != previewLineIndex)
            {
                previewLineIndex = newPreviewIndex;
                if (showPreview)
                {
                    UpdatePreview();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        // Preview Button
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUI.enabled = lineCount > 0; // Disable if no lines
        string buttonText = showPreview ? "Hide Canvas Preview" : "Show Canvas Preview";
        Color originalColor = GUI.backgroundColor;
        GUI.backgroundColor = showPreview ? new Color(1f, 0.7f, 0.7f) : new Color(0.7f, 1f, 0.7f);

        if (GUILayout.Button(buttonText, GUILayout.Height(30), GUILayout.Width(200)))
        {
            showPreview = !showPreview;
            if (showPreview)
            {
                ShowPreview();
            }
            else
            {
                HidePreview();
            }
        }

        GUI.backgroundColor = originalColor;
        GUI.enabled = true;
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        if (lineCount == 0)
        {
            EditorGUILayout.HelpBox("Add dialogue lines to enable preview", MessageType.Info);
        }

        EditorGUILayout.Space(5);

        // Background Image
        EditorGUILayout.LabelField("Background Image", EditorStyles.miniBoldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(serializedObject.FindProperty("backgroundImage"), new GUIContent("Image"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("backgroundPosition"), new GUIContent("Background Position"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("backgroundSize"), new GUIContent("Background Size"));
        EditorGUI.indentLevel--;

        EditorGUILayout.Space(5);

        // Character Images
        EditorGUILayout.LabelField("Character Images", EditorStyles.miniBoldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(serializedObject.FindProperty("leftPosition"), new GUIContent("Left Image Position"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("rightPosition"), new GUIContent("Right Image Position"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("portraitSize"), new GUIContent("Image Size"));
        EditorGUI.indentLevel--;

        EditorGUILayout.Space(5);

        // Dialogue Text
        EditorGUILayout.LabelField("Dialogue Text", EditorStyles.miniBoldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(serializedObject.FindProperty("textPosition"), new GUIContent("Text Position"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("textSize"), new GUIContent("Text Box Size"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("fontSize"), new GUIContent("Font Size"));
        EditorGUI.indentLevel--;

        // Dialogue Events header
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Dialogue Events", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onDialogueStart"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onDialogueComplete"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onLineChanged"));

        // Check if any properties changed
        bool changed = serializedObject.ApplyModifiedProperties();

        // Update preview if showing and values changed
        if (showPreview && changed)
        {
            UpdatePreview();
        }
    }

    private void ShowPreview()
    {
        ActionDialogueSequence dialogue = (ActionDialogueSequence)target;
        dialogue.CreatePreviewCanvas(previewLineIndex);
        EditorUtility.SetDirty(dialogue);
    }

    private void HidePreview()
    {
        if (target == null) return;

        ActionDialogueSequence dialogue = (ActionDialogueSequence)target;
        dialogue.DestroyPreviewCanvas();
        showPreview = false;
        EditorUtility.SetDirty(dialogue);
    }

    private void UpdatePreview()
    {
        ActionDialogueSequence dialogue = (ActionDialogueSequence)target;
        dialogue.UpdatePreviewCanvas(previewLineIndex);
        EditorUtility.SetDirty(dialogue);
    }
}
