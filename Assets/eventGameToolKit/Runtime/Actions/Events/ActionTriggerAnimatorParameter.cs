using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Animator parameter types supported by Unity
/// </summary>
public enum AnimatorParameterType
{
    Trigger,    // One-shot trigger (resets automatically)
    Bool,       // True/false state
    Int,        // Integer value
    Float       // Decimal value
}

/// <summary>
/// Triggers animator parameters via UnityEvents. Perfect for location-based interactions that play different animations.
/// Can set Triggers, Bools, Ints, or Floats on an Animator Controller.
/// Common use: Interact buttons triggering different animations at different locations (open door, pull lever, pick up item).
/// </summary>
public class ActionTriggerAnimatorParameter : MonoBehaviour
{
    [Header("Animator Setup")]
    [Tooltip("The Animator component to control (usually on the player character)")]
    [SerializeField] private Animator targetAnimator;

    [Header("Parameter Settings")]
    [Tooltip("Type of animator parameter to set")]
    [SerializeField] private AnimatorParameterType parameterType = AnimatorParameterType.Trigger;

    [Tooltip("Name of the animator parameter (must match exactly, case-sensitive)")]
    [SerializeField] private string parameterName = "";

    [Header("Bool/Int/Float Values")]
    [Tooltip("Value to set for Bool parameters")]
    [SerializeField] private bool boolValue = true;

    [Tooltip("Value to set for Int parameters")]
    [SerializeField] private int intValue = 0;

    [Tooltip("Value to set for Float parameters")]
    [SerializeField] private float floatValue = 0f;

    [Header("Events")]
    /// <summary>
    /// Fires when the animator parameter is successfully set
    /// </summary>
    public UnityEvent onParameterSet;

    /// <summary>
    /// Fires when the parameter cannot be set (animator missing, parameter doesn't exist, etc.)
    /// </summary>
    public UnityEvent onParameterFailed;

    // Cached parameter hash for performance
    private int _parameterHash;
    private bool _isInitialized = false;

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (_isInitialized) return;

        if (targetAnimator == null)
        {
            Debug.LogWarning($"ActionTriggerAnimatorParameter on {gameObject.name}: Target Animator is not assigned!", this);
            return;
        }

        if (string.IsNullOrEmpty(parameterName))
        {
            Debug.LogWarning($"ActionTriggerAnimatorParameter on {gameObject.name}: Parameter Name is empty!", this);
            return;
        }

        // Cache the parameter hash for performance
        _parameterHash = Animator.StringToHash(parameterName);

        // Validate parameter exists
        if (!HasParameter(_parameterHash))
        {
            Debug.LogWarning($"ActionTriggerAnimatorParameter on {gameObject.name}: Animator does not have parameter '{parameterName}'!", this);
            return;
        }

        _isInitialized = true;
    }

    /// <summary>
    /// Triggers the configured animator parameter (call this from UnityEvents)
    /// </summary>
    public void TriggerParameter()
    {
        if (!_isInitialized)
        {
            Initialize();
        }

        if (!_isInitialized || targetAnimator == null)
        {
            onParameterFailed?.Invoke();
            return;
        }

        // Set the parameter based on type
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

        onParameterSet?.Invoke();
    }

    /// <summary>
    /// Set a trigger parameter (one-shot animation)
    /// </summary>
    public void SetTrigger()
    {
        if (!_isInitialized) Initialize();
        if (targetAnimator == null) return;

        targetAnimator.SetTrigger(_parameterHash);
        onParameterSet?.Invoke();
    }

    /// <summary>
    /// Set a bool parameter to true
    /// </summary>
    public void SetBoolTrue()
    {
        if (!_isInitialized) Initialize();
        if (targetAnimator == null) return;

        targetAnimator.SetBool(_parameterHash, true);
        onParameterSet?.Invoke();
    }

    /// <summary>
    /// Set a bool parameter to false
    /// </summary>
    public void SetBoolFalse()
    {
        if (!_isInitialized) Initialize();
        if (targetAnimator == null) return;

        targetAnimator.SetBool(_parameterHash, false);
        onParameterSet?.Invoke();
    }

    /// <summary>
    /// Set an int parameter to a specific value
    /// </summary>
    public void SetInt(int value)
    {
        if (!_isInitialized) Initialize();
        if (targetAnimator == null) return;

        targetAnimator.SetInteger(_parameterHash, value);
        onParameterSet?.Invoke();
    }

    /// <summary>
    /// Set a float parameter to a specific value
    /// </summary>
    public void SetFloat(float value)
    {
        if (!_isInitialized) Initialize();
        if (targetAnimator == null) return;

        targetAnimator.SetFloat(_parameterHash, value);
        onParameterSet?.Invoke();
    }

    /// <summary>
    /// Update the parameter name and recache (useful for dynamic parameter switching)
    /// </summary>
    public void SetParameterName(string newParameterName)
    {
        parameterName = newParameterName;
        _parameterHash = Animator.StringToHash(parameterName);
        _isInitialized = false;
        Initialize();
    }

    /// <summary>
    /// Update the bool value used by TriggerParameter()
    /// </summary>
    public void SetBoolValue(bool value)
    {
        boolValue = value;
    }

    /// <summary>
    /// Update the int value used by TriggerParameter()
    /// </summary>
    public void SetIntValue(int value)
    {
        intValue = value;
    }

    /// <summary>
    /// Update the float value used by TriggerParameter()
    /// </summary>
    public void SetFloatValue(float value)
    {
        floatValue = value;
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

    // Public properties for read-only access
    public string ParameterName => parameterName;
    public AnimatorParameterType ParameterType => parameterType;
    public bool IsInitialized => _isInitialized;
}
