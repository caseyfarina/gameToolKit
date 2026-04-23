using UnityEngine;
using UnityEngine.Animations;
//using Unity.Burst;
using Unity.Collections;

//[BurstCompile]
public struct StopMotionJob : IAnimationJob
{
    public NativeArray<TransformStreamHandle> handles;
    public NativeArray<Vector3> cachedPositions;
    public NativeArray<Quaternion> cachedRotations;
    public NativeArray<Vector3> cachedScales;
    public bool shouldUpdate;

    // Root motion is intentionally not handled here.
    // EGTK characters use a parent GameObject with a CharacterController
    // for translation, and the Animator only drives local bone poses.
    public void ProcessRootMotion(AnimationStream stream) { }

    public void ProcessAnimation(AnimationStream stream)
    {
        if (shouldUpdate)
        {
            // Tick frame: read the state machine's freshly-evaluated pose
            // and cache it for replay on subsequent hold frames.
            for (int i = 0; i < handles.Length; i++)
            {
                cachedPositions[i] = handles[i].GetLocalPosition(stream);
                cachedRotations[i] = handles[i].GetLocalRotation(stream);
                cachedScales[i]    = handles[i].GetLocalScale(stream);
            }
        }
        else
        {
            // Hold frame: overwrite the state machine's output with the
            // cached pose so the rig appears frozen until the next tick.
            for (int i = 0; i < handles.Length; i++)
            {
                handles[i].SetLocalPosition(stream, cachedPositions[i]);
                handles[i].SetLocalRotation(stream, cachedRotations[i]);
                handles[i].SetLocalScale(stream, cachedScales[i]);
            }
        }
    }
}
