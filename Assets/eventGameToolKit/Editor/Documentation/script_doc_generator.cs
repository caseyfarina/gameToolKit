using UnityEngine;
using UnityEditor;
using UnityEngine.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine.UI;

public class ScriptDocumentationGenerator : EditorWindow
{
    private string rootFolderPath = "Assets/Scripts";
    private GameObject documentationRoot;
    private float columnSpacing = 400f;
    private float minFontSize = 8f;
    private float maxFontSize = 24f;
    private TMP_FontAsset customFont = null;
    
    [MenuItem("Tools/Script Documentation Generator")]
    public static void ShowWindow()
    {
        GetWindow<ScriptDocumentationGenerator>("Script Documentation");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Script Documentation Generator", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        rootFolderPath = EditorGUILayout.TextField("Root Folder Path", rootFolderPath);
        
        EditorGUILayout.Space();
        GUILayout.Label("Layout Settings", EditorStyles.boldLabel);
        columnSpacing = EditorGUILayout.FloatField("Column Spacing", columnSpacing);

        EditorGUILayout.HelpBox("Script card heights and spacing are calculated automatically based on content.", MessageType.Info);
        
        EditorGUILayout.Space();
        GUILayout.Label("Font Settings", EditorStyles.boldLabel);
        minFontSize = EditorGUILayout.FloatField("Min Font Size", minFontSize);
        maxFontSize = EditorGUILayout.FloatField("Max Font Size", maxFontSize);
        
        // Show a helpful note about the proportional sizing
        EditorGUILayout.HelpBox(
            "Min = Function descriptions (0.5x)\n" +
            "Max = Folder labels (10x)\n" +
            "All text scales proportionally between these values.", 
            MessageType.Info);
        
        customFont = (TMP_FontAsset)EditorGUILayout.ObjectField("Custom Font (Optional)", customFont, typeof(TMP_FontAsset), false);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Generate Documentation", GUILayout.Height(30)))
        {
            GenerateDocumentation();
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Clear Documentation", GUILayout.Height(30)))
        {
            ClearDocumentation();
        }
    }
    
    private void GenerateDocumentation()
    {
        ClearDocumentation();
        
        // Create root GameObject
        documentationRoot = new GameObject("ScriptDocumentation");
        
        // Create Canvas
        Canvas canvas = documentationRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        documentationRoot.AddComponent<CanvasScaler>();
        documentationRoot.AddComponent<GraphicRaycaster>();
        
        // Get all subdirectories in root folder
        string[] subdirectories = AssetDatabase.GetSubFolders(rootFolderPath);
        
        if (subdirectories.Length == 0)
        {
            Debug.LogWarning($"No subfolders found in {rootFolderPath}. Processing root folder as single column.");
            ProcessFolder(rootFolderPath, 0, 1, canvas.transform);
        }
        else
        {
            for (int i = 0; i < subdirectories.Length; i++)
            {
                ProcessFolder(subdirectories[i], i, subdirectories.Length, canvas.transform);
            }
        }
        
        Debug.Log($"Documentation generated with {subdirectories.Length} columns.");
    }
    
    private void ProcessFolder(string folderPath, int columnIndex, int totalColumns, Transform canvasTransform)
    {
        // Calculate color based on evenly distributed hue
        Color columnColor = GetColumnColor(columnIndex, totalColumns);
        
        // Get all C# scripts in this folder
        string[] guids = AssetDatabase.FindAssets("t:MonoScript", new[] { folderPath });
        List<ScriptInfo> scriptInfos = new List<ScriptInfo>();
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            
            if (script != null)
            {
                Type type = script.GetClass();
                if (type != null && type.IsSubclassOf(typeof(MonoBehaviour)))
                {
                    string description = ExtractSummaryFromFile(path);
                    Dictionary<string, string> methodDescriptions = ExtractMethodDescriptions(path, type);
                    Dictionary<string, string> eventDescriptions = ExtractEventDescriptions(path, type);
                    scriptInfos.Add(new ScriptInfo
                    {
                        Type = type,
                        Description = description,
                        MethodDescriptions = methodDescriptions,
                        EventDescriptions = eventDescriptions,
                        FilePath = path
                    });
                }
            }
        }
        
        // Create column container
        GameObject columnObj = new GameObject(System.IO.Path.GetFileName(folderPath));
        RectTransform columnRect = columnObj.AddComponent<RectTransform>();
        columnRect.SetParent(canvasTransform, false);
        columnRect.anchorMin = new Vector2(0, 1);
        columnRect.anchorMax = new Vector2(0, 1);
        columnRect.pivot = new Vector2(0, 1);
        columnRect.anchoredPosition = new Vector2(columnIndex * columnSpacing + 50, -50);
        
        // Create folder label at the top of the column
        CreateFolderLabel(columnRect, System.IO.Path.GetFileName(folderPath), columnColor);

        // Create scripts in this column with cumulative spacing
        float currentYOffset = 0;
        for (int i = 0; i < scriptInfos.Count; i++)
        {
            float scriptHeight = CreateScriptVisualization(scriptInfos[i], currentYOffset, columnRect, columnColor);
            currentYOffset += scriptHeight + 20; // Add script height plus 20px gap between cards
        }
    }
    
