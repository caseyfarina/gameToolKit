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
        EditorGUILayout.PropertyField(serializedObject.FindProperty("enableClickThrough"));

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
        EditorGUILayout.PropertyField(serializedObject.FindProperty("textAlignment"), new GUIContent("Text Alignment"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("textColor"), new GUIContent("Text Color"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("customFont"), new GUIContent("Custom Font"));
        EditorGUI.indentLevel--;

        // Decision System header
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Decision System (Optional)", EditorStyles.boldLabel);

        SerializedProperty enableDecisionProp = serializedObject.FindProperty("enableDecision");
        EditorGUILayout.PropertyField(enableDecisionProp);

        // Show decision settings if enabled
        if (enableDecisionProp.boolValue)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("decisionChoices"), new GUIContent("Decision Choices"));

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Decision Panel Settings", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("decisionPanelPosition"), new GUIContent("Panel Position"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("decisionButtonSize"), new GUIContent("Button Size"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("decisionButtonSpacing"), new GUIContent("Button Spacing"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("decisionImageSize"), new GUIContent("Choice Image Size"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("decisionFontSize"), new GUIContent("Font Size"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("decisionButtonOpacity"), new GUIContent("Button Opacity"));
            EditorGUI.indentLevel--;

            EditorGUI.indentLevel--;
        }

        // Editor Preview Controls
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Editor Preview", EditorStyles.boldLabel);

        int lineCount = serializedObject.FindProperty("dialogueLines").arraySize;
        bool hasDecisions = enableDecisionProp.boolValue && serializedObject.FindProperty("decisionChoices").arraySize > 0;

        // Calculate max preview index (lines + optional decision)
        int maxPreviewIndex = lineCount > 0 ? lineCount - 1 : 0;
        if (hasDecisions && lineCount > 0)
        {
            maxPreviewIndex = lineCount; // One extra for decision preview
        }

        if (lineCount == 0)
        {
            EditorGUILayout.HelpBox("Add dialogue lines to enable preview.", MessageType.Info);
        }
        else
        {
            // Preview slider
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Preview Line", GUILayout.Width(100));

            int newPreviewIndex = EditorGUILayout.IntSlider(previewLineIndex, 0, maxPreviewIndex);

            // Show label for what's being previewed
            if (hasDecisions && newPreviewIndex == lineCount)
            {
                EditorGUILayout.LabelField("(Decision)", GUILayout.Width(80));
            }
            else if (lineCount > 0)
            {
                EditorGUILayout.LabelField($"(Line {newPreviewIndex + 1})", GUILayout.Width(80));
            }

            EditorGUILayout.EndHorizontal();

            if (newPreviewIndex != previewLineIndex)
            {
                previewLineIndex = newPreviewIndex;
                if (showPreview)
                {
                    UpdatePreview();
                }
            }

            // Show help text if decisions are enabled
            if (hasDecisions)
            {
                EditorGUILayout.HelpBox("Slide preview to the right to see the decision panel.", MessageType.Info);
            }
        }

        // Show/Hide Preview button
        EditorGUILayout.BeginHorizontal();

        GUI.enabled = lineCount > 0;

        if (!showPreview)
        {
            if (GUILayout.Button("Show Canvas Preview", GUILayout.Height(30)))
            {
                showPreview = true;
                ShowPreview();
            }
        }
        else
        {
            if (GUILayout.Button("Hide Canvas Preview", GUILayout.Height(30)))
            {
                showPreview = false;
                HidePreview();
            }
        }

        EditorGUILayout.EndHorizontal();

        GUI.enabled = true;

        if (showPreview)
        {
            EditorGUILayout.HelpBox("Preview is visible in the Scene view. Adjust Visual Settings to see changes in real-time.", MessageType.Info);
        }

        // Dialogue Events header
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Dialogue Events", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onDialogueStart"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onDialogueComplete"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onLineChanged"));

        // Decision event (only show if decision system is enabled)
        if (enableDecisionProp.boolValue)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onDecisionStart"));
        }

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
