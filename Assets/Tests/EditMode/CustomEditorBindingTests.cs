using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Guards against a custom editor referencing a serialized field that no longer exists.
///
/// This is the toolkit's most damaging silent failure. `serializedObject.FindProperty("x")`
/// returns null for a name that does not match a field, and `EditorGUILayout.PropertyField`
/// then throws — so renaming a field without updating its editor breaks that component's
/// entire Inspector, often in its default configuration. It produces no compiler error and
/// no test failure anywhere else.
///
/// It has already happened twice: renaming `storeKey` to `storeInputKey` and `fallbackKey`
/// to `fallbackInputKey` during the Input System migration broke GameStoreManager's and
/// InputInteractionZone's Inspectors, and neither was noticed until a manual review.
///
/// CLAUDE.md tells contributors to update both files together. This makes forgetting fail
/// the build instead of relying on memory.
/// </summary>
public class CustomEditorBindingTests
{
    private const string RuntimeRoot = "Assets/eventGameToolKit/Runtime";
    private const string EditorRoot = "Assets/eventGameToolKit/Editor";

    // Serialized field declarations. The type portion allows spaces so generics such as
    // `UnityEvent<int, int>` are matched — an earlier version of this check missed those
    // and reported false positives for every two-argument event in the toolkit.
    private static readonly Regex FieldPattern = new Regex(
        @"(?:\[SerializeField\][^;{]*?|public\s+)" +
        @"(?<type>[A-Za-z_][\w\.]*(?:\s*<[^>]*>)?(?:\[\])?)\s+" +
        @"(?<name>[a-zA-Z_]\w*)\s*(?:=|;)",
        RegexOptions.Compiled);

    private static readonly Regex CustomEditorPattern =
        new Regex(@"CustomEditor\(typeof\((?<target>[A-Za-z0-9_]+)\)", RegexOptions.Compiled);

    private static readonly Regex FindPropertyPattern =
        new Regex(@"FindProperty\(\s*""(?<prop>[^""]+)""", RegexOptions.Compiled);

    private static Dictionary<string, HashSet<string>> ReadRuntimeFields()
    {
        var map = new Dictionary<string, HashSet<string>>();
        if (!Directory.Exists(RuntimeRoot)) return map;

        foreach (string path in Directory.GetFiles(RuntimeRoot, "*.cs", SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(path);
            var names = new HashSet<string>();
            foreach (Match m in FieldPattern.Matches(source))
            {
                names.Add(m.Groups["name"].Value);
            }
            map[Path.GetFileNameWithoutExtension(path)] = names;
        }
        return map;
    }

    [Test]
    public void EveryFindPropertyMatchesARealSerializedField()
    {
        Dictionary<string, HashSet<string>> runtimeFields = ReadRuntimeFields();
        Assert.IsNotEmpty(runtimeFields, $"No runtime scripts found under {RuntimeRoot}.");

        var problems = new List<string>();

        foreach (string path in Directory.GetFiles(EditorRoot, "*.cs", SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(path);

            Match editorMatch = CustomEditorPattern.Match(source);
            if (!editorMatch.Success) continue;

            string target = editorMatch.Groups["target"].Value;
            if (!runtimeFields.TryGetValue(target, out HashSet<string> fields)) continue;

            foreach (Match pm in FindPropertyPattern.Matches(source))
            {
                string prop = pm.Groups["prop"].Value;

                // Unity's own built-in serialized properties are not declared in the script.
                if (prop.StartsWith("m_")) continue;

                if (!fields.Contains(prop))
                {
                    int line = source.Take(pm.Index).Count(c => c == '\n') + 1;
                    problems.Add($"{Path.GetFileName(path)}:{line} -> " +
                                 $"FindProperty(\"{prop}\") has no matching field on {target}");
                }
            }
        }

        Assert.IsEmpty(problems,
            "A custom editor references a serialized field that does not exist. The " +
            "Inspector for that component will throw or silently hide the field:\n  " +
            string.Join("\n  ", problems));
    }
}