    private Color GetColumnColor(int index, int totalColumns)
    {
        // Evenly distribute hue across the color wheel
        float hue = (float)index / (float)totalColumns;
        
        // Use full saturation and maximum brightness for readability
        return Color.HSVToRGB(hue, 0.8f, 1f);
    }
    
    private float GetFontSize(float multiplier)
    {
        // Map the multiplier to the min-max range
        // multiplier ranges from 0.5x (function descriptions) to 10x (folder labels)
        // We want to map this proportionally to minFontSize - maxFontSize
        float normalizedMultiplier = (multiplier - 0.5f) / (10f - 0.5f); // Normalize to 0-1
        return Mathf.Lerp(minFontSize, maxFontSize, normalizedMultiplier);
    }
    
    private void CreateFolderLabel(RectTransform columnRect, string folderName, Color columnColor)
    {
        float fontSize = GetFontSize(10f); // 10x multiplier for folder labels
        
        GameObject labelObj = new GameObject("FolderLabel");
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.SetParent(columnRect, false);
        labelRect.anchorMin = new Vector2(0, 1);
        labelRect.anchorMax = new Vector2(0, 1);
        labelRect.pivot = new Vector2(0, 1);
        // Position at top of column, not rotated
        labelRect.anchoredPosition = new Vector2(0, 0);
        labelRect.sizeDelta = new Vector2(350, fontSize * 1.5f);
        
        TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = folderName.ToUpper();
        labelText.fontSize = fontSize;
        labelText.fontStyle = FontStyles.Bold;
        labelText.alignment = TextAlignmentOptions.Left;
        labelText.color = columnColor;
        if (customFont != null) labelText.font = customFont;
    }
    
