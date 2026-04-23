using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using Unity.Collections;

/// <summary>
/// Quantizes Animator bone pose updates to a target framerate for a stop
/// motion / "on-twos" aesthetic while the scene continues to render at
/// full framerate. The Animator state machine (transitions, parameters,
/// events, IK) still ticks every frame — only the committed pose is held.
///
/// EGTK usage: place this on the same GameObject as the Animator, which
/// should be a CHILD of the GameObject that carries the CharacterController.
/// This keeps smooth character translation separate from quantized pose.
/// </summary>
[RequireComponent(typeof(Animator))]
public class StopMotionPostProcess : MonoBehaviour
{
    [Tooltip("Target pose update rate. 12 = classic 'on twos', 8 = Rankin/Bass, " +
             "15 = smoother stop motion, 24 = effectively off.")]
    [Range(1, 30)] public int targetFPS = 12;

    PlayableGraph _graph;
    AnimationScriptPlayable _scriptPlayable;
    StopMotionJob _job;
    float _accumulator;

    void OnEnable()
    {
        var animator   = GetComponent<Animator>();
        var controller = animator.runtimeAnimatorController;

        if (controller == null)
        {
            Debug.LogWarning("StopMotionPostProcess: No RuntimeAnimatorController found.", this);
            return;
        }

        // EGTK convention check: the Animator should live on a child of the
        // GameObject doing movement, not on the CharacterController itself.
        if (GetComponent<CharacterController>() != null)
        {
            Debug.LogWarning(
                "StopMotionPostProcess: Animator is on the same GameObject as a " +
                "CharacterController. Place the Animator on a child GameObject so " +
                "movement (smooth) and pose (quantized) are separated.", this);
        }

        var bones = animator.GetComponentsInChildren<Transform>();

        _job = new StopMotionJob
        {
            handles         = new NativeArray<TransformStreamHandle>(bones.Length, Allocator.Persistent),
            cachedPositions = new NativeArray<Vector3>(bones.Length, Allocator.Persistent),
            cachedRotations = new NativeArray<Quaternion>(bones.Length, Allocator.Persistent),
            cachedScales    = new NativeArray<Vector3>(bones.Length, Allocator.Persistent),
            shouldUpdate    = true   // first frame populates the cache
        };

        for (int i = 0; i < bones.Length; i++)
            _job.handles[i] = animator.BindStreamTransform(bones[i]);

        _graph = PlayableGraph.Create("StopMotion_" + name);
        _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        var output             = AnimationPlayableOutput.Create(_graph, "Output", animator);
        var controllerPlayable = AnimatorControllerPlayable.Create(_graph, controller);
        _scriptPlayable        = AnimationScriptPlayable.Create(_graph, _job);

        _scriptPlayable.AddInput(controllerPlayable, 0, 1f);
        output.SetSourcePlayable(_scriptPlayable);

        _graph.Play();
    }

    void Update()
    {
        float frameDuration = 1f / targetFPS;

        // Clamp the accumulator so changing targetFPS at runtime (high -> low
        // or vice versa) cannot cause a burst of back-to-back tick frames
        // while the accumulator drains.
        _accumulator = Mathf.Min(_accumulator + Time.deltaTime, frameDuration);

        var job = _scriptPlayable.GetJobData<StopMotionJob>();
        job.shouldUpdate = _accumulator >= frameDuration;
        if (job.shouldUpdate) _accumulator -= frameDuration;
        _scriptPlayable.SetJobData(job);
    }

    void OnDisable()
    {
        if (_graph.IsValid())
            _graph.Destroy();

        // Guard each NativeArray individually — OnEnable may have bailed
        // early (no controller) before allocating them, and we don't want
        // OnDisable to throw in that case.
        if (_job.handles.IsCreated)         _job.handles.Dispose();
        if (_job.cachedPositions.IsCreated) _job.cachedPositions.Dispose();
        if (_job.cachedRotations.IsCreated) _job.cachedRotations.Dispose();
        if (_job.cachedScales.IsCreated)    _job.cachedScales.Dispose();
    }
}
