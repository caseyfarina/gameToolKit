using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Plays character emote animations when pressing buttons. Students map keyboard/gamepad inputs to animator trigger parameters.
/// Uses InputActionReference for easy dropdown selection of actions. Only supports Trigger parameters (for one-shot emote animations).
/// </summary>
public class ActionPlayCharacterEmoteAnimation : MonoBehaviour
{
    [Header("Animator Setup")]
    [Tooltip("The character's Animator component (usually on a child GameObject)")]
    [SerializeField] private Animator characterAnimator;

    [Header("Emote Mappings")]
    [Tooltip("Add emote mappings: each input action triggers a specific animator trigger parameter")]
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
            if (!string.IsNullOrEmpty(mapping.animatorTriggerName))
            {
                _parameterHashes[mapping.animatorTriggerName] = Animator.StringToHash(mapping.animatorTriggerName);
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

        if (string.IsNullOrEmpty(mapping.animatorTriggerName))
        {
            Debug.LogWarning($"ActionPlayCharacterEmoteAnimation on {gameObject.name}: Animator trigger name is empty in mapping!", this);
            return;
        }

        // Get cached parameter hash
        if (!_parameterHashes.TryGetValue(mapping.animatorTriggerName, out int paramHash))
        {
            Debug.LogWarning($"ActionPlayCharacterEmoteAnimation on {gameObject.name}: Trigger '{mapping.animatorTriggerName}' not found in cache!", this);
            return;
        }

        // Check if parameter exists in animator
        if (!HasParameter(paramHash))
        {
            Debug.LogWarning($"ActionPlayCharacterEmoteAnimation on {gameObject.name}: Animator does not have trigger parameter '{mapping.animatorTriggerName}'!", this);
            return;
        }

        // Trigger the emote animation
        characterAnimator.SetTrigger(paramHash);

        // Fire event
        onEmoteTriggered?.Invoke();
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

            if (string.IsNullOrEmpty(mapping.animatorTriggerName))
            {
                Debug.LogWarning($"ActionPlayCharacterEmoteAnimation on {gameObject.name}: Mapping {i} has no Animator Trigger Name!", this);
            }
        }
    }
}

/// <summary>
/// Defines a mapping between an Input System action and an Animator trigger parameter.
/// Simple: Press button → Trigger emote animation
/// </summary>
[System.Serializable]
public class EmoteMapping
{
    [Header("Input Action")]
    [Tooltip("Select the input action from your Input Actions asset (appears as dropdown)")]
    public InputActionReference actionReference;

    [Header("Animator Trigger")]
    [Tooltip("Name of the animator TRIGGER parameter to activate (must match exactly)")]
    public string animatorTriggerName = "";
}
