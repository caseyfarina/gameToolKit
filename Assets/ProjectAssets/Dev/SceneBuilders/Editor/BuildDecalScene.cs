using System;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Builds a 3D "station board" scene that demonstrates all four DecalAnimation components:
/// ActionBlinkDecal, ActionBlinkDecalOptimized, ActionDecalSequence and
/// ActionDecalSequenceLibrary. One large light-coloured wall is the shared projection
/// surface; four labelled stations sit in front of it, each with its own DecalProjector(s).
///
/// Stations 1-3 are autonomous (blink/play on start, loop). Station 4 (the Library) is
/// driven by two InputKeyPress components so a human can step through its sequences.
///
/// Harness-only tooling; only the scene it produces is shipped.
/// </summary>
public static class BuildDecalScene
{
    private const string ScenePath =
        "Assets/eventGameToolKit/ExampleScenes/Example3D_DecalAnimation.unity";
    private const string DecalRoot = "Assets/eventGameToolKit/ExampleScenes/GeneratedAssets/Decals";

    private static TMP_FontAsset _font;

    // ---- small helpers, following BuildPlatformPairingScene / BuildInputTestScene -------

    // Labels here sit against the light projection wall (the wall fills nearly the whole
    // camera view once decalY is behind it), not the scene's dark camera background, so they
    // default to black. A label placed off the wall can still opt into white.
    private static readonly Color WallTextColor = Color.black;

    private static TextMeshPro Label(string text, Vector3 pos, float width = 6.5f, float size = 0.5f,
                                      TextAlignmentOptions align = TextAlignmentOptions.Top,
                                      bool onWall = true)
    {
        Color color = onWall ? WallTextColor : Color.white;
        return SceneLabel.Create(_font, text, pos, width, size, color, align);
    }

    private static void SetProp(UnityEngine.Object target, string name, Action<SerializedProperty> set)
    {
        var so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(name);
        if (p == null) { Debug.LogWarning($"{target.GetType().Name}.{name} not found"); return; }
        set(p);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetInlineBinding(MonoBehaviour component, string fieldName, string path)
    {
        var field = component.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (field == null) { Debug.LogError($"{component.GetType().Name}.{fieldName} not found"); return; }

        var action = new InputAction("Activation", InputActionType.Button);
        InputActionSetupExtensions.AddBinding(action, path);
        field.SetValue(component, action);
        EditorUtility.SetDirty(component);
    }

    private static Material Mat(string fileName) =>
        AssetDatabase.LoadAssetAtPath<Material>($"{DecalRoot}/{fileName}");

    private static Texture2D Tex(string fileName) =>
        AssetDatabase.LoadAssetAtPath<Texture2D>($"{DecalRoot}/{fileName}");

    /// <summary>Creates a DecalProjector GameObject at a given position, facing +Z (into the wall).</summary>
    private static DecalProjector MakeProjector(string name, Vector3 pos, Vector2 size, Transform parent = null)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.identity; // local +Z points at the wall (wall is further +Z)
        if (parent != null) go.transform.SetParent(parent, true);

        var proj = go.AddComponent<DecalProjector>();
        proj.size = new Vector3(size.x, size.y, 8f); // deep enough to guarantee it reaches the wall
        return proj;
    }