    private float CreateScriptVisualization(ScriptInfo scriptInfo, float yOffset, RectTransform columnRect, Color columnColor)
    {
        Type scriptType = scriptInfo.Type;
        float baseFontSize = GetFontSize(1f); // 1x multiplier for base font
        float folderLabelSize = GetFontSize(10f);
        float folderLabelHeight = folderLabelSize * 1.5f + 20; // Account for folder label height

        // Create desaturated version of column color for background
        Color.RGBToHSV(columnColor, out float h, out float s, out float v);
        Color desaturatedBgColor = Color.HSVToRGB(h, s * 0.2f, 0.95f); // Low saturation, high brightness

        // Use black for all text
        Color textColor = Color.black;

        // Get methods and events to calculate dynamic height
        List<MethodInfo> inputMethods = GetInputMethods(scriptType);
        List<FieldInfo> outputEvents = GetOutputEvents(scriptType);

        // Calculate dynamic height based on actual content
        float titleHeight = GetFontSize(4f) * 1.2f + 5; // Title
        float descriptionHeight = string.IsNullOrEmpty(scriptInfo.Description) ? 0 : 60; // Description

        // Estimate height for each function (name + description + spacing)
        float methodHeight = 0;
        foreach (var method in inputMethods)
        {
            methodHeight += baseFontSize * 1.2f; // Function name line
            if (scriptInfo.MethodDescriptions != null && scriptInfo.MethodDescriptions.ContainsKey(method.Name))
            {
                methodHeight += GetFontSize(0.5f) * 1.5f; // Description line(s)
            }
            methodHeight += baseFontSize * 0.5f; // Spacing between entries
        }

        // Estimate height for each event (name + description + spacing)
        float eventHeight = 0;
        foreach (var evt in outputEvents)
        {
            eventHeight += baseFontSize * 1.2f; // Event name line
            if (scriptInfo.EventDescriptions != null && scriptInfo.EventDescriptions.ContainsKey(evt.Name))
            {
                eventHeight += GetFontSize(0.5f) * 1.5f; // Description line(s)
            }
            eventHeight += baseFontSize * 0.5f; // Spacing between entries
        }

        // Calculate total height: header + title + description + max(methods, events) + padding
        float sectionHeaderHeight = baseFontSize * 1.5f; // "FUNCTIONS:" and "EVENTS:" labels
        float contentHeight = Mathf.Max(methodHeight, eventHeight); // They're side by side, so use max
        float totalHeight = titleHeight + descriptionHeight + sectionHeaderHeight + contentHeight + 30; // 30 = padding

        // Minimum height to prevent tiny cards
        totalHeight = Mathf.Max(totalHeight, 150);

        // Store original height for spacing calculations
        float spacingHeight = totalHeight;

        // Make background 20% taller for more breathing room
        float displayHeight = totalHeight * 1.2f;

        // Create container for this script
        GameObject scriptObj = new GameObject(scriptType.Name);
        RectTransform scriptRect = scriptObj.AddComponent<RectTransform>();
        scriptRect.SetParent(columnRect, false);
        scriptRect.anchorMin = new Vector2(0, 1);
        scriptRect.anchorMax = new Vector2(0, 1);
        scriptRect.pivot = new Vector2(0, 1);
        scriptRect.anchoredPosition = new Vector2(0, -folderLabelHeight - yOffset);

        // Set display height (20% taller)
        scriptRect.sizeDelta = new Vector2(350, displayHeight);

        // Add background with desaturated column color
        Image bg = scriptObj.AddComponent<Image>();
        bg.color = desaturatedBgColor;
        
        // Add colored border/accent
        GameObject borderObj = new GameObject("ColorAccent");
        RectTransform borderRect = borderObj.AddComponent<RectTransform>();
        borderRect.SetParent(scriptRect, false);
        borderRect.anchorMin = new Vector2(0, 0);
        borderRect.anchorMax = new Vector2(0, 1);
        borderRect.pivot = new Vector2(0, 0.5f);
        borderRect.anchoredPosition = Vector2.zero;
        borderRect.sizeDelta = new Vector2(5, 0);
        
        Image borderImage = borderObj.AddComponent<Image>();
        borderImage.color = columnColor;
        
        float currentY = -10;
        
        // Create title (4x larger)
        float titleFontSize = GetFontSize(4f); // 4x multiplier for titles
        GameObject titleObj = new GameObject("Title");
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.SetParent(scriptRect, false);
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, currentY);
        titleRect.sizeDelta = new Vector2(-20, titleFontSize * 1.2f);
        
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = scriptType.Name;
        titleText.fontSize = titleFontSize;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Left;
        titleText.color = textColor;
        if (customFont != null) titleText.font = customFont;

        currentY -= titleFontSize * 1.2f + 5;

