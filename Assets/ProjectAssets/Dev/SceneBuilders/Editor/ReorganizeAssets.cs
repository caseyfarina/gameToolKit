using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-shot reorganisation of the Assets folder so students see a clear hierarchy:
/// their own work first, then the toolkit, then everything else out of the way.
///
/// Every move goes through AssetDatabase.MoveAsset. Moving folders on the filesystem
/// would orphan .meta files and silently break every scene reference; MoveAsset keeps
/// GUIDs intact and rewrites nothing else.
///
/// Safe to re-run: each step checks whether it has already been done.
/// </summary>
public static class ReorganizeAssets
{
    private const string Student = "Assets/_STUDENT_WORK";
    private const string Project = "Assets/ProjectAssets";

    private static readonly List<string> Log = new List<string>();

    [MenuItem("EGTK/Reorganize Assets Folder")]
    public static void Run()
    {
        Log.Clear();

        // Deliberately NOT wrapped in StartAssetEditing/StopAssetEditing. Batching defers
        // asset database registration, so folders created inside the batch do not exist yet
        // as far as MoveAsset is concerned, and every move fails with "Parent directory is
        // not in asset database". Correctness matters more than speed for a one-shot move.
        CreateFolders();
        AssetDatabase.Refresh();

        DeleteDeadFolders();
        MoveFolders();
        MoveLooseRootFiles();
        AssetDatabase.Refresh();

        Debug.Log("EGTKREORG\n" + string.Join("\n", Log));
    }

    private static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) Folder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
        Log.Add($"created  {path}");
    }

    private static void CreateFolders()
    {
        // Students save everything here. The underscore pins it to the top of the
        // Project window, above the toolkit.
        Folder(Student);
        Folder($"{Student}/Scenes");
        Folder($"{Student}/Scripts");
        Folder($"{Student}/Art");
        Folder($"{Student}/Prefabs");

        Folder(Project);
        Folder($"{Project}/Animations");
        Folder($"{Project}/Art");
        Folder($"{Project}/Input");
        Folder($"{Project}/Scenes");
        Folder($"{Project}/Dev");
        Folder($"{Project}/ThirdParty");
    }

    // Confirmed unreachable from eventGameToolKit/ and Assets/Scenes/ by a transitive
    // GUID walk before deletion.
    private static void DeleteDeadFolders()
    {
        foreach (string dead in new[]
                 {
                     "Assets/_Recovery",       // Unity crash-recovery leftovers
                     "Assets/SceneReports",    // regenerable doc-generator output
                     "Assets/TutorialInfo",    // Unity project-template leftovers
                     "Assets/lighting",        // lighting experiment, unreferenced
                 })
        {
            if (AssetDatabase.IsValidFolder(dead) && AssetDatabase.DeleteAsset(dead))
                Log.Add($"deleted  {dead}");
        }

        if (File.Exists("Assets/lighting.unity") && AssetDatabase.DeleteAsset("Assets/lighting.unity"))
            Log.Add("deleted  Assets/lighting.unity");
    }

    private static void Move(string from, string to)
    {
        if (!AssetDatabase.IsValidFolder(from) && !File.Exists(from)) return;
        if (AssetDatabase.IsValidFolder(to) || File.Exists(to)) return;

        string error = AssetDatabase.MoveAsset(from, to);
        Log.Add(string.IsNullOrEmpty(error) ? $"moved    {from} -> {to}"
                                            : $"FAILED   {from}: {error}");
    }

    private static void MoveFolders()
    {
        Move("Assets/Animations", $"{Project}/Animations/Clips");
        Move("Assets/CharacterAnimations", $"{Project}/Animations/Character");
        Move("Assets/Materials", $"{Project}/Materials");
        Move("Assets/Scenes", $"{Project}/Scenes/TestScenes");

        // Harness-only tooling; students never need it, and it should not sit beside
        // their own folder.
        Move("Assets/Tests", $"{Project}/Dev/Tests");
        // SceneBuilders deliberately not moved here: this script lives inside it, and
        // relocating its own assembly mid-run risks a recompile before the batch ends.

        // Kept, not deleted: the package still references assets inside both of these,
        // so removing them would leave missing textures in shipped example scenes.
        Move("Assets/StarterAssets", $"{Project}/ThirdParty/StarterAssets");
        Move("Assets/Samples", $"{Project}/ThirdParty/Samples");
    }

    // The 20-odd loose files sitting directly in Assets/ are the main reason the folder
    // reads as chaotic.
    private static void MoveLooseRootFiles()
    {
        foreach (string path in Directory.GetFiles("Assets"))
        {
            string file = Path.GetFileName(path);
            if (file.EndsWith(".meta")) continue;

            string ext = Path.GetExtension(file).ToLowerInvariant();
            string destination;

            switch (ext)
            {
                case ".inputactions":
                case ".inputsettings":
                    destination = $"{Project}/Input/{file}";
                    break;
                case ".cs":
                    // Generated input action wrapper, keeps company with its .inputactions
                    destination = $"{Project}/Input/{file}";
                    break;
                case ".unity":
                    destination = $"{Project}/Scenes/{file}";
                    break;
                case ".anim":
                case ".controller":
                    destination = $"{Project}/Animations/{file}";
                    break;
                case ".asset":
                    destination = file.Contains("inputsettings")
                        ? $"{Project}/Input/{file}"
                        : $"{Project}/Art/{file}";
                    break;
                default:
                    // fbx, mat, png, shadergraph, lighting, everything else
                    destination = $"{Project}/Art/{file}";
                    break;
            }

            Move($"Assets/{file}", destination);
        }
    }
}