    private static void SetMaterialFrames(UnityEngine.Object seqTarget, (Material mat, float duration)[] frames)
    {
        var so = new SerializedObject(seqTarget);
        SerializedProperty p = so.FindProperty("materialFrames");
        if (p == null) { Debug.LogWarning($"{seqTarget.GetType().Name}.materialFrames not found"); return; }

        p.arraySize = frames.Length;
        for (int i = 0; i < frames.Length; i++)
        {
            SerializedProperty element = p.GetArrayElementAtIndex(i);
            SerializedProperty matProp = element.FindPropertyRelative("material");
            SerializedProperty durProp = element.FindPropertyRelative("duration");
            if (matProp != null) matProp.objectReferenceValue = frames[i].mat;
            if (durProp != null) durProp.floatValue = frames[i].duration;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    [MenuItem("EGTK/Rebuild Decal Scene")]
    public static void Build()
    {
        _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

        UnityEngine.SceneManagement.Scene scene =
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ---- Wall (shared projection surface) --------------------------------------
        const float wallZ = 6f;
        const float wallCentreY = 7f;   // 26x14 wall spans y 0..14
        const float projectorZ = wallZ - 3f; // 3 units in front of the wall

        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "ProjectionWall";
        wall.transform.position = new Vector3(0f, wallCentreY, wallZ);
        wall.transform.localScale = new Vector3(26f, 14f, 0.5f);
        var wallMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        wallMat.color = new Color(0.82f, 0.82f, 0.78f);
        wall.GetComponent<Renderer>().sharedMaterial = wallMat;

        // ---- Camera and light --------------------------------------------------------
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        Vector3 camPos = new Vector3(0f, wallCentreY, -8.5f);
        cam.transform.position = camPos;
        cam.transform.rotation = Quaternion.LookRotation(
            new Vector3(0f, wallCentreY, wallZ) - camPos, Vector3.up);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.12f, 0.14f, 0.18f);

        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // The header sits ON the wall, not above it: at the current label size a two-line
        // block is tall enough that anything placed above the wall top (y = 9) still grows
        // down across it, which left the second line white-on-light and unreadable.
        Label("DECAL ANIMATION STATIONS\nAll four run automatically except Station 4, which takes input.",
              new Vector3(0f, 13.4f, wallZ - 1f), 22f, 0.55f, TextAlignmentOptions.Center);

        // Stations are a 2x2 grid, not a 4-wide row. A 38x9 strip forced the camera far
        // enough back to fit its width that the board filled only the middle third of a
        // 16:9 frame and the labels rendered a few pixels tall whatever their world size.
        float[] stationX = { -6.5f, 6.5f, -6.5f, 6.5f };
        float[] stationY = { 10f, 10f, 4.6f, 4.6f };

        // =====================================================================
        // Station 1 — ActionBlinkDecal (swaps whole Materials)
        // =====================================================================
        {
            DecalProjector proj = MakeProjector("Station1_ActionBlinkDecal",
                new Vector3(stationX[0], stationY[0], projectorZ), new Vector2(2.4f, 2.4f));
            proj.material = Mat("Mat_EyeOpen.mat");

            ActionBlinkDecal blink = proj.gameObject.AddComponent<ActionBlinkDecal>();
            SetProp(blink, "openEyesMaterial", p => p.objectReferenceValue = Mat("Mat_EyeOpen.mat"));
            SetProp(blink, "closedEyesMaterial", p => p.objectReferenceValue = Mat("Mat_EyeClosed.mat"));
            SetProp(blink, "timeBetweenBlinks", p => p.floatValue = 2f);
            SetProp(blink, "blinkDuration", p => p.floatValue = 0.2f);
            SetProp(blink, "blinkOnStart", p => p.boolValue = true);

            Label("1. ActionBlinkDecal\nSwaps whole Materials on the DecalProjector.\nBlinks automatically — no input needed.",
                  new Vector3(stationX[0], stationY[0] - 1.6f, wallZ - 1f), 6.5f);
        }

        // =====================================================================
        // Station 2 — ActionBlinkDecalOptimized (swaps a Texture on one material)
        // =====================================================================
        {
            DecalProjector proj = MakeProjector("Station2_ActionBlinkDecalOptimized",
                new Vector3(stationX[1], stationY[1], projectorZ), new Vector2(2.4f, 2.4f));
            proj.material = Mat("Mat_OptimizedBase.mat");

            ActionBlinkDecalOptimized opt = proj.gameObject.AddComponent<ActionBlinkDecalOptimized>();
            SetProp(opt, "openEyesTexture", p => p.objectReferenceValue = Tex("Tex_OptimizedEyeOpen.png"));
            SetProp(opt, "closedEyesTexture", p => p.objectReferenceValue = Tex("Tex_OptimizedEyeClosed.png"));
            SetProp(opt, "texturePropertyName", p => p.stringValue = GenerateExampleAssets.DecalBaseMapProperty);
            SetProp(opt, "timeBetweenBlinks", p => p.floatValue = 2.5f);
            SetProp(opt, "blinkDuration", p => p.floatValue = 0.2f);
            SetProp(opt, "blinkOnStart", p => p.boolValue = true);

            Label("2. ActionBlinkDecalOptimized\nSwaps a Texture on one shared material (cheaper).\nBlinks automatically — no input needed.",
                  new Vector3(stationX[1], stationY[1] - 1.6f, wallZ - 1f), 6.5f);
        }

        // =====================================================================
        // Station 3 — ActionDecalSequence (flipbook of MaterialFrame)
        // =====================================================================
        {
            DecalProjector proj = MakeProjector("Station3_ActionDecalSequence",
                new Vector3(stationX[2], stationY[2], projectorZ), new Vector2(2.4f, 2.4f));
            proj.material = Mat("Mat_SeqFrame1.mat");

            ActionDecalSequence seq = proj.gameObject.AddComponent<ActionDecalSequence>();
            SetMaterialFrames(seq, new (Material, float)[]
            {
                (Mat("Mat_SeqFrame1.mat"), 0.35f),
                (Mat("Mat_SeqFrame2.mat"), 0.35f),
                (Mat("Mat_SeqFrame3.mat"), 0.35f),
                (Mat("Mat_SeqFrame4.mat"), 0.35f),
            });
            SetProp(seq, "playOnStart", p => p.boolValue = true);
            SetProp(seq, "loop", p => p.boolValue = true);

            Label("3. ActionDecalSequence\nFlipbook of material frames with custom timing.\nLoops automatically — no input needed.",
                  new Vector3(stationX[2], stationY[2] - 1.6f, wallZ - 1f), 6.5f);
        }

        // =====================================================================
        // Station 4 — ActionDecalSequenceLibrary (switches between sequences)
        // =====================================================================
        {
            var libGo = new GameObject("Station4_ActionDecalSequenceLibrary");
            libGo.transform.position = new Vector3(stationX[3], stationY[3], projectorZ);

            // Three small sub-patches, each its own ActionDecalSequence (RequireComponent
            // needs its own DecalProjector per instance). Only the active one animates;
            // the others sit idle on their first frame.
            (string label, string prefix, Color tint)[] subSeqs =
            {
                ("A", "LibA", new Color(0.9f, 0.55f, 0.2f)),
                ("B", "LibB", new Color(0.85f, 0.25f, 0.75f)),
                ("C", "LibC", new Color(0.3f, 0.75f, 0.35f)),
            };

            ActionDecalSequence[] built = new ActionDecalSequence[subSeqs.Length];
            float[] offsets = { -0.9f, 0f, 0.9f };

            for (int i = 0; i < subSeqs.Length; i++)
            {
                DecalProjector subProj = MakeProjector($"Station4_Seq{subSeqs[i].label}",
                    new Vector3(stationX[3] + offsets[i], stationY[3], projectorZ),
                    new Vector2(1.3f, 1.3f), libGo.transform);

                string prefix = subSeqs[i].prefix;
                Material frame1 = Mat($"Mat_{prefix}_Frame1.mat");
                subProj.material = frame1;

                ActionDecalSequence seq = subProj.gameObject.AddComponent<ActionDecalSequence>();
                SetMaterialFrames(seq, new (Material, float)[]
                {
                    (frame1, 0.3f),
                    (Mat($"Mat_{prefix}_Frame2.mat"), 0.3f),
                    (Mat($"Mat_{prefix}_Frame3.mat"), 0.3f),
                });
                SetProp(seq, "playOnStart", p => p.boolValue = false);
                SetProp(seq, "loop", p => p.boolValue = true);

                built[i] = seq;
            }

            ActionDecalSequenceLibrary library = libGo.AddComponent<ActionDecalSequenceLibrary>();
            SetProp(library, "sequences", p =>
            {
                p.arraySize = built.Length;
                for (int i = 0; i < built.Length; i++)
                    p.GetArrayElementAtIndex(i).objectReferenceValue = built[i];
            });
            SetProp(library, "defaultSequenceIndex", p => p.intValue = 0);
            SetProp(library, "playOnStart", p => p.boolValue = true);

            // -- Input: N steps to the next sequence, R resets to the first --------------
            var keyNext = new GameObject("Station4_Input_Next");
            keyNext.transform.position = new Vector3(stationX[3], stationY[3] + 1.9f, projectorZ);
            InputKeyPress next = keyNext.AddComponent<InputKeyPress>();
            SetInlineBinding(next, "activation", "<Keyboard>/n");
            next.onPressEvent ??= new UnityEvent();
            UnityEventTools.AddVoidPersistentListener(next.onPressEvent, library.PlayNextSequence);

            var keyReset = new GameObject("Station4_Input_Reset");
            keyReset.transform.position = new Vector3(stationX[3], stationY[3] + 1.5f, projectorZ);
            InputKeyPress reset = keyReset.AddComponent<InputKeyPress>();
            SetInlineBinding(reset, "activation", "<Keyboard>/r");
            reset.onPressEvent ??= new UnityEvent();
            UnityEventTools.AddIntPersistentListener(reset.onPressEvent, library.PlaySequence, 0);

            Label("4. ActionDecalSequenceLibrary\nSwitches between several ActionDecalSequence.\nPress N: next sequence   Press R: reset to first",
                  new Vector3(stationX[3], stationY[3] - 1.6f, wallZ - 1f), 6.5f);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"EGTKDECAL built {ScenePath}");
    }
}
