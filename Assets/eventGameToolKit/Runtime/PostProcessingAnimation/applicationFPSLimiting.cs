using UnityEngine;

/// <summary>
/// Locks the whole application to a low framerate for a stop motion look.
/// Place one of these anywhere in the scene. Everything renders at the
/// chosen rate, so the entire game shares the same choppy cadence.
///
/// Use this for a whole-scene stop motion style. To make only one character
/// look stop motion while the rest of the game stays smooth, use
/// StopMotionPostProcess on that character's Animator instead.
/// </summary>
[HelpURL("https://caseyfarina.github.io/egtk-docs/")]
public class applicationFPSLimiting : MonoBehaviour
{
    [Tooltip("Framerate the whole game runs at. 6 = strong stop motion, " +
             "12 = classic 'on twos', 24 = film-like, 60 = effectively off.")]
    [Range(1, 60)] public int targetFPS = 6;

    void Start()
    {
        Application.targetFrameRate = targetFPS;
    }

    /// <summary>
    /// Changes the game's framerate while playing. Wire this to any UnityEvent
    /// to switch the stop motion look on or off mid-game.
    /// </summary>
    public void SetTargetFPS(int fps)
    {
        targetFPS = Mathf.Clamp(fps, 1, 60);
        Application.targetFrameRate = targetFPS;
    }
}
