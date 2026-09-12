using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Builds the Actions test scene: one labelled station per Action component that had no
/// scene coverage, so each can be exercised and the smoke test can catch regressions in it.
///
/// Harness-only tooling; only the scene it produces is shipped.
/// </summary>
public static class BuildActionsTestScene
{
    private const string Art = "Assets/eventGameToolKit/ExampleScenes/Art2D";
    private const string Gen = "Assets/eventGameToolKit/ExampleScenes/GeneratedAssets";
    private const string ScenePath =
        "Assets/eventGameToolKit/ExampleScenes/Example2D_ActionsTest.unity";

    private static TMP_FontAsset _font;

    private static Sprite Tile(string f) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Art}/{f}");

    private static GameObject Sprite(string name, Sprite sprite, Vector2 pos, int order = 5)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return go;
    }

    private static TextMeshPro Label(string text, Vector2 pos, float width = 4.6f, float size = 0.62f)
        => SceneLabel.Create(_font, text, pos, width, size, Color.white);

    private static GameObject Indicator(Vector2 pos, Sprite sprite, Color tint)
    {
        var go = Sprite("Indicator", sprite, pos, 6);
        go.GetComponent<SpriteRenderer>().color = tint;
        return go;
    }

    // Every station is driven by clicking a crate, so the scene needs no player.
    private static InputMouseInteraction Clickable(GameObject go)
    {
        go.AddComponent<BoxCollider2D>();
        var mi = go.AddComponent<InputMouseInteraction>();
        mi.onMouseClick ??= new UnityEngine.Events.UnityEvent();
        mi.onMouseEnter ??= new UnityEngine.Events.UnityEvent();
        mi.onMouseExit ??= new UnityEngine.Events.UnityEvent();
        mi.onMouseDown ??= new UnityEngine.Events.UnityEvent();
        mi.onMouseUp ??= new UnityEngine.Events.UnityEvent();
        mi.onMouseHover ??= new UnityEngine.Events.UnityEvent();
        return mi;
    }

    private static void Wire(UnityEngine.Events.UnityEvent evt, UnityEngine.Events.UnityAction call)
        => UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(evt, call);

    private static void SetProp(Object target, string name, System.Action<SerializedProperty> set)
    {
        var so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(name);
        if (p == null) { Debug.LogWarning($"{target.GetType().Name}.{name} not found"); return; }
        set(p);
        so.ApplyModifiedPropertiesWithoutUndo();
    }


    private static void SetPrivateField(object target, string name, object value)
    {
        var f = target.GetType().GetField(name,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (f == null) { Debug.LogWarning($"{target.GetType().Name}.{name} not found"); return; }
        f.SetValue(target, value);
        EditorUtility.SetDirty((Object)target);
    }

    // Builds one indicator per option and a matching ActionToggle, so each option in a
    // random/shuffle list visibly does something different.
    private static ActionToggle[] OptionToggles(GameObject host, Sprite[] sprites, Color[] tints,
                                                Vector2 centre)
    {
        var toggles = new ActionToggle[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            float x = centre.x + (i - (sprites.Length - 1) / 2f) * 0.9f;
            GameObject indicator = Indicator(new Vector2(x, centre.y), sprites[i], tints[i]);
            var toggle = host.AddComponent<ActionToggle>();
            SetProp(toggle, "targets", p =>
            {
                p.arraySize = 1;
                p.GetArrayElementAtIndex(0).objectReferenceValue = indicator;
            });
            toggles[i] = toggle;
        }
        return toggles;
    }

    [MenuItem("EGTK/Rebuild Actions Test Scene")]
    public static void Build()
    {
        _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

        UnityEngine.SceneManagement.Scene scene =
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Sprite ground = Tile("tile_0000.png");
        Sprite crate = Tile("tile_0130.png");
        Sprite coin = Tile("tile_0151.png");
        Sprite tree = Tile("tile_0126.png");
        Sprite flag = Tile("tile_0111.png");
        if (ground == null || crate == null) { Debug.LogError("Art2D sprites missing"); return; }

        var blip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Gen}/Blip.wav");
        var thud = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Gen}/Thud.wav");
        var animController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            $"{Gen}/ExampleAnimator.controller");

        // ---- Camera ---------------------------------------------------------------
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 7.5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.13f, 0.15f, 0.21f);
        camGo.transform.position = new Vector3(11f, 4f, -10f);

        Label("EGTK ACTIONS TEST  —  click each crate", new Vector2(11f, 11.6f), 34f, 0.75f)
            .alignment = TextAlignmentOptions.Center;

        var level = new GameObject("Level");
        for (int x = -2; x <= 24; x++)
        {
            var t = Sprite($"Ground_{x}", ground, new Vector2(x, -1f), 0);
            t.transform.SetParent(level.transform);
        }

        float row = 6.5f;

        // 1. ActionPlaySound ---------------------------------------------------------
        var soundGo = Sprite("1_ActionPlaySound", crate, new Vector2(1.5f, row));
        var audioSource = soundGo.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        var play = soundGo.AddComponent<ActionPlaySound>();
        // Two clips, so the component's random-clip pick is audible.
        SetProp(play, "audioClips", p =>
        {
            p.arraySize = 2;
            p.GetArrayElementAtIndex(0).objectReferenceValue = blip;
            p.GetArrayElementAtIndex(1).objectReferenceValue = thud;
        });
        Wire(Clickable(soundGo).onMouseClick, play.Play);
        Label("1. ActionPlaySound\nClick: one of two clips", new Vector2(1.5f, row - 1.2f));

        // 2. ActionDisplayImage ------------------------------------------------------
        var imageGo = Sprite("2_ActionDisplayImage", crate, new Vector2(6f, row));
        var display = imageGo.AddComponent<ActionDisplayImage>();
        SetProp(display, "defaultImage", p => p.objectReferenceValue = coin);
        SetProp(display, "timeOnScreen", p => p.floatValue = 1.5f);
        Wire(Clickable(imageGo).onMouseClick, display.DisplayDefaultImage);
        Label("2. ActionDisplayImage\nClick to flash an image", new Vector2(6f, row - 1.2f));

        // 3. ActionRandomEvent -------------------------------------------------------
        var randomGo = Sprite("3_ActionRandomEvent", crate, new Vector2(10.5f, row));
        var random = randomGo.AddComponent<ActionRandomEvent>();
        ActionToggle[] randomToggles = OptionToggles(randomGo,
            new[] { coin, tree, flag },
            new[] { new Color(1f, 0.9f, 0.3f), new Color(0.4f, 1f, 0.5f), new Color(1f, 0.5f, 0.5f) },
            new Vector2(10.5f, row + 1.7f));

        // Each option owns its own event, so a weight of 1/1/2 makes the third twice as likely.
        var weighted = new WeightedEvent[randomToggles.Length];
        for (int i = 0; i < weighted.Length; i++)
        {
            weighted[i] = new WeightedEvent
            {
                label = $"Option {i + 1}",
                probability = i == 2 ? 2f : 1f,
                onSelected = new UnityEngine.Events.UnityEvent()
            };
            Wire(weighted[i].onSelected, randomToggles[i].Toggle);
        }
        SetPrivateField(random, "weightedEvents", weighted);
        Wire(Clickable(randomGo).onMouseClick, random.Trigger);
        Label("3. ActionRandomEvent\nClick: picks at random", new Vector2(10.5f, row - 1.2f));

        // 4. ActionShuffleEvent ------------------------------------------------------
        var shuffleGo = Sprite("4_ActionShuffleEvent", crate, new Vector2(15f, row));
        var shuffle = shuffleGo.AddComponent<ActionShuffleEvent>();
        ActionToggle[] shuffleToggles = OptionToggles(shuffleGo,
            new[] { coin, tree, flag },
            new[] { new Color(0.6f, 0.8f, 1f), new Color(0.4f, 1f, 0.5f), new Color(1f, 0.7f, 0.4f) },
            new Vector2(15f, row + 1.7f));

        var entries = new ShuffleEntry[shuffleToggles.Length];
        for (int i = 0; i < entries.Length; i++)
        {
            entries[i] = new ShuffleEntry
            {
                label = $"Entry {i + 1}",
                onSelected = new UnityEngine.Events.UnityEvent()
            };
            Wire(entries[i].onSelected, shuffleToggles[i].Toggle);
        }
        SetPrivateField(shuffle, "entries", entries);
        Wire(Clickable(shuffleGo).onMouseClick, shuffle.Trigger);
        Label("4. ActionShuffleEvent\nClick: no repeats until\nthe cycle ends", new Vector2(15f, row - 1.2f));

        // 5. ActionEventSequencer ----------------------------------------------------
        var seqGo = Sprite("5_ActionEventSequencer", crate, new Vector2(19.5f, row));
        var sequencer = seqGo.AddComponent<ActionEventSequencer>();
        SetProp(sequencer, "playOnStart", p => p.boolValue = true);
        SetProp(sequencer, "loop", p => p.boolValue = true);
        SetProp(sequencer, "duration", p => p.floatValue = 3f);
        Label("5. ActionEventSequencer\nRuns a timed sequence\non its own, looping",
              new Vector2(19.5f, row - 1.2f));

        // 6. ActionRandomMotion ------------------------------------------------------
        var motionGo = Sprite("6_ActionRandomMotion", tree, new Vector2(1.5f, 2f));
        var motion = motionGo.AddComponent<ActionRandomMotion>();
        SetProp(motion, "moveX", p => p.boolValue = true);
        SetProp(motion, "moveY", p => p.boolValue = true);
        SetProp(motion, "moveZ", p => p.boolValue = false);
        SetProp(motion, "rangeX", p => p.floatValue = 1.2f);
        SetProp(motion, "rangeY", p => p.floatValue = 1.2f);
        Label("6. ActionRandomMotion\nDrifts on its own", new Vector2(1.5f, 0.6f));

        // 7. ActionTriggerAnimatorParameter -----------------------------------------
        // Station 6's unbreakable "ActionRandomMotion" word overran its box at 2.5x font
        // and collided with this station's label, so the row-2 gap here was widened from
        // 4.5 to 6.5 units (row 1's shorter per-station text tolerates the tighter gap).
        var animTarget = Sprite("AnimatedSprite", flag, new Vector2(9.5f, 2.6f));
        var animator = animTarget.AddComponent<Animator>();
        animator.runtimeAnimatorController = animController;

        var paramGo = Sprite("7_ActionTriggerAnimatorParameter", crate, new Vector2(8f, 2f));
        var setParam = paramGo.AddComponent<ActionTriggerAnimatorParameter>();
        SetProp(setParam, "targetAnimator", p => p.objectReferenceValue = animator);
        SetProp(setParam, "parameterName", p => p.stringValue = "Spin");
        SetProp(setParam, "boolValue", p => p.boolValue = true);
        Wire(Clickable(paramGo).onMouseClick, setParam.TriggerParameter);
        Label("7. ActionTriggerAnimatorParameter\nClick: sets Spin = true",
              new Vector2(8f, 0.6f), 6f);

        // 8. ActionPlayCharacterEmoteAnimation --------------------------------------
        var emoteGo = Sprite("8_ActionPlayCharacterEmote", crate, new Vector2(15.5f, 2f));
        var emote = emoteGo.AddComponent<ActionPlayCharacterEmoteAnimation>();
        SetProp(emote, "characterAnimator", p => p.objectReferenceValue = animator);
        Label("8. ActionPlayCharacterEmote\nDrives the same Animator\n(set an emote in the Inspector)",
              new Vector2(15.5f, 0.6f), 6f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"EGTKACTIONS built {ScenePath}");
    }
}
