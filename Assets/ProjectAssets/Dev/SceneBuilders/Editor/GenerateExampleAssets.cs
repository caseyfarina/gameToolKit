using System;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Generates the small assets the example scenes need: two short sound effects and an
/// Animator Controller with a couple of clips.
///
/// These are generated rather than borrowed. The only audio already in the project lives
/// in ThirdParty/StarterAssets, and the only rigged animators come from Unity samples —
/// referencing either from a shipped example scene would repeat the packaging bug where
/// the package depends on assets it does not distribute. Everything produced here is
/// written into the package itself and owes nothing to anyone.
///
/// Harness-only tooling. Re-runnable: existing assets are left alone.
/// </summary>
public static class GenerateExampleAssets
{
    private const string Root = "Assets/eventGameToolKit/ExampleScenes/GeneratedAssets";
    private const string DecalRoot = Root + "/Decals";

    [MenuItem("EGTK/Generate Example Assets")]
    public static void Generate()
    {
        EnsureFolder();

        WriteTone($"{Root}/Blip.wav", frequency: 880f, seconds: 0.12f);
        WriteTone($"{Root}/Thud.wav", frequency: 160f, seconds: 0.18f);

        AssetDatabase.Refresh();
        BuildAnimator();
        GenerateDecalAssets();
        AssetDatabase.SaveAssets();

        Debug.Log("EGTKGEN example assets ready in " + Root);
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(Root))
        {
            AssetDatabase.CreateFolder("Assets/eventGameToolKit/ExampleScenes", "GeneratedAssets");
        }
    }

    // ---- Decal assets ----------------------------------------------------------------
    //
    // Materials and textures for Example3D_DecalAnimation. Generated rather than borrowed,
    // same reasoning as the audio/animator assets above. Procedural Texture2D shapes are
    // simple and readable at station scale rather than pretty: an eye (open/closed) and a
    // rotating wedge used as a cheap "flipbook" frame indicator.
    //
    // URP's built-in decal shader is "Shader Graphs/Decal" and its base texture property
    // is the reference name "Base_Map" (verified against the shader's own property list —
    // NOT "_BaseMap" and NOT "_BaseColorMap", both of which are absent from this shader).

    public const string DecalShaderName = "Shader Graphs/Decal";
    public const string DecalBaseMapProperty = "Base_Map";

    private static void GenerateDecalAssets()
    {
        if (!AssetDatabase.IsValidFolder(DecalRoot))
        {
            AssetDatabase.CreateFolder(Root, "Decals");
        }

        Shader decalShader = Shader.Find(DecalShaderName);
        if (decalShader == null)
        {
            Debug.LogError($"EGTKGEN: shader '{DecalShaderName}' not found — decal assets not generated.");
            return;
        }

        // Station 1 — ActionBlinkDecal: two complete materials swapped wholesale.
        MakeMaterial(decalShader, $"{DecalRoot}/Mat_EyeOpen.mat",
            SaveTexture($"{DecalRoot}/Tex_EyeOpen.png", MakeEyeTexture(open: true)));
        MakeMaterial(decalShader, $"{DecalRoot}/Mat_EyeClosed.mat",
            SaveTexture($"{DecalRoot}/Tex_EyeClosed.png", MakeEyeTexture(open: false)));

        // Station 2 — ActionBlinkDecalOptimized: one material, two textures swapped on it.
        Texture2D optOpen = SaveTexture($"{DecalRoot}/Tex_OptimizedEyeOpen.png", MakeEyeTexture(open: true));
        SaveTexture($"{DecalRoot}/Tex_OptimizedEyeClosed.png", MakeEyeTexture(open: false));
        MakeMaterial(decalShader, $"{DecalRoot}/Mat_OptimizedBase.mat", optOpen);

        // Station 3 — ActionDecalSequence: a 4-frame rotating wedge "flipbook", blue.
        Color blue = new Color(0.25f, 0.45f, 0.9f);
        for (int i = 0; i < 4; i++)
        {
            float angle = i * 90f;
            MakeMaterial(decalShader, $"{DecalRoot}/Mat_SeqFrame{i + 1}.mat",
                SaveTexture($"{DecalRoot}/Tex_SeqFrame{i + 1}.png", MakeWedgeTexture(blue, angle)));
        }

        // Station 4 — ActionDecalSequenceLibrary: three 3-frame sequences, each its own colour.
        MakeLibrarySequenceMaterials(decalShader, "LibA", new Color(0.9f, 0.55f, 0.2f));  // orange
        MakeLibrarySequenceMaterials(decalShader, "LibB", new Color(0.85f, 0.25f, 0.75f)); // magenta
        MakeLibrarySequenceMaterials(decalShader, "LibC", new Color(0.3f, 0.75f, 0.35f));  // green

        Debug.Log("EGTKGEN decal assets ready in " + DecalRoot);
    }

    private static void MakeLibrarySequenceMaterials(Shader decalShader, string prefix, Color colour)
    {
        for (int i = 0; i < 3; i++)
        {
            float angle = i * 120f;
            MakeMaterial(decalShader, $"{DecalRoot}/Mat_{prefix}_Frame{i + 1}.mat",
                SaveTexture($"{DecalRoot}/Tex_{prefix}_Frame{i + 1}.png", MakeWedgeTexture(colour, angle)));
        }
    }

    // Every decal shape is drawn onto a fully transparent background (alpha 0) so the
    // DecalProjector's quad has no visible rectangular edge — only the shape itself is
    // opaque. The alpha channel is feathered across a couple of pixels at the shape
    // boundary rather than left as a hard, jagged 0/1 cut.
    private const float EdgeFeather = 1.5f;

    /// <summary>
    /// Alpha for a pixel given its signed distance inside a shape boundary (positive =
    /// inside, negative = outside). Ramps linearly from 0 to 1 across <see cref="EdgeFeather"/>
    /// world (texel) units, centred on the boundary.
    /// </summary>
    private static float EdgeAlpha(float insideDistance) =>
        Mathf.Clamp01(insideDistance / EdgeFeather + 0.5f);

    /// <summary>
    /// Draws a simple eye: open = white sclera + blue iris + black pupil; closed = a black
    /// eyelid bar. Everything outside the shape is fully transparent.
    /// </summary>
    private static Texture2D MakeEyeTexture(bool open, int size = 128)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float scleraR = size * 0.42f;
        float irisR = size * 0.22f;
        float pupilR = size * 0.10f;
        float lidHalfHeight = size * 0.06f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color rgb;
                float alpha;
                if (open)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    if (d > irisR) rgb = Color.white;
                    else if (d > pupilR) rgb = new Color(0.2f, 0.4f, 0.9f);
                    else rgb = Color.black;
                    alpha = EdgeAlpha(scleraR - d);
                }
                else
                {
                    // Signed distance to a rectangle: positive inside on both axes.
                    float insideX = scleraR - Mathf.Abs(x - center.x);
                    float insideY = lidHalfHeight - Mathf.Abs(y - center.y);
                    rgb = Color.black;
                    alpha = EdgeAlpha(Mathf.Min(insideX, insideY));
                }
                pixels[y * size + x] = new Color(rgb.r, rgb.g, rgb.b, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// Draws a 90-degree wedge, rotated to start at <paramref name="startAngleDeg"/>, inside
    /// an outlined circle. Used as a cheap, clearly-readable "frame number" stand-in for
    /// sequence flipbook frames — each frame is a different rotation of the same shape.
    /// Everything outside the circle is fully transparent.
    /// </summary>
    private static Texture2D MakeWedgeTexture(Color wedgeColour, float startAngleDeg, int size = 128)
    {
        var bg = new Color(0.9f, 0.9f, 0.88f);
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size * 0.45f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 rel = new Vector2(x - center.x, y - center.y);
                float d = rel.magnitude;
                Color rgb;
                if (d > radius - 4f) rgb = Color.black; // outline ring, also the feathered edge
                else
                {
                    float ang = Mathf.Atan2(rel.y, rel.x) * Mathf.Rad2Deg;
                    if (ang < 0f) ang += 360f;
                    float diff = Mathf.Repeat(ang - startAngleDeg, 360f);
                    rgb = diff < 90f ? wedgeColour : bg;
                }
                float alpha = EdgeAlpha(radius - d);
                pixels[y * size + x] = new Color(rgb.r, rgb.g, rgb.b, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// Writes a Texture2D to disk as PNG and returns the imported asset. Idempotent: if the
    /// asset already exists at <paramref name="path"/>, that asset is returned unchanged.
    /// Sets importer alpha settings so a transparent PNG background actually renders
    /// transparent — a correct PNG with the default importer settings still renders opaque.
    /// </summary>
    private static Texture2D SaveTexture(string path, Texture2D tex)
    {
        Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null) return existing;

        File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path);

        if (AssetImporter.GetAtPath(path) is TextureImporter importer)
        {
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            // These textures are tiny (128x128) and used for exactly two states each, so
            // there is no size reason to compress them. Compression is also what silently
            // swallows alpha: Unity is free to pick DXT1/BC1 (no alpha channel at all) for
            // an RGBA source depending on platform defaults. Uncompressed RGBA32 removes
            // that failure mode entirely rather than hoping the picked format has alpha.
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    /// <summary>
    /// Creates (or reuses) a decal Material with its base map set. Idempotent.
    /// </summary>
    private static Material MakeMaterial(Shader decalShader, string path, Texture2D baseMap)
    {
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        var mat = new Material(decalShader);
        mat.SetTexture(DecalBaseMapProperty, baseMap);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    // ---- Audio ---------------------------------------------------------------------

    /// <summary>
    /// Writes a short sine tone with a linear fade-out as a 16-bit mono WAV. Unity can
    /// import a WAV directly, so no AudioClip asset serialisation is needed.
    /// </summary>
    private static void WriteTone(string path, float frequency, float seconds)
    {
        if (File.Exists(path)) return;

        const int sampleRate = 44100;
        int sampleCount = Mathf.RoundToInt(sampleRate * seconds);
        short[] samples = new short[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            // Fade to silence so the tone does not click when it ends.
            float envelope = 1f - (float)i / sampleCount;
            float value = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.35f;
            samples[i] = (short)(value * short.MaxValue);
        }

        using (var stream = new FileStream(path, FileMode.Create))
        using (var w = new BinaryWriter(stream))
        {
            int dataBytes = samples.Length * 2;

            w.Write(new[] { 'R', 'I', 'F', 'F' });
            w.Write(36 + dataBytes);
            w.Write(new[] { 'W', 'A', 'V', 'E' });
            w.Write(new[] { 'f', 'm', 't', ' ' });
            w.Write(16);                       // PCM header size
            w.Write((short)1);                 // PCM format
            w.Write((short)1);                 // mono
            w.Write(sampleRate);
            w.Write(sampleRate * 2);           // byte rate
            w.Write((short)2);                 // block align
            w.Write((short)16);                // bits per sample
            w.Write(new[] { 'd', 'a', 't', 'a' });
            w.Write(dataBytes);
            foreach (short s in samples) w.Write(s);
        }

        Debug.Log($"EGTKGEN wrote {path}");
    }

    // ---- Animator ------------------------------------------------------------------

    /// <summary>
    /// A controller with one bool and one trigger, so ActionTriggerAnimatorParameter and
    /// ActionPlayCharacterEmoteAnimation have something real to drive.
    /// </summary>
    private static void BuildAnimator()
    {
        string controllerPath = $"{Root}/ExampleAnimator.controller";
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null) return;

        AnimationClip idle = MakeScaleClip($"{Root}/Idle.anim", 1f, 1f, loop: true);
        AnimationClip spin = MakeRotateClip($"{Root}/Spin.anim");
        AnimationClip emote = MakeScaleClip($"{Root}/Emote.anim", 1f, 1.6f, loop: false);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.AddParameter("Spin", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Emote", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState idleState = machine.AddState("Idle");
        idleState.motion = idle;
        AnimatorState spinState = machine.AddState("Spin");
        spinState.motion = spin;
        AnimatorState emoteState = machine.AddState("Emote");
        emoteState.motion = emote;
        machine.defaultState = idleState;

        AnimatorStateTransition toSpin = idleState.AddTransition(spinState);
        toSpin.AddCondition(AnimatorConditionMode.If, 0f, "Spin");
        toSpin.duration = 0f;

        AnimatorStateTransition fromSpin = spinState.AddTransition(idleState);
        fromSpin.AddCondition(AnimatorConditionMode.IfNot, 0f, "Spin");
        fromSpin.duration = 0f;

        AnimatorStateTransition toEmote = machine.AddAnyStateTransition(emoteState);
        toEmote.AddCondition(AnimatorConditionMode.If, 0f, "Emote");
        toEmote.duration = 0f;
        toEmote.canTransitionToSelf = false;

        AnimatorStateTransition emoteBack = emoteState.AddTransition(idleState);
        emoteBack.hasExitTime = true;
        emoteBack.exitTime = 1f;
        emoteBack.duration = 0f;

        Debug.Log("EGTKGEN built " + controllerPath);
    }

    private static AnimationClip MakeScaleClip(string path, float from, float to, bool loop)
    {
        AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (existing != null) return existing;

        var clip = new AnimationClip { frameRate = 30f };
        foreach (string axis in new[] { "m_LocalScale.x", "m_LocalScale.y", "m_LocalScale.z" })
        {
            var curve = new AnimationCurve(
                new Keyframe(0f, from),
                new Keyframe(0.25f, to),
                new Keyframe(0.5f, from));
            clip.SetCurve("", typeof(Transform), axis, curve);
        }

        if (loop)
        {
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }

        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static AnimationClip MakeRotateClip(string path)
    {
        AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (existing != null) return existing;

        var clip = new AnimationClip { frameRate = 30f };
        clip.SetCurve("", typeof(Transform), "localEulerAnglesRaw.z",
            new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 360f)));

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }
}
