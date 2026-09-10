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

    [MenuItem("EGTK/Generate Example Assets")]
    public static void Generate()
    {
        EnsureFolder();

        WriteTone($"{Root}/Blip.wav", frequency: 880f, seconds: 0.12f);
        WriteTone($"{Root}/Thud.wav", frequency: 160f, seconds: 0.18f);

        AssetDatabase.Refresh();
        BuildAnimator();
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
