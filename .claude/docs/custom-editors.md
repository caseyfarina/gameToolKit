# Custom Editor Scripts Guide

**CRITICAL: Some MonoBehaviour scripts have custom Unity Editor scripts that control what appears in the Inspector.**

When adding new `[SerializeField]` properties to scripts with custom editors, you **MUST update both files**:
1. The main MonoBehaviour script (adds the field)
2. The corresponding Editor script (displays the field in Inspector)

---

## Scripts with Custom Editors

**Location**: `Assets/eventGameToolKit/Editor/`

| MonoBehaviour Script | Custom Editor Script | Purpose |
|---------------------|---------------------|---------|
| **ActionDialogueSequence.cs** | `ActionEditors/ActionDialogueSequenceEditor.cs` | Context-aware animation settings, preview system with line slider |
| **ActionPlaySound.cs** | `ActionEditors/ActionPlaySoundEditor.cs` | Min/max range fields for volume and pitch, contextual hints, Play-mode test button |
| **ActionDecalSequence.cs** | `ActionEditors/ActionDecalSequenceEditor.cs` | Live playback controls in play mode, duration calculator |
| **ActionDecalSequenceLibrary.cs** | `ActionEditors/ActionDecalSequenceLibraryEditor.cs` | Sequence switcher buttons, live status display |
| **ActionDisplayImage.cs** | `ActionEditors/ActionDisplayImageEditor.cs` | Custom layout and organization |
| **ActionDisplayText.cs** | `ActionEditors/ActionDisplayTextEditor.cs` | Custom layout and organization |
| **ActionPlatformAnimator.cs** | `ActionEditors/ActionPlatformAnimatorEditor.cs` | Animation controls and settings |
| **ActionRandomEvent.cs** | `ActionEditors/ActionRandomEventEditor.cs` | Normalized probability percentages per entry, Play-mode Trigger button |
| **ActionShuffleEvent.cs** | `ActionEditors/ActionShuffleEventEditor.cs` | Per-entry queue status (fired/next/queued), cycle progress bar, Trigger/Reshuffle buttons |
| **PhysicsPlatformAnimator.cs** | `PhysicsEditors/PhysicsPlatformAnimatorEditor.cs` | Physics-based animation controls |
| **PhysicsForceZone.cs** | `PhysicsEditors/PhysicsForceZoneEditor.cs` | Force range inline min/max, one-force-per-stay help text, play-mode zone readout with per-object forced status |
| **PuzzleSwitch.cs** | `PuzzleEditors/PuzzleSwitchEditor.cs` | Puzzle-specific UI layout |
| **PuzzleSwitchChecker.cs** | `PuzzleEditors/PuzzleSwitchCheckerEditor.cs` | Checker-specific UI layout |
| **PuzzleSequenceChecker.cs** | `PuzzleEditors/PuzzleSequenceCheckerEditor.cs` | Numbered sequence list, current-step highlight in play mode, progress bar |
| **InputMouseInteraction.cs** | `InputEditors/InputMouseInteractionEditor.cs` | Conditional scale/hover fields |
| **InputOnStart.cs** | `InputEditors/InputOnStartEditor.cs` | Awake vs Start explanatory help boxes, conditional delay note |
| **InputFPMouseInteraction.cs** | `InputEditors/InputFPMouseInteractionEditor.cs` | Conditional scale/hover fields, raycast settings |
| **GameCollectionManager.cs** | `GameEditors/GameCollectionManagerEditor.cs` | Conditional UI text/bar fields, canvas preview system |
| **GameHealthManager.cs** | `GameEditors/GameHealthManagerEditor.cs` | Conditional UI text/bar fields, canvas preview system |
| **GameTimerManager.cs** | `GameEditors/GameTimerManagerEditor.cs` | Conditional clock/bar fields, totalTime shown only for count-up, canvas preview |
| **GameInventoryManager.cs** | `GameEditors/GameInventoryManagerEditor.cs` | Conditional card UI fields (layout, count text), canvas preview system |
| **InputClickDrag.cs** | `InputEditors/InputClickDragEditor.cs` | Hides snapSize when snapping off, hides limit vectors when limits off |
| **InputClickRotate.cs** | `InputEditors/InputClickRotateEditor.cs` | Hides snapAngle when snapping off, hides limit fields when limits off, play-mode angle readout |
| **ActionRandomMotion.cs** | `AnimationEditors/ActionRandomMotionEditor.cs` | Axis toggle + range inline per row, play-mode Play/Pause/Stop/Return controls |

**Total**: 24 scripts with custom editors

---

## How to Update Scripts with Custom Editors

### Step-by-Step Workflow

**Example: Adding `textColor` field to ActionDialogueSequence**

#### 1. Add the Field to the MonoBehaviour

**File**: `Runtime/Actions/Display/ActionDialogueSequence.cs`

```csharp
[Header("Visual Settings")]
[SerializeField] private float fontSize = 48f;

[Tooltip("Text color for dialogue text")]
[SerializeField] private Color textColor = Color.white;  // ← NEW FIELD

[SerializeField] private TMP_FontAsset customFont;
```

#### 2. Add the Field to the Custom Editor

**File**: `Editor/ActionEditors/ActionDialogueSequenceEditor.cs`

Find the section where related fields are displayed:

```csharp
// Dialogue Text
EditorGUILayout.LabelField("Dialogue Text", EditorStyles.miniBoldLabel);
EditorGUI.indentLevel++;
EditorGUILayout.PropertyField(serializedObject.FindProperty("textPosition"), new GUIContent("Text Position"));
EditorGUILayout.PropertyField(serializedObject.FindProperty("textSize"), new GUIContent("Text Box Size"));
EditorGUILayout.PropertyField(serializedObject.FindProperty("fontSize"), new GUIContent("Font Size"));
EditorGUILayout.PropertyField(serializedObject.FindProperty("textColor"), new GUIContent("Text Color"));  // ← NEW LINE
EditorGUILayout.PropertyField(serializedObject.FindProperty("customFont"), new GUIContent("Custom Font"));
EditorGUI.indentLevel--;
```

**Key Points**:
- Use `serializedObject.FindProperty("fieldName")` to get the property
- Use `EditorGUILayout.PropertyField()` to display it
- Use `new GUIContent("Label")` for student-friendly labels
- **Location matters**: Place in the correct section to match Inspector layout

#### 3. Reimport the Editor Script

After editing the editor script:

1. Right-click the editor script in Unity Project window
2. Select **"Reimport"**
3. This forces Unity to recompile the editor

Alternatively, you can trigger a recompile by:
- Switching to Unity (auto-detects file changes)
- Going to Assets > Refresh
- Restarting Unity Editor

#### 4. Test in Unity

1. Select a GameObject with the component
2. Verify the new field appears in Inspector
3. Test that the field works as expected
4. Check that serialization persists (save/load scene)

---

## Warning Signs You Need to Update an Editor Script

If any of these apply, the script likely has a custom editor:

- ✅ You added `[SerializeField]` fields but they **don't appear in Inspector**
- ✅ The Inspector has **custom section headers** (like "Background Image", "Dialogue Text") instead of Unity's default `[Header]` attributes
- ✅ The Inspector shows/hides fields based on other settings (context-aware UI)
- ✅ The script has **live preview**, **playback controls**, or **interactive buttons** in the Inspector

---

## Finding the Editor Script

### Quick Search Pattern

If a script is named `MyComponent.cs`, search for `MyComponentEditor.cs` in:
```
Assets/eventGameToolKit/Editor/
```

### Unity Project Search

Use Unity's search bar with the type filter:
```
t:script MyComponentEditor
```

### By Folder

Editor scripts are organized by category:

```
Editor/
├── ActionEditors/           # Action component editors
├── GameEditors/             # Game manager editors
├── InputEditors/            # Input component editors
├── PhysicsEditors/          # Physics component editors
└── PuzzleEditors/           # Puzzle component editors
```

---

## Custom Editor Features

### Context-Aware UI (ActionDialogueSequenceEditor)

Shows/hides animation settings based on selected animation type:

```csharp
ActionDialogueSequence.ImageAnimation imageAnim = (ActionDialogueSequence.ImageAnimation)imageAnimProp.enumValueIndex;

if (imageAnim == ActionDialogueSequence.ImageAnimation.SlideUpFromBottom)
{
    EditorGUILayout.PropertyField(serializedObject.FindProperty("slideDistance"), new GUIContent("Slide Distance"));
    // Only show slide distance when slide animation is selected
}
```

**Benefit**: Students only see relevant fields, reducing confusion.

### Live Playback Controls (ActionDecalSequenceEditor)

Adds Play/Pause/Stop buttons in play mode:

```csharp
if (Application.isPlaying)
{
    if (GUILayout.Button("Play"))
    {
        sequence.Play();
    }
}
```

**Benefit**: Students can test animations directly from Inspector.

### Preview System (ActionDialogueSequenceEditor)

Shows dialogue preview with line slider in edit mode:

```csharp
int newPreviewIndex = EditorGUILayout.IntSlider(previewLineIndex, 0, maxPreviewIndex);
if (newPreviewIndex != previewLineIndex)
{
    previewLineIndex = newPreviewIndex;
    UpdatePreview();
}
```

**Benefit**: Students can see dialogue layout before playing.

---

## Best Practices

### 1. Always Check for Editor Scripts First

Before adding new fields to any component:
1. Check the table above
2. Search for `[ComponentName]Editor.cs`
3. If found, plan to update both files

