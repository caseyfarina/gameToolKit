using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Triggers animator parameters (Bool, Int, Float, or Trigger) via UnityEvents or button presses.
/// Common use: Play emote animations on button press, trigger special moves, or activate animations from other scripts.
/// </summary>
public class ActionTriggerAnimation : MonoBehaviour
{
    [Header("Animator Setup")]
    [Tooltip("The Animator component to control (e.g., character's animator)")]
    [SerializeField] private Animator targetAnimator;

    [Tooltip("Name of the animator parameter to trigger (will be hashed for performance)")]
    [SerializeField] private string parameterName = "";

    [Header("Parameter Type")]
    [Tooltip("Type of animator parameter to set")]
    [SerializeField] private AnimatorParameterType parameterType = AnimatorParameterType.Trigger;

    [Header("Parameter Values (for Bool, Int, Float)")]
    [Tooltip("Value to set for Bool parameters")]
    [SerializeField] private bool boolValue = true;

    [Tooltip("Value to set for Int parameters")]
    [SerializeField] private int intValue = 1;

    [Tooltip("Value to set for Float parameters")]
    [SerializeField] private float floatValue = 1f;

    [Header("Optional: Reset After Duration")]
    [Tooltip("For Bool/Int/Float: Automatically reset to default after this many seconds (0 = no auto-reset)")]
    [SerializeField] private float resetAfterSeconds = 0f;

    [Tooltip("Default Bool value to reset to")]
    [SerializeField] private bool defaultBoolValue = false;

    [Tooltip("Default Int value to reset to")]
    [SerializeField] private int defaultIntValue = 0;

    [Tooltip("Default Float value to reset to")]
    [SerializeField] private float defaultFloatValue = 0f;

    [Header("Events")]
    /// <summary>
    /// Fires when the animation parameter is triggered
    /// </summary>
    public UnityEvent onAnimationTriggered;

    /// <summary>
    /// Fires when the parameter is reset to default (if using auto-reset)
    /// </summary>
    public UnityEvent onAnimationReset;

    // Cached parameter hash for performance (StringToHash optimization)
    private int _parameterHash;

    private void Start()
    {
        // Cache the parameter hash once at start for better performance
        if (!string.IsNullOrEmpty(parameterName))
        {
            _parameterHash = Animator.StringToHash(parameterName);
        }
        else
        {
            Debug.LogWarning($"ActionTriggerAnimation on {gameObject.name}: Parameter Name is empty!", this);
        }

        // Validate animator
        if (targetAnimator == null)
        {
            Debug.LogWarning($"ActionTriggerAnimation on {gameObject.name}: Target Animator is not assigned!", this);
        }
    }

    /// <summary>
    /// Triggers the animator parameter. Call this from UnityEvents or other scripts.
    /// </summary>
    public void TriggerAnimation()
    {
        if (targetAnimator == null)
        {
            Debug.LogWarning($"ActionTriggerAnimation on {gameObject.name}: Cannot trigger - no animator assigned!", this);
            return;
        }

        if (string.IsNullOrEmpty(parameterName))
        {
            Debug.LogWarning($"ActionTriggerAnimation on {gameObject.name}: Cannot trigger - parameter name is empty!", this);
            return;
        }

        // Check if parameter exists in animator
        if (!HasParameter(_parameterHash))
        {
            Debug.LogWarning($"ActionTriggerAnimation on {gameObject.name}: Animator does not have parameter '{parameterName}'!", this);
            return;
        }

        // Set the appropriate parameter type
        switch (parameterType)
        {
            case AnimatorParameterType.Trigger:
                targetAnimator.SetTrigger(_parameterHash);
                break;

            case AnimatorParameterType.Bool:
                targetAnimator.SetBool(_parameterHash, boolValue);
                break;

            case AnimatorParameterType.Int:
                targetAnimator.SetInteger(_parameterHash, intValue);
                break;

            case AnimatorParameterType.Float:
                targetAnimator.SetFloat(_parameterHash, floatValue);
                break;
        }

        // Fire event
        onAnimationTriggered?.Invoke();

        // Schedule reset if enabled
        if (resetAfterSeconds > 0f && parameterType != AnimatorParameterType.Trigger)
        {
            Invoke(nameof(ResetParameter), resetAfterSeconds);
        }
    }

    /// <summary>
    /// Resets the parameter to its default value
    /// </summary>
    public void ResetParameter()
    {
        if (targetAnimator == null || !HasParameter(_parameterHash))
            return;

        switch (parameterType)
        {
            case AnimatorParameterType.Bool:
                targetAnimator.SetBool(_parameterHash, defaultBoolValue);
                break;

            case AnimatorParameterType.Int:
                targetAnimator.SetInteger(_parameterHash, defaultIntValue);
                break;

            case AnimatorParameterType.Float:
                targetAnimator.SetFloat(_parameterHash, defaultFloatValue);
                break;
        }

        // Fire reset event
        onAnimationReset?.Invoke();
    }

    /// <summary>
    /// Checks if the animator has the specified parameter
    /// </summary>
    private bool HasParameter(int paramHash)
    {
        if (targetAnimator == null) return false;

        foreach (AnimatorControllerParameter param in targetAnimator.parameters)
        {
            if (param.nameHash == paramHash)
                return true;
        }
        return false;
    }
}

/// <summary>
/// Types of animator parameters that can be triggered
/// </summary>
public enum AnimatorParameterType
{
    Trigger,    // One-shot trigger (most common for emotes)
    Bool,       // True/False toggle
    Int,        // Integer value
    Float       // Float value
}
