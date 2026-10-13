using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Adds a PuzzleSequenceChecker to the hand-authored puzzleExample scene.
///
/// This is a MIGRATION, not a builder: puzzleExample has hand-placed content and no
/// generator, so regenerating it would destroy that work. The script edits the scene in
/// place and is idempotent — re-running it on a migrated scene changes nothing.
///
/// What it adds, layered on the existing "make every switch glow" puzzle:
///   - The three pads now call PuzzleSwitch.Activate() instead of NextState(). Activate()
///     cycles the state exactly as NextState() did, and additionally fires onActivated —
///     which is the only thing PuzzleSequenceChecker listens to. With NextState() the
///     checker would silently never advance.
///   - A sequence checker: step on the pads in the numbered order 1-2-3 and a bonus orb
///     appears. One lap in order sets every switch to state 1, so it never interferes with
///     the glow puzzle, which needs a second lap.
///
/// Harness-only tooling; never shipped.
/// </summary>
public static class MigratePuzzleSequenceScene
{
    private const string ScenePath = "Assets/eventGameToolKit/ExampleScenes/puzzleExample.unity";
    private const string CheckerName = "sequenceManager";
    private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    private const string OrbMaterialPath = "Assets/eventGameToolKit/Materials/red.mat";

    // Order the player must step on the pads. Names are the hand-placed switches.
    private static readonly string[] SequenceOrder = { "switch (2)", "switch", "switch (1)" };

    [MenuItem("EGTK/Migrate puzzleExample (add Sequence Checker)")]
    public static void Migrate()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject managers = GameObject.Find("/MANAGERS");
        if (managers == null)
        {
            Debug.LogError("MigratePuzzleSequenceScene: /MANAGERS not found — is this the right scene?");
            return;
        }

        if (managers.transform.Find(CheckerName) != null)
        {
            Debug.Log("MigratePuzzleSequenceScene: already migrated, nothing to do.");
            return;
        }

        int repointed = RepointPadsToActivate();

        var switches = new PuzzleSwitch[SequenceOrder.Length];
        for (int i = 0; i < SequenceOrder.Length; i++)
        {
            GameObject go = GameObject.Find("/" + SequenceOrder[i]);
            switches[i] = go != null ? go.GetComponent<PuzzleSwitch>() : null;
            if (switches[i] == null)
            {
                Debug.LogError($"MigratePuzzleSequenceScene: PuzzleSwitch '{SequenceOrder[i]}' not found. Scene left unsaved.");
                return;
            }
        }

        // Use the existing Hint's rotation so new labels face the camera the same way it does.
        GameObject hint = GameObject.Find("/Hint");
        Quaternion labelRotation = hint != null ? hint.transform.rotation : Quaternion.identity;
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        // ── Bonus orb, hidden until the sequence is solved ────────────
        var bonusRoot = new GameObject("SequenceBonus");

        var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = "BonusOrb";
        orb.transform.SetParent(bonusRoot.transform);
        // Open floor between pads 1 and 2, clear of the player spawn at the origin.
        orb.transform.position = new Vector3(-5.5f, 1.5f, -1f);
        orb.transform.localScale = Vector3.one * 1.5f;
        Object.DestroyImmediate(orb.GetComponent<Collider>()); // decorative; the player walks through it
        var orbMat = AssetDatabase.LoadAssetAtPath<Material>(OrbMaterialPath);
        if (orbMat != null) orb.GetComponent<MeshRenderer>().sharedMaterial = orbMat;

        TextMeshPro bonusLabel = SceneLabel.Create(font, "BONUS!", orb.transform.position + Vector3.up * 2.4f,
            6f, 2f, new Color(0.1f, 0.1f, 0.2f), TextAlignmentOptions.Top, labelRotation);
        bonusLabel.name = "BonusLabel";
        bonusLabel.transform.SetParent(orb.transform, true);
        orb.SetActive(false);

        // ── Step numbers above each pad ──────────────────────────────
        for (int i = 0; i < switches.Length; i++)
        {
            TextMeshPro step = SceneLabel.Create(font, (i + 1).ToString(),
                switches[i].transform.position + Vector3.up * 3.2f, 3f, 3f, Color.white,
                TextAlignmentOptions.Top, labelRotation);
            step.name = $"StepLabel {i + 1}";
            step.transform.SetParent(bonusRoot.transform, true);
        }

        // ── The checker ──────────────────────────────────────────────
        var checkerGO = new GameObject(CheckerName);
        checkerGO.transform.SetParent(managers.transform, false);
        var checker = checkerGO.AddComponent<PuzzleSequenceChecker>();

        typeof(PuzzleSequenceChecker)
            .GetField("correctSequence", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(checker, switches);

        checker.onFirstTimeSolved ??= new UnityEvent();
        UnityEventTools.AddBoolPersistentListener(checker.onFirstTimeSolved, orb.SetActive, true);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"MigratePuzzleSequenceScene: added {CheckerName}, repointed {repointed} pad(s) to Activate().");
    }

    /// <summary>
    /// Every InputTriggerZone listener calling PuzzleSwitch.NextState is switched to
    /// Activate. Same visible behaviour; Activate also fires onActivated.
    /// </summary>
    private static int RepointPadsToActivate()
    {
        int count = 0;
        foreach (var zone in Object.FindObjectsByType<InputTriggerZone>(FindObjectsSortMode.None))
        {
            var so = new SerializedObject(zone);
            SerializedProperty calls = so.FindProperty("onTriggerEnterEvent.m_PersistentCalls.m_Calls");
            if (calls == null) continue;

            for (int i = 0; i < calls.arraySize; i++)
            {
                SerializedProperty call = calls.GetArrayElementAtIndex(i);
                if (!(call.FindPropertyRelative("m_Target").objectReferenceValue is PuzzleSwitch)) continue;

                SerializedProperty method = call.FindPropertyRelative("m_MethodName");
                if (method.stringValue != nameof(PuzzleSwitch.NextState)) continue;

                method.stringValue = nameof(PuzzleSwitch.Activate);
                count++;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        return count;
    }
}
