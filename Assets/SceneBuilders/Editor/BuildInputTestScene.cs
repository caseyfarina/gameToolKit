using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Builds the input test scene: one labelled station per input component, so every
/// Input script in the toolkit can be exercised in a single play session.
///
/// Harness-only tooling. Lives outside Assets/eventGameToolKit/, so the robocopy sync
/// never ships it; only the scene it produces is shipped.
/// </summary>
public static class BuildInputTestScene
{
    private const string ArtRoot = "Assets/eventGameToolKit/ExampleScenes/Art2D";
    private const string ScenePath =
        "Assets/eventGameToolKit/ExampleScenes/Example2D_InputTest.unity";

    private static TMP_FontAsset _font;

    private static Sprite Tile(string file) =>
        AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}/{file}");

    private static GameObject Sprite(string name, Sprite sprite, Vector2 pos, int order = 5)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return go;
    }

    /// <summary>A world-space caption under a station.</summary>
    private static TextMeshPro Label(string text, Vector2 pos, float width = 4.2f,
                                     float size = 0.62f)
    {
        GameObject go = new GameObject("Label");
        go.transform.position = pos;

        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Top;
        tmp.color = Color.white;
        if (_font != null) tmp.font = _font;

        // World-space TMP: keep a small fontSize and scale the object down, which keeps
        // glyphs crisp at this camera size instead of blurry and tiny.
        RectTransform rt = tmp.rectTransform;
        rt.sizeDelta = new Vector2(width / 0.35f, 3.4f / 0.35f);
        rt.pivot = new Vector2(0.5f, 1f);   // grow downward from the placed position
        go.transform.localScale = Vector3.one * 0.35f;
        tmp.fontSize = size / 0.35f * 2.4f;
        return tmp;
    }

    // Trigger volumes are invisible, so each gets a faint marker showing where to walk.
    private static void ZoneMarker(GameObject parent, Sprite sprite, Vector2 pos, Color tint)
    {
        GameObject m = Sprite("ZoneMarker", sprite, pos, 1);
        m.transform.SetParent(parent.transform, false);
        m.transform.localPosition = pos;
        m.transform.localScale = new Vector3(1.6f, 2.4f, 1f);
        m.GetComponent<SpriteRenderer>().color = tint;
    }

    // A sprite that visibly turns on and off, used as the "did it fire?" readout.
    private static GameObject Indicator(Vector2 pos, Sprite sprite)
    {
        GameObject go = Sprite("Indicator", sprite, pos, 6);
        go.GetComponent<SpriteRenderer>().color = new Color(0.4f, 1f, 0.5f);
        return go;
    }

    private static void WireVoid(UnityEngine.Events.UnityEvent evt,
                                 UnityEngine.Events.UnityAction call)
    {
        UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(evt, call);
    }

    [MenuItem("EGTK/Rebuild Input Test Scene")]
    public static void Build()
    {
        _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

        UnityEngine.SceneManagement.Scene scene =
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        int groundLayer = LayerMask.NameToLayer("Ground");
        Sprite ground = Tile("tile_0000.png");
        Sprite crate = Tile("tile_0130.png");
        Sprite tree = Tile("tile_0126.png");
        Sprite coin = Tile("tile_0151.png");
        Sprite flag = Tile("tile_0111.png");
        Sprite playerSprite = Tile("Characters/tile_0000.png");
        if (ground == null || playerSprite == null) { Debug.LogError("Art2D sprites missing"); return; }

        // ---- Camera --------------------------------------------------------------
        GameObject camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 8.4f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.13f, 0.15f, 0.21f);
        camGo.transform.position = new Vector3(11.5f, 5.0f, -10f);

        Label("EGTK INPUT TEST  —  every Input component, one station each", new Vector2(11.5f, 12.6f), 34f, 0.75f)
            .alignment = TextAlignmentOptions.Center;

        // ---- Floor ---------------------------------------------------------------
        GameObject level = new GameObject("Level");
        for (int x = -2; x <= 24; x++)
        {
            GameObject t = Sprite($"Ground_{x}", ground, new Vector2(x, -1f), 0);
            t.transform.SetParent(level.transform);
            t.layer = groundLayer;
            t.AddComponent<BoxCollider2D>();
        }

        // ---- Managers -------------------------------------------------------------
        // InputCheckpointZone warns and does nothing without this.
        GameObject managers = new GameObject("Managers");
        managers.AddComponent<GameCheckpointManager>();

        // ---- Player --------------------------------------------------------------
        GameObject player = new GameObject("Player") { tag = "Player" };
        player.transform.position = new Vector2(0f, 1f);
        SpriteRenderer psr = player.AddComponent<SpriteRenderer>();
        psr.sprite = playerSprite;
        psr.sortingOrder = 10;

        Rigidbody2D body = player.AddComponent<Rigidbody2D>();
        body.freezeRotation = true;
        CapsuleCollider2D cap = player.AddComponent<CapsuleCollider2D>();
        cap.size = new Vector2(0.7f, 0.95f);

        PlayerInput pi = player.AddComponent<PlayerInput>();
        pi.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
            "Assets/eventGameToolKit/EGTK_InputSystem_Actions.inputactions");
        pi.defaultActionMap = "Player";
        pi.notificationBehavior = PlayerNotifications.SendMessages;

        CharacterController2D controller = player.AddComponent<CharacterController2D>();
        SerializedObject cso = new SerializedObject(controller);
        cso.FindProperty("groundLayer").intValue = 1 << groundLayer;
        cso.ApplyModifiedPropertiesWithoutUndo();

        Label("WASD to move\nSpace to jump", new Vector2(0f, 4.6f), 6f, 0.5f);

        // =========================================================================
        // WALK-THROUGH STATIONS (bottom row) — the player must reach these
        // =========================================================================

        // -- InputTriggerZone ------------------------------------------------------
        GameObject tz = new GameObject("1_InputTriggerZone");
        tz.transform.position = new Vector2(4f, 0.5f);
        BoxCollider2D tzc = tz.AddComponent<BoxCollider2D>();
        tzc.isTrigger = true;
        tzc.size = new Vector2(2f, 3f);
        InputTriggerZone zone = tz.AddComponent<InputTriggerZone>();
        zone.onTriggerEnterEvent ??= new UnityEngine.Events.UnityEvent();
        zone.onTriggerExitEvent ??= new UnityEngine.Events.UnityEvent();
        zone.onTriggerStayEvent ??= new UnityEngine.Events.UnityEvent();

        GameObject tzInd = Indicator(new Vector2(4f, 2.6f), coin);
        ActionToggle tzToggle = tz.AddComponent<ActionToggle>();
        SetToggleTarget(tzToggle, tzInd);
        WireVoid(zone.onTriggerEnterEvent, tzToggle.Toggle);
        WireVoid(zone.onTriggerExitEvent, tzToggle.Toggle);
        ZoneMarker(tz, crate, Vector2.zero, new Color(0.35f, 0.75f, 1f, 0.30f));
        Label("1. InputTriggerZone\nWalk in and out", new Vector2(4f, 4.6f));

        // -- InputInteractionZone --------------------------------------------------
        GameObject iz = new GameObject("2_InputInteractionZone");
        iz.transform.position = new Vector2(8f, 0.5f);
        BoxCollider2D izc = iz.AddComponent<BoxCollider2D>();
        izc.isTrigger = true;
        izc.size = new Vector2(2.4f, 3f);
        InputInteractionZone interact = iz.AddComponent<InputInteractionZone>();
        // Give the floating "press E" prompt a sprite, or the component warns and shows nothing.
        SerializedObject izso = new SerializedObject(interact);
        SerializedProperty promptSprite = izso.FindProperty("promptSprite");
        if (promptSprite != null) promptSprite.objectReferenceValue = coin;
        SerializedProperty promptOrient = izso.FindProperty("promptOrientation");
        if (promptOrient != null) promptOrient.enumValueIndex = 1; // FixedWorld: 2D needs no billboarding
        izso.ApplyModifiedPropertiesWithoutUndo();

        interact.onInteract ??= new UnityEngine.Events.UnityEvent();
        interact.onEnter ??= new UnityEngine.Events.UnityEvent();
        interact.onExit ??= new UnityEngine.Events.UnityEvent();

        GameObject izInd = Indicator(new Vector2(8f, 2.6f), tree);
        ActionToggle izToggle = iz.AddComponent<ActionToggle>();
        SetToggleTarget(izToggle, izInd);
        WireVoid(interact.onInteract, izToggle.Toggle);
        ZoneMarker(iz, crate, Vector2.zero, new Color(1f, 0.8f, 0.3f, 0.30f));
        Label("2. InputInteractionZone\nStand here, press E", new Vector2(8f, 4.6f));

        // -- InputCheckpointZone ---------------------------------------------------
        GameObject cz = new GameObject("3_InputCheckpointZone");
        cz.transform.position = new Vector2(12f, 0.5f);
        BoxCollider2D czc = cz.AddComponent<BoxCollider2D>();
        czc.isTrigger = true;
        czc.size = new Vector2(2f, 3f);
        InputCheckpointZone checkpoint = cz.AddComponent<InputCheckpointZone>();
        checkpoint.onCheckpointActivated ??= new UnityEngine.Events.UnityEvent();
        checkpoint.onCheckpointPositionSaved ??= new UnityEngine.Events.UnityEvent<Vector3>();

        GameObject czInd = Indicator(new Vector2(12f, 2.6f), flag);
        czInd.SetActive(false);
        ActionToggle czToggle = cz.AddComponent<ActionToggle>();
        SetToggleTarget(czToggle, czInd);
        WireVoid(checkpoint.onCheckpointActivated, czToggle.Toggle);
        ZoneMarker(cz, crate, Vector2.zero, new Color(0.6f, 1f, 0.6f, 0.30f));
        Label("3. InputCheckpointZone\nWalk in once", new Vector2(12f, 4.6f));

        // -- InputCollisionEnter ---------------------------------------------------
        GameObject pad = Sprite("4_InputCollisionEnter", crate, new Vector2(16f, 0f), 4);
        pad.AddComponent<BoxCollider2D>();
        Rigidbody2D padBody = pad.AddComponent<Rigidbody2D>();
        padBody.bodyType = RigidbodyType2D.Kinematic;
        InputCollisionEnter collide = pad.AddComponent<InputCollisionEnter>();
        collide.onCollisionEnter ??= new UnityEngine.Events.UnityEvent();

        GameObject collInd = Indicator(new Vector2(16f, 2.6f), coin);
        ActionToggle collToggle = pad.AddComponent<ActionToggle>();
        SetToggleTarget(collToggle, collInd);
        WireVoid(collide.onCollisionEnter, collToggle.Toggle);
        Label("4. InputCollisionEnter\nJump onto the crate", new Vector2(16f, 4.6f));

        // =========================================================================
        // MOUSE AND KEY STATIONS (top row) — reachable without walking
        // =========================================================================

        float topY = 8.8f;

        // -- InputKeyPress ---------------------------------------------------------
        GameObject kp = Sprite("5_InputKeyPress", crate, new Vector2(2f, topY));
        InputKeyPress keyPress = kp.AddComponent<InputKeyPress>();
        SetInlineBinding(keyPress, "activation", "<Keyboard>/1");
        keyPress.onPressEvent ??= new UnityEngine.Events.UnityEvent();

        GameObject kpInd = Indicator(new Vector2(2f, topY + 1.6f), coin);
        ActionToggle kpToggle = kp.AddComponent<ActionToggle>();
        SetToggleTarget(kpToggle, kpInd);
        WireVoid(keyPress.onPressEvent, kpToggle.Toggle);
        Label("5. InputKeyPress\nPress 1", new Vector2(2f, topY - 1.3f));

        // -- InputKeyCountdown -----------------------------------------------------
        GameObject kc = Sprite("6_InputKeyCountdown", crate, new Vector2(6.5f, topY));
        InputKeyCountdown countdown = kc.AddComponent<InputKeyCountdown>();
        SetInlineBinding(countdown, "activation", "<Keyboard>/2");
        SerializedObject kcso = new SerializedObject(countdown);
        kcso.FindProperty("countDownValue").intValue = 3;
        kcso.ApplyModifiedPropertiesWithoutUndo();
        countdown.onCountLimitKey ??= new UnityEngine.Events.UnityEvent();

        GameObject kcInd = Indicator(new Vector2(6.5f, topY + 1.6f), tree);
        ActionToggle kcToggle = kc.AddComponent<ActionToggle>();
        SetToggleTarget(kcToggle, kcInd);
        WireVoid(countdown.onCountLimitKey, kcToggle.Toggle);
        Label("6. InputKeyCountdown\nPress 2 three times", new Vector2(6.5f, topY - 1.3f));

        // -- InputMouseInteraction -------------------------------------------------
        GameObject mi = Sprite("7_InputMouseInteraction", crate, new Vector2(11f, topY));
        mi.AddComponent<BoxCollider2D>();
        InputMouseInteraction mouse = mi.AddComponent<InputMouseInteraction>();
        mouse.onMouseClick ??= new UnityEngine.Events.UnityEvent();
        mouse.onMouseEnter ??= new UnityEngine.Events.UnityEvent();
        mouse.onMouseExit ??= new UnityEngine.Events.UnityEvent();
        mouse.onMouseDown ??= new UnityEngine.Events.UnityEvent();
        mouse.onMouseUp ??= new UnityEngine.Events.UnityEvent();
        mouse.onMouseHover ??= new UnityEngine.Events.UnityEvent();

        GameObject miInd = Indicator(new Vector2(11f, topY + 1.6f), coin);
        ActionToggle miToggle = mi.AddComponent<ActionToggle>();
        SetToggleTarget(miToggle, miInd);
        WireVoid(mouse.onMouseClick, miToggle.Toggle);
        Label("7. InputMouseInteraction\nClick the crate", new Vector2(11f, topY - 1.3f));

        // -- InputClickDrag --------------------------------------------------------
        GameObject cd = Sprite("8_InputClickDrag", coin, new Vector2(15.5f, topY));
        cd.AddComponent<BoxCollider2D>();
        InputClickDrag drag = cd.AddComponent<InputClickDrag>();
        SerializedObject dso = new SerializedObject(drag);
        SerializedProperty plane = dso.FindProperty("dragPlane");
        if (plane != null) plane.enumValueIndex = 2; // WorldXY
        dso.ApplyModifiedPropertiesWithoutUndo();
        Label("8. InputClickDrag\nDrag the coin", new Vector2(15.5f, topY - 1.3f));

        // -- InputClickRotate ------------------------------------------------------
        GameObject cr = Sprite("9_InputClickRotate", flag, new Vector2(20f, topY));
        cr.AddComponent<BoxCollider2D>();
        cr.AddComponent<InputClickRotate>();
        Label("9. InputClickRotate\nDrag to spin the flag", new Vector2(20f, topY - 1.3f));

        // =========================================================================
        // NO-TARGET COMPONENTS
        // =========================================================================

        // -- InputOnStart ----------------------------------------------------------
        GameObject os = new GameObject("10_InputOnStart");
        os.transform.position = new Vector2(23f, topY);
        InputOnStart onStart = os.AddComponent<InputOnStart>();
        onStart.onAwake ??= new UnityEngine.Events.UnityEvent();
        onStart.onStart ??= new UnityEngine.Events.UnityEvent();

        GameObject osInd = Indicator(new Vector2(23f, topY + 1.6f), tree);
        osInd.SetActive(false);
        ActionToggle osToggle = os.AddComponent<ActionToggle>();
        SetToggleTarget(osToggle, osInd);
        WireVoid(onStart.onStart, osToggle.Toggle);
        Label("10. InputOnStart\nFires by itself at launch", new Vector2(24f, topY - 1.3f));

        // -- InputActionEvent ------------------------------------------------------
        GameObject ae = new GameObject("11_InputActionEvent");
        ae.transform.position = new Vector2(20f, 2.5f);
        InputActionEvent actionEvent = ae.AddComponent<InputActionEvent>();
        InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
            "Assets/eventGameToolKit/EGTK_InputSystem_Actions.inputactions");
        if (asset != null)
        {
            foreach (Object sub in AssetDatabase.LoadAllAssetsAtPath(
                         "Assets/eventGameToolKit/EGTK_InputSystem_Actions.inputactions"))
            {
                if (sub is InputActionReference reference && reference.action != null &&
                    reference.action.name == "Attack")
                {
                    SerializedObject aeso = new SerializedObject(actionEvent);
                    aeso.FindProperty("actionReference").objectReferenceValue = reference;
                    aeso.ApplyModifiedPropertiesWithoutUndo();
                    break;
                }
            }
        }

        GameObject aeInd = Indicator(new Vector2(20f, 4.1f), coin);
        ActionToggle aeToggle = ae.AddComponent<ActionToggle>();
        SetToggleTarget(aeToggle, aeInd);
        actionEvent.onActionTriggered ??= new UnityEngine.Events.UnityEvent();
        WireVoid(actionEvent.onActionTriggered, aeToggle.Toggle);
        Label("11. InputActionEvent\nLeft-click (Attack)", new Vector2(20.5f, 0.4f));

        // -- InputQuitGame ---------------------------------------------------------
        GameObject qg = Sprite("12_InputQuitGame", crate, new Vector2(24f, 2.5f));
        qg.GetComponent<SpriteRenderer>().color = new Color(1f, 0.45f, 0.45f);
        qg.AddComponent<BoxCollider2D>();
        InputMouseInteraction quitClick = qg.AddComponent<InputMouseInteraction>();
        quitClick.onMouseClick ??= new UnityEngine.Events.UnityEvent();
        quitClick.onMouseEnter ??= new UnityEngine.Events.UnityEvent();
        quitClick.onMouseExit ??= new UnityEngine.Events.UnityEvent();
        quitClick.onMouseDown ??= new UnityEngine.Events.UnityEvent();
        quitClick.onMouseUp ??= new UnityEngine.Events.UnityEvent();
        quitClick.onMouseHover ??= new UnityEngine.Events.UnityEvent();
        InputQuitGame quit = qg.AddComponent<InputQuitGame>();
        WireVoid(quitClick.onMouseClick, quit.QuitGame);
        Label("12. InputQuitGame\nClick to quit", new Vector2(24.5f, 0.4f));

        // -- Not included ----------------------------------------------------------
        Label("Not shown: InputFPMouseInteraction is first-person only and needs a 3D reticle camera",
              new Vector2(11.5f, -2.4f), 34f, 0.45f).alignment = TextAlignmentOptions.Center;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"Built input test scene at {ScenePath}");
    }

    private static void SetToggleTarget(ActionToggle toggle, GameObject target)
    {
        SerializedObject so = new SerializedObject(toggle);
        SerializedProperty targets = so.FindProperty("targets");
        targets.arraySize = 1;
        targets.GetArrayElementAtIndex(0).objectReferenceValue = target;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetInlineBinding(MonoBehaviour component, string fieldName, string path)
    {
        var field = component.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (field == null) { Debug.LogError($"{component.GetType().Name}.{fieldName} not found"); return; }

        InputAction action = new InputAction("Activation", InputActionType.Button);
        InputActionSetupExtensions.AddBinding(action, path);
        field.SetValue(component, action);
        EditorUtility.SetDirty(component);
    }
}
