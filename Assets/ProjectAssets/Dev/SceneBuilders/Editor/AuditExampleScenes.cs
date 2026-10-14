using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Opens every example scene and reports problems that load fine, pass SceneSmokeTests and
/// log nothing, but still leave a scene that does not work for a student:
///   - persistent UnityEvent calls whose target is gone or whose method no longer exists
///   - missing scripts
///   - InputKeyPress / InputKeyCountdown with no binding (the old key dropdown was replaced,
///     so an upgraded component is silently deaf)
///   - more than one active PlayerInput (the second is paired to another device)
///   - a PlayerInput or InputActionEvent using an actions asset outside the package
///   - legacy StandaloneInputModule, which does nothing in an Input-System-only project
///   - transitive asset dependencies outside the package (a Package Manager install lacks them)
///
/// Writes Temp/egtk_scene_audit.txt. Read-only: scenes are opened, never saved.
/// Harness-only tooling; never shipped.
/// </summary>
public static class AuditExampleScenes
{
    private const string OutPath = "Temp/egtk_scene_audit.txt";

    [MenuItem("EGTK/Audit Example Scenes")]
    public static void Run()
    {
        var scenes = Directory.GetFiles("Assets/eventGameToolKit/ExampleScenes", "*.unity")
            .Concat(Directory.GetFiles("Assets/eventGameToolKit/Samples~/ExampleScenes", "*.unity"))
            .Select(p => p.Replace('\\', '/')).OrderBy(p => p).ToList();
        var buildScenes = new HashSet<string>(EditorBuildSettings.scenes.Select(s => s.path));

        var sb = new StringBuilder();
        foreach (string path in scenes)
        {
            sb.AppendLine($"=== {Path.GetFileName(path)}  inBuild={buildScenes.Contains(path)}");
            var problems = new List<string>();
            var counts = new SortedDictionary<string, int>();

            if (path.Contains("Samples~"))
            {
                // Unity does not import folders ending in ~, so these cannot be opened here.
                sb.AppendLine("  (Samples~ folder is not imported; skipped in-editor checks)");
                continue;
            }

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            GameObject[] all = scene.GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject).ToArray();

            foreach (GameObject go in all)
            {
                int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                if (missing > 0) problems.Add($"missing script x{missing} on '{GoPath(go)}'");

                foreach (MonoBehaviour mb in go.GetComponents<MonoBehaviour>())
                {
                    if (mb == null) continue;
                    string ns = mb.GetType().Namespace ?? "";
                    if (!ns.StartsWith("Unity") && !ns.StartsWith("TMPro"))
                        counts[mb.GetType().Name] = counts.TryGetValue(mb.GetType().Name, out int c) ? c + 1 : 1;

                    CheckPersistentCalls(mb, problems);
                    CheckKeyBinding(mb, problems);

                    if (mb is PlayerInput pi && pi.actions != null)
                    {
                        string a = AssetDatabase.GetAssetPath(pi.actions);
                        if (!a.StartsWith("Assets/eventGameToolKit/"))
                            problems.Add($"PlayerInput on '{GoPath(go)}' uses {a} (not the package's actions)");
                    }
                    if (mb.GetType().Name == "InputActionEvent")
                    {
                        var r = new SerializedObject(mb).FindProperty("actionReference")?.objectReferenceValue;
                        string a = r != null ? AssetDatabase.GetAssetPath(r) : "(none)";
                        if (!a.StartsWith("Assets/eventGameToolKit/"))
                            problems.Add($"InputActionEvent on '{GoPath(go)}' uses {a}");
                    }
                    if (mb.GetType().Name == "StandaloneInputModule")
                        problems.Add($"legacy StandaloneInputModule on '{GoPath(go)}' (UI clicks dead in Input-System-only projects)");
                }
            }

            int activePI = all.Count(g => g.activeInHierarchy && g.GetComponent<PlayerInput>() != null && g.GetComponent<PlayerInput>().enabled);
            if (activePI > 1) problems.Add($"{activePI} active PlayerInputs");

            foreach (string dep in AssetDatabase.GetDependencies(path, true))
            {
                if (dep == path || !dep.StartsWith("Assets/")) continue;
                if (dep.StartsWith("Assets/eventGameToolKit/") || dep.StartsWith("Assets/TextMesh Pro/")) continue;
                if (dep.EndsWith(".cs")) continue;
                problems.Add($"depends on outside-package asset {dep}");
            }

            sb.AppendLine($"  activePlayerInputs={activePI}  objects={all.Length}");
            sb.AppendLine("  components: " + string.Join(", ", counts.Select(kv => kv.Value > 1 ? $"{kv.Key}x{kv.Value}" : kv.Key)));
            foreach (string p in problems.Distinct()) sb.AppendLine("  !! " + p);
        }

        File.WriteAllText(OutPath, sb.ToString());
        Debug.Log($"AuditExampleScenes: wrote {OutPath}");
    }

    private static void CheckPersistentCalls(MonoBehaviour mb, List<string> problems)
    {
        var so = new SerializedObject(mb);
        SerializedProperty it = so.GetIterator();
        while (it.Next(true))
        {
            if (!it.propertyPath.EndsWith("m_PersistentCalls.m_Calls") || !it.isArray) continue;
            for (int i = 0; i < it.arraySize; i++)
            {
                SerializedProperty call = it.GetArrayElementAtIndex(i);
                Object target = call.FindPropertyRelative("m_Target").objectReferenceValue;
                string method = call.FindPropertyRelative("m_MethodName").stringValue;
                string evt = it.propertyPath.Replace(".m_PersistentCalls.m_Calls", "");
                string where = $"'{GoPath(mb.gameObject)}' {mb.GetType().Name}.{evt}";
                if (string.IsNullOrEmpty(method)) { problems.Add($"{where}: empty call slot"); continue; }
                if (target == null) { problems.Add($"{where}: target missing for {method}()"); continue; }
                bool exists = target.GetType()
                    .GetMember(method, MemberTypes.Method | MemberTypes.Property,
                               BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static)
                    .Length > 0
                    || (method.StartsWith("set_") && target.GetType().GetProperty(method.Substring(4)) != null);
                if (!exists) problems.Add($"{where}: {target.GetType().Name}.{method}() does not exist");
            }
        }
    }

    private static void CheckKeyBinding(MonoBehaviour mb, List<string> problems)
    {
        string n = mb.GetType().Name;
        if (n != "InputKeyPress" && n != "InputKeyCountdown") return;
        var f = mb.GetType().GetField("activation", BindingFlags.Instance | BindingFlags.NonPublic);
        var action = f?.GetValue(mb) as InputAction;
        if (action == null || action.bindings.Count == 0)
            problems.Add($"{n} on '{GoPath(mb.gameObject)}' has NO key bound — pressing anything does nothing");
        else
            problems.Add($"(info) {n} on '{GoPath(mb.gameObject)}' bound to {string.Join(" / ", action.bindings.Select(b => b.path))}");
    }

    private static string GoPath(GameObject go)
    {
        string p = go.name;
        for (Transform t = go.transform.parent; t != null; t = t.parent) p = t.name + "/" + p;
        return p;
    }
}