        // Create description if available
        if (!string.IsNullOrEmpty(scriptInfo.Description))
        {
            float descFontSize = GetFontSize(0.8f); // 0.8x multiplier for descriptions
            GameObject descObj = new GameObject("Description");
            RectTransform descRect = descObj.AddComponent<RectTransform>();
            descRect.SetParent(scriptRect, false);
            descRect.anchorMin = new Vector2(0, 1);
            descRect.anchorMax = new Vector2(1, 1);
            descRect.pivot = new Vector2(0.5f, 1);
            descRect.anchoredPosition = new Vector2(0, currentY);
            descRect.sizeDelta = new Vector2(-20, 55);

            TextMeshProUGUI descText = descObj.AddComponent<TextMeshProUGUI>();
            descText.text = scriptInfo.Description;
            descText.fontSize = descFontSize;
            descText.fontStyle = FontStyles.Italic;
            descText.alignment = TextAlignmentOptions.TopLeft;
            descText.color = textColor;
            descText.textWrappingMode = TextWrappingModes.Normal;
            if (customFont != null) descText.font = customFont;

            currentY -= 60;
        }

        // inputMethods and outputEvents already declared earlier for height calculation

        // Create functions section
        CreateMethodList(scriptRect, "FUNCTIONS", inputMethods, scriptInfo.MethodDescriptions, true, currentY, textColor);

        // Create events section
        CreateEventList(scriptRect, "EVENTS", outputEvents, scriptInfo.EventDescriptions, false, currentY, textColor);

