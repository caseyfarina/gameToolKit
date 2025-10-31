using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Automatically plays character emote animations in response to Input System actions.
/// Students can map keyboard/gamepad inputs to animator parameters without writing code.
/// Uses InputActionReference for easy action selection from dropdown menus in the Inspector.
/// </summary>
public class ActionPlayCharacterEmoteAnimation : MonoBehaviour
{
    [Header("Animator Setup")]
    [Tooltip("The character's Animator component (usually on a child GameObject)")]
    [SerializeField] private Animator characterAnimator;

    [Header("Emote Mappings")]
    [Tooltip("Add emote mappings: each input action triggers a specific animator parameter")]
    [SerializeField] private List<EmoteMapping> emoteMappings = new List<EmoteMapping>();

    [Header("Events")]
    /// <summary>
    /// Fires when any emote animation is triggered
    /// </summary>
    public UnityEvent onEmoteTriggered;

    // Cached parameter hashes for performance
    private Dictionary<string, int> _parameterHashes = new Dictionary<string, int>();

    private void Start()
    {
        // Validate animator
        if (characterAnimator == null)
        {
            Debug.LogWarning($"ActionPlayCharacterEmoteAnimation on {gameObject.name}: Character Animator is not assigned!", this);
            return;
        }

        // Cache all parameter hashes for performance
        foreach (var mapping in emoteMappings)
        {
            if (!string.IsNullOrEmpty(mapping.animatorParameterName))
            {
                _parameterHashes[mapping.animatorParameterName] = Animator.StringToHash(mapping.animatorParameterName);
            }
        }

        // Validate all mappings
        ValidateMappings();
    }

    private void OnEnable()
    {
        // Subscribe to all input actions
        foreach (var mapping in emoteMappings)
        {
            if (mapping.actionReference != null && mapping.actionReference.action != null)
            {
                mapping.actionReference.action.Enable();
                mapping.actionReference.action.performed += ctx => TriggerEmote(mapping);
            }
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from all input actions
        foreach (var mapping in emoteMappings)
        {
            if (mapping.actionReference != null && mapping.actionReference.action != null)
            {
                mapping.actionReference.action.performed -= ctx => TriggerEmote(mapping);
                mapping.actionReference.action.Disable();
            }
        }
    }

    /// <summary>
    /// Triggers an emote animation based on the mapping
    /// </summary>
    private void TriggerEmote(EmoteMapping mapping)
    {
        if (characterAnimator == null)
            return;

        if (string.IsNullOrEmpty(mapping.animatorParameterName))
        {
            Debug.LogWarning($"ActionPlayCharacterEmoteAnimation on {gameObject.name}: Animator parameter name is empty in mapping!", this);
            return;
        }

        // Get cached parameter hash
        if (!_parameterHashes.TryGetValue(mapping.animatorParameterName, out int paramHash))
        {
            Debug.LogWarning($"ActionPlayCharacterEmoteAnimation on {gameObject.name}: Parameter '{mapping.animatorParameterName}' not found in cache!", this);
            return;
        }

        // Check if parameter exists in animator
        if (!HasParameter(paramHash))
        {
            Debug.LogWarning($"ActionPlayCharacterEmoteAnimation on {gameObject.name}: Animator does not have parameter '{mapping.animatorParameterName}'!", this);
            return;
        }

        // Set the appropriate parameter type
        switch (mapping.parameterType)
        {
            case AnimatorParameterType.Trigger:
                characterAnimator.SetTrigger(paramHash);
                break;

            case AnimatorParameterType.Bool:
                characterAnimator.SetBool(paramHash, mapping.boolValue);
                break;

            case AnimatorParameterType.Int:
                characterAnimator.SetInteger(paramHash, mapping.intValue);
                break;

            case AnimatorParameterType.Float:
                characterAnimator.SetFloat(paramHash, mapping.floatValue);
                break;
        }

        // Fire event
        onEmoteTriggered?.Invoke();

        // Schedule reset if enabled
        if (mapping.resetAfterSeconds > 0f && mapping.parameterType != AnimatorParameterType.Trigger)
        {
            StartCoroutine(ResetParameterAfterDelay(mapping, paramHash));
        }
    }

    /// <summary>
    /// Resets a parameter to its default value after a delay
    /// </summary>
    private System.Collections.IEnumerator ResetParameterAfterDelay(EmoteMapping mapping, int paramHash)
    {
        yield return new WaitForSeconds(mapping.resetAfterSeconds);

        if (characterAnimator == null || !HasParameter(paramHash))
            yield break;

        switch (mapping.parameterType)
        {
            case AnimatorParameterType.Bool:
                characterAnimator.SetBool(paramHash, mapping.defaultBoolValue);
                break;

            case AnimatorParameterType.Int:
                characterAnimator.SetInteger(paramHash, mapping.defaultIntValue);
                break;

            case AnimatorParameterType.Float:
                characterAnimator.SetFloat(paramHash, mapping.defaultFloatValue);
                break;
        }
    }

    /// <summary>
    /// Checks if the animator has the specified parameter
    /// </summary>
    private bool HasParameter(int paramHash)
    {
        if (characterAnimator == null) return false;

        foreach (AnimatorControllerParameter param in characterAnimator.parameters)
        {
            if (param.nameHash == paramHash)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Validates all mappings and logs warnings for issues
    /// </summary>
    private void ValidateMappings()
    {
        if (emoteMappings.Count == 0)
        {
            Debug.LogWarning($"ActionPlayCharacterEmoteAnimation on {gameObject.name}: No emote mappings configured!", this);
            return;
        }

        for (int i = 0; i < emoteMappings.Count; i++)
        {
            var mapping = emoteMappings[i];

            if (mapping.actionReference == null)
            {
                Debug.LogWarning($"ActionPlayCharacterEmoteAnimation on {gameObject.name}: Mapping {i} has no Input Action assigned!", this);
            }

            if (string.IsNullOrEmpty(mapping.animatorParameterName))
            {
                Debug.LogWarning($"ActionPlayCharacterEmoteAnimation on {gameObject.name}: Mapping {i} has no Animator Parameter Name!", this);
            }
        }
    }
}

/// <summary>
/// Defines a mapping between an Input System action and an Animator parameter
/// </summary>
[System.Serializable]
public class EmoteMapping
{
    [Header("Input Action")]
    [Tooltip("Select the input action from your Input Actions asset (appears as dropdown)")]
    public InputActionReference actionReference;

    [Header("Animator Parameter")]
    [Tooltip("Name of the animator parameter to set (must match exactly)")]
    public string animatorParameterName = "";

    [Tooltip("Type of animator parameter")]
    public AnimatorParameterType parameterType = AnimatorParameterType.Trigger;

    [Header("Parameter Values (for Bool, Int, Float)")]
    [Tooltip("Value to set for Bool parameters")]
    public bool boolValue = true;

    [Tooltip("Value to set for Int parameters")]
    public int intValue = 1;

    [Tooltip("Value to set for Float parameters")]
    public float floatValue = 1f;

    [Header("Optional: Auto-Reset")]
    [Tooltip("For Bool/Int/Float: Automatically reset to default after this many seconds (0 = no auto-reset)")]
    public float resetAfterSeconds = 0f;

    [Tooltip("Default Bool value to reset to")]
    public bool defaultBoolValue = false;

    [Tooltip("Default Int value to reset to")]
    public int defaultIntValue = 0;

    [Tooltip("Default Float value to reset to")]
    public float defaultFloatValue = 0f;
}
