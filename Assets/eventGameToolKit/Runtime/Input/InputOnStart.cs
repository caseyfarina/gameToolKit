using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// Fires UnityEvents at scene initialization (Awake and/or Start), with an optional delay for Start.
/// Use this as an event source to trigger any action when the scene begins — no code required.
/// Common use: opening cutscenes, intro dialogue, initial UI reveal, scene-start sound effects.
/// </summary>
public class InputOnStart : MonoBehaviour
{
    [Header("Awake Event")]
    [Tooltip("Fires in Awake() — before any Start() calls in the scene. Use for early initialization that other objects may depend on.")]
    public UnityEvent onAwake;

    [Header("Start Event")]
    [Tooltip("Fires in Start() — after all Awake() calls. Use for actions that depend on other objects being fully initialized.")]
    public UnityEvent onStart;

    [Tooltip("Optional delay in seconds before firing the Start event. Useful for intro pauses, fade-ins, or timed reveals. Set to 0 for immediate.")]
    [Min(0f)]
    [SerializeField] private float startDelay = 0f;

    private void Awake()
    {
        onAwake?.Invoke();
    }

    private void Start()
    {
        if (startDelay > 0f)
            StartCoroutine(FireStartDelayed());
        else
            onStart?.Invoke();
    }

    private IEnumerator FireStartDelayed()
    {
        yield return new WaitForSeconds(startDelay);
        onStart?.Invoke();
    }
}