        // Return the display height (actual background height) so spacing accounts for the full panel size
        return displayHeight;
    }
    
    private void CreateMethodList(RectTransform parent, string label, List<MethodInfo> methods, Dictionary<string, string> methodDescriptions, bool isLeft, float yOffset, Color textColor)
    {
        float fontSize = GetFontSize(1f); // 1x for function names
        float descFontSize = GetFontSize(0.5f); // 0.5x for function descriptions
        
        GameObject section = new GameObject(label);
        RectTransform sectionRect = section.AddComponent<RectTransform>();
        sectionRect.SetParent(parent, false);
        sectionRect.anchorMin = isLeft ? new Vector2(0, 0) : new Vector2(1, 0);
        sectionRect.anchorMax = isLeft ? new Vector2(0, 1) : new Vector2(1, 1);
        sectionRect.pivot = isLeft ? new Vector2(0, 1) : new Vector2(1, 1);
        sectionRect.anchoredPosition = isLeft ? new Vector2(10, yOffset) : new Vector2(-10, yOffset);
        sectionRect.sizeDelta = new Vector2(160, yOffset - 10);
        
        TextMeshProUGUI text = section.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = isLeft ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.TopRight;
        text.color = textColor;
        text.textWrappingMode = TextWrappingModes.Normal;
        if (customFont != null) text.font = customFont;
        
        string content = $"<b>{label}:</b>\n";
        foreach (var method in methods)
        {
            string methodSignature = GetMethodSignature(method);
            content += $"<size={fontSize}>• {methodSignature}</size>\n";
            
            // Add method description if available
            if (methodDescriptions != null && methodDescriptions.ContainsKey(method.Name))
            {
                string description = methodDescriptions[method.Name];
                content += $"<size={descFontSize}><i>{description}</i></size>\n";
            }
            
            // Add spacing between function entries
            content += "\n";
        }
        
        text.text = content;
    }
    
    private void CreateEventList(RectTransform parent, string label, List<FieldInfo> events, Dictionary<string, string> eventDescriptions, bool isLeft, float yOffset, Color textColor)
    {
        float fontSize = GetFontSize(1f); // 1x for event names
        float descFontSize = GetFontSize(0.5f); // 0.5x for event descriptions

        GameObject section = new GameObject(label);
        RectTransform sectionRect = section.AddComponent<RectTransform>();
        sectionRect.SetParent(parent, false);
        sectionRect.anchorMin = isLeft ? new Vector2(0, 0) : new Vector2(1, 0);
        sectionRect.anchorMax = isLeft ? new Vector2(0, 1) : new Vector2(1, 1);
        sectionRect.pivot = isLeft ? new Vector2(0, 1) : new Vector2(1, 1);
        sectionRect.anchoredPosition = isLeft ? new Vector2(10, yOffset) : new Vector2(-10, yOffset);
        sectionRect.sizeDelta = new Vector2(160, yOffset - 10);

        TextMeshProUGUI text = section.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = isLeft ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.TopRight;
        text.color = textColor;
        text.textWrappingMode = TextWrappingModes.Normal;
        if (customFont != null) text.font = customFont;

        string content = $"<b>{label}:</b>\n";
        foreach (var evt in events)
        {
            content += $"<size={fontSize}>• {evt.Name}</size>\n";

            // Add event description if available
            if (eventDescriptions != null && eventDescriptions.ContainsKey(evt.Name))
            {
                string description = eventDescriptions[evt.Name];
                content += $"<size={descFontSize}><i>{description}</i></size>\n";
            }

            // Add spacing between event entries
            content += "\n";
        }

        text.text = content;
    }
    
    private List<MethodInfo> GetInputMethods(Type type)
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName && // Excludes property getters/setters
                       m.DeclaringType == type && // Only methods declared in this type
                       !IsUnityLifecycleMethod(m.Name) &&
                       m.GetParameters().Length <= 4) // UnityEvent supports up to 4 params
            .OrderBy(m => m.Name)
            .ToList();
    }
    
    private List<FieldInfo> GetOutputEvents(Type type)
    {
        return type.GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(f => typeof(UnityEventBase).IsAssignableFrom(f.FieldType))
            .OrderBy(f => f.Name)
            .ToList();
    }
    
    private bool IsUnityLifecycleMethod(string methodName)
    {
        string[] lifecycleMethods = { "Start", "Update", "FixedUpdate", "LateUpdate", 
            "OnEnable", "OnDisable", "OnDestroy", "Awake", "OnApplicationQuit", 
            "OnApplicationPause", "OnApplicationFocus" };
        return lifecycleMethods.Contains(methodName);
    }
    
    private string GetMethodSignature(MethodInfo method)
    {
        var parameters = method.GetParameters();
        if (parameters.Length == 0)
            return $"{method.Name}()";
        
        string paramString = string.Join(", ", parameters.Select(p => $"{GetFriendlyTypeName(p.ParameterType)} {p.Name}"));
        return $"{method.Name}({paramString})";
    }
    
    private string GetFriendlyTypeName(Type type)
    {
        if (type == typeof(int)) return "int";
        if (type == typeof(float)) return "float";
        if (type == typeof(bool)) return "bool";
        if (type == typeof(string)) return "string";
        if (type == typeof(void)) return "void";
        return type.Name;
    }
    
    private string ExtractSummaryFromFile(string filePath)
    {
        try
        {
            string fileContent = File.ReadAllText(filePath);
            
            // Regex to match XML summary tags and capture content
            // Handles multi-line summaries with proper trimming
            string pattern = @"/// <summary>(.*?)/// </summary>";
            Match match = Regex.Match(fileContent, pattern, RegexOptions.Singleline);
            
            if (match.Success)
            {
                string summary = match.Groups[1].Value;
                
                // Clean up the summary text
                summary = Regex.Replace(summary, @"///\s*", ""); // Remove /// and whitespace
                summary = summary.Trim();
                summary = Regex.Replace(summary, @"\s+", " "); // Replace multiple spaces/newlines with single space
                
                return summary;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Could not extract summary from {filePath}: {e.Message}");
        }
        
        return string.Empty;
    }
    
    private Dictionary<string, string> ExtractMethodDescriptions(string filePath, Type type)
    {
        Dictionary<string, string> descriptions = new Dictionary<string, string>();
        
        try
        {
            string[] lines = File.ReadAllLines(filePath);
            var methods = GetInputMethods(type);
            
            foreach (var method in methods)
            {
                // Find the method declaration line
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    
                    // Check if this line contains the method signature
                    if (line.Contains("public") && line.Contains(method.Name) && line.Contains("("))
                    {
                        // Found the method declaration, now look backwards for the summary
                        List<string> summaryLines = new List<string>();
                        bool inSummary = false;
                        
                        // Walk backwards from the method declaration
                        for (int j = i - 1; j >= 0; j--)
                        {
                            string prevLine = lines[j].Trim();
                            
                            // If we hit the closing summary tag, start collecting
                            if (prevLine.Contains("/// </summary>"))
                            {
                                inSummary = true;
                                continue;
                            }
                            
                            // If we're in the summary, collect lines
                            if (inSummary)
                            {
                                if (prevLine.Contains("/// <summary>"))
                                {
                                    // Found the opening tag, we're done
                                    break;
                                }
                                else if (prevLine.StartsWith("///"))
                                {
                                    // Remove /// and add to our collection
                                    string content = prevLine.Substring(3).Trim();
                                    if (!string.IsNullOrEmpty(content))
                                    {
                                        summaryLines.Insert(0, content);
                                    }
                                }
                            }
                            else
                            {
                                // If we encounter a non-comment line before finding summary, stop
                                if (!string.IsNullOrEmpty(prevLine) && !prevLine.StartsWith("///"))
                                {
                                    break;
                                }
                            }
                        }
                        
                        // Combine the summary lines
                        if (summaryLines.Count > 0)
                        {
                            string summary = string.Join(" ", summaryLines);
                            descriptions[method.Name] = summary;
                        }
                        
                        break; // Found this method, move to next
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Could not extract method descriptions from {filePath}: {e.Message}");
        }
        
        return descriptions;
    }

    private Dictionary<string, string> ExtractEventDescriptions(string filePath, Type type)
    {
        Dictionary<string, string> descriptions = new Dictionary<string, string>();

        try
        {
            string[] lines = File.ReadAllLines(filePath);
            var events = GetOutputEvents(type);

            foreach (var evt in events)
            {
                // Find the field declaration line
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();

                    // Check if this line contains the event field declaration
                    // Look for "public UnityEvent" or "public UnityEvent<" followed by the field name
                    if ((line.Contains("public") && line.Contains("UnityEvent") && line.Contains(evt.Name)))
                    {
                        // Found the field declaration, now look backwards for the summary
                        List<string> summaryLines = new List<string>();
                        bool inSummary = false;

                        // Walk backwards from the field declaration
                        for (int j = i - 1; j >= 0; j--)
                        {
                            string prevLine = lines[j].Trim();

                            // If we hit the closing summary tag, start collecting
                            if (prevLine.Contains("/// </summary>"))
                            {
                                inSummary = true;
                                continue;
                            }

                            // If we're in the summary, collect lines
                            if (inSummary)
                            {
                                if (prevLine.Contains("/// <summary>"))
                                {
                                    // Found the opening tag, we're done
                                    break;
                                }
                                else if (prevLine.StartsWith("///"))
                                {
                                    // Remove /// and add to our collection
                                    string content = prevLine.Substring(3).Trim();
                                    if (!string.IsNullOrEmpty(content))
                                    {
                                        summaryLines.Insert(0, content);
                                    }
                                }
                            }
                            else
                            {
                                // If we encounter a non-comment line before finding summary, stop
                                if (!string.IsNullOrEmpty(prevLine) && !prevLine.StartsWith("///"))
                                {
                                    break;
                                }
                            }
                        }

                        // Combine the summary lines
                        if (summaryLines.Count > 0)
                        {
                            string summary = string.Join(" ", summaryLines);
                            descriptions[evt.Name] = summary;
                        }

                        break; // Found this event, move to next
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Could not extract event descriptions from {filePath}: {e.Message}");
        }

        return descriptions;
    }

    private void ClearDocumentation()
    {
        GameObject existing = GameObject.Find("ScriptDocumentation");
        if (existing != null)
        {
            DestroyImmediate(existing);
        }
    }
    
    private class ScriptInfo
    {
        public Type Type;
        public string Description;
        public Dictionary<string, string> MethodDescriptions;
        public Dictionary<string, string> EventDescriptions;
        public string FilePath;
    }
}