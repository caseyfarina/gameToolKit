using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor for GameStoreManager with conditional UI settings and an editor preview.
/// </summary>
[CustomEditor(typeof(GameStoreManager))]
public class GameStoreManagerEditor : Editor
{
    private bool showPreview = false;

    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        HidePreview();
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
            HidePreview();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Script field
        GUI.enabled = false;
        EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((GameStoreManager)target), typeof(GameStoreManager), false);
        GUI.enabled = true;

        // ── Currency Source ─────────────────────────────────────────────────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Currency Source", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("currencySource"));

        // ── Store Items ─────────────────────────────────────────────────────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Store Items", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("items"), true);

        // ── Open Mode ───────────────────────────────────────────────────────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Open Mode", EditorStyles.boldLabel);
        SerializedProperty openModeProp = serializedObject.FindProperty("openMode");
        EditorGUILayout.PropertyField(openModeProp, new GUIContent("Open Mode"));

        if (openModeProp.enumValueIndex == (int)StoreOpenMode.Key)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("storeKey"), new GUIContent("Open / Close Key"));
            EditorGUI.indentLevel--;
        }
        else
        {
            EditorGUILayout.HelpBox("Call OpenStore() or ToggleStore() from a UnityEvent — for example, wire an InputTriggerZone's On Enter event to open the store when the player walks up to a shopkeeper.", MessageType.None);
        }

        // ── Character Controller (Optional) ─────────────────────────────────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Character Controller (Optional)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("fpController"), new GUIContent("FP Controller"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("ccController"), new GUIContent("CC Controller"));

        // ── Purchase Limit ───────────────────────────────────────────────────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Purchase Limit", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("limitOnePurchasePerOpen"), new GUIContent("One Per Visit"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onRestartBehavior"), new GUIContent("On Restart"));

        // ── Audio (Optional) ────────────────────────────────────────────────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Audio (Optional)", EditorStyles.boldLabel);

        SerializedProperty audioManagerProp = serializedObject.FindProperty("audioManager");
        EditorGUILayout.PropertyField(audioManagerProp, new GUIContent("Audio Manager"));

        if (audioManagerProp.objectReferenceValue != null)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("storeMusic"),    new GUIContent("Store Music"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("previousMusic"), new GUIContent("Previous Music"));
            EditorGUI.indentLevel--;
        }
        else
        {
            EditorGUILayout.HelpBox("Assign an Audio Manager to enable music switching when the store opens and closes.", MessageType.None);
        }

        // ── Sound Effects (Optional) ─────────────────────────────────────────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Sound Effects (Optional)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("audioSource"),      new GUIContent("Audio Source"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("purchaseSound"),    new GUIContent("Purchase Sound"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("cannotAffordSound"),new GUIContent("Can't Afford Sound"));

        // ── Store UI ────────────────────────────────────────────────────────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Store UI", EditorStyles.boldLabel);

        SerializedProperty showUIProp = serializedObject.FindProperty("showUI");
        EditorGUILayout.PropertyField(showUIProp, new GUIContent("Show UI"));

        if (showUIProp.boolValue)
        {
            EditorGUI.indentLevel++;

            // Panel
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Panel", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("storeTitle"),            new GUIContent("Title"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("panelPosition"),          new GUIContent("Position"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("panelSize"),              new GUIContent("Size"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("panelBackgroundSprite"),  new GUIContent("Background Sprite"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("panelBackgroundColor"),   new GUIContent("Background Color"));
            EditorGUI.indentLevel--;

            // Rows
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Item Rows", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rowHeight"),             new GUIContent("Row Height"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rowSpacing"),            new GUIContent("Row Spacing"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rowPadding"),            new GUIContent("Row Padding"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rowBackgroundSprite"),   new GUIContent("Row Sprite"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rowBackgroundColor"),    new GUIContent("Row Color"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rowDisabledColor"),      new GUIContent("Row Disabled Color"));
            EditorGUI.indentLevel--;

            // Icons
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Icons", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("iconSize"), new GUIContent("Icon Size"));
            EditorGUI.indentLevel--;

            // Buy Button
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Buy Button", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("buttonSprite"),        new GUIContent("Button Sprite"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("buttonColor"),         new GUIContent("Button Color"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("buttonDisabledColor"), new GUIContent("Disabled Color"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("buttonSize"),          new GUIContent("Button Size"));
            EditorGUI.indentLevel--;

            // Typography
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Typography", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fontSize"),       new GUIContent("Name Font Size"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("priceFontSize"),  new GUIContent("Price Font Size"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("textColor"),      new GUIContent("Text Color"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("priceTextColor"), new GUIContent("Price Color"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("customFont"),     new GUIContent("Custom Font"));
            EditorGUI.indentLevel--;

            // Balance Display
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Balance Display", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("currencyLabel"),  new GUIContent("Label"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("currencySymbol"), new GUIContent("Symbol"));
            EditorGUI.indentLevel--;

            EditorGUI.indentLevel--;

            // Preview
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Editor Preview", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

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

            if (showPreview)
                EditorGUILayout.HelpBox("Preview is visible in the Game view. Adjust settings to see changes.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("Enable Show UI to create a self-contained store panel at runtime.", MessageType.None);

            if (showPreview)
            {
                showPreview = false;
                HidePreview();
            }
        }

        // ── Events ──────────────────────────────────────────────────────────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onStoreOpened"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onStoreClosed"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onAnyPurchase"));

        bool changed = serializedObject.ApplyModifiedProperties();

        if (showPreview && changed)
            UpdatePreview();
    }

    private void ShowPreview()
    {
        GameStoreManager manager = (GameStoreManager)target;
        manager.CreatePreviewUI();
        EditorUtility.SetDirty(manager);
    }

    private void HidePreview()
    {
        if (target == null) return;
        GameStoreManager manager = (GameStoreManager)target;
        manager.DestroyPreviewUI();
        EditorUtility.SetDirty(manager);
    }

    private void UpdatePreview()
    {
        GameStoreManager manager = (GameStoreManager)target;
        manager.UpdatePreviewUI();
        EditorUtility.SetDirty(manager);
    }
}