### 2. Match Field Order

Keep the same field order in both files:

**MonoBehaviour**:
```csharp
[SerializeField] private float fontSize = 48f;
[SerializeField] private Color textColor = Color.white;
[SerializeField] private TMP_FontAsset customFont;
```

**Editor**:
```csharp
EditorGUILayout.PropertyField(serializedObject.FindProperty("fontSize"), ...);
EditorGUILayout.PropertyField(serializedObject.FindProperty("textColor"), ...);
EditorGUILayout.PropertyField(serializedObject.FindProperty("customFont"), ...);
```

### 3. Use Meaningful Labels

Use student-friendly labels in `GUIContent`:

```csharp
// Good
new GUIContent("Text Color")

// Bad
new GUIContent("textColor")
```

### 4. Group Related Fields

Use mini headers and indentation:

```csharp
EditorGUILayout.LabelField("Text Formatting", EditorStyles.miniBoldLabel);
EditorGUI.indentLevel++;
EditorGUILayout.PropertyField(...);
EditorGUILayout.PropertyField(...);
EditorGUI.indentLevel--;
```

### 5. Add Tooltips

Help students understand fields:

```csharp
// In MonoBehaviour
[Tooltip("Color of the dialogue text (default: white)")]
[SerializeField] private Color textColor = Color.white;
```

Tooltips automatically appear in custom editors when using `PropertyField`.

---

## Common Issues & Solutions

### Issue: Fields Don't Appear After Adding

**Cause**: Editor script not updated or not reimported

**Solution**:
1. Add the field to the editor script
2. Right-click editor script → Reimport
3. Refresh Unity or restart editor

### Issue: Field Order is Wrong

**Cause**: PropertyField calls are in wrong order in editor script

**Solution**: Reorder the `EditorGUILayout.PropertyField()` calls to match desired layout

### Issue: Field Shows Wrong Label

**Cause**: Incorrect `GUIContent` label

**Solution**: Update the label in the editor script:
```csharp
new GUIContent("Correct Label")
```

### Issue: Custom Editor Breaks After Update

**Cause**: Removed a field that editor script references

**Solution**: Remove the corresponding `PropertyField` call from editor script

---

## Example: Full Workflow

Let's add `textAlignment` and `textColor` to ActionDialogueSequence:

### 1. Update MonoBehaviour

**File**: `Runtime/Actions/Display/ActionDialogueSequence.cs`

```csharp
[Header("Visual Settings")]
[SerializeField] private Vector2 textSize = new Vector2(1200f, 200f);
[SerializeField] private float fontSize = 48f;

// NEW FIELDS
[Tooltip("Text alignment for dialogue text")]
[SerializeField] private TextAlignmentOptions textAlignment = TextAlignmentOptions.Left;

[Tooltip("Text color for dialogue text")]
[SerializeField] private Color textColor = Color.white;

[Tooltip("Optional custom font")]
[SerializeField] private TMP_FontAsset customFont;
```

### 2. Update Custom Editor

**File**: `Editor/ActionEditors/ActionDialogueSequenceEditor.cs`

```csharp
// Dialogue Text section (around line 152)
EditorGUILayout.LabelField("Dialogue Text", EditorStyles.miniBoldLabel);
EditorGUI.indentLevel++;
EditorGUILayout.PropertyField(serializedObject.FindProperty("textPosition"), new GUIContent("Text Position"));
EditorGUILayout.PropertyField(serializedObject.FindProperty("textSize"), new GUIContent("Text Box Size"));
EditorGUILayout.PropertyField(serializedObject.FindProperty("fontSize"), new GUIContent("Font Size"));

// NEW LINES
EditorGUILayout.PropertyField(serializedObject.FindProperty("textAlignment"), new GUIContent("Text Alignment"));
EditorGUILayout.PropertyField(serializedObject.FindProperty("textColor"), new GUIContent("Text Color"));

EditorGUILayout.PropertyField(serializedObject.FindProperty("customFont"), new GUIContent("Custom Font"));
EditorGUI.indentLevel--;
```

### 3. Reimport and Test

1. Save both files
2. Switch to Unity (auto-compiles)
3. If fields don't appear: Right-click `ActionDialogueSequenceEditor.cs` → Reimport
4. Select GameObject with ActionDialogueSequence
5. Verify new fields appear under "Dialogue Text" section
6. Test that values save/load correctly

---

## Summary

✅ **Always update both files** when adding fields to scripts with custom editors
✅ **Check the table** before modifying any component
✅ **Match field order** between MonoBehaviour and Editor
✅ **Use meaningful labels** for student clarity
✅ **Reimport after changes** to force Unity recompile
✅ **Test in Unity** to verify fields work correctly

Custom editors make the toolkit more student-friendly, but require careful maintenance!
