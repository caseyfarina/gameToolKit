using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using TMPro;


/// <summary>
/// Counts down each time a key is pressed, triggering an event when the countdown reaches zero.
/// Common use: Multi-press unlock sequences, button mashing challenges, or limited-use abilities.
/// </summary>
[HelpURL("https://caseyfarina.github.io/egtk-docs/")]
public class InputKeyCountdown : MonoBehaviour
{

    [Tooltip("What counts down. Press + then Listen, then the key or button you want.")]
    [SerializeField] private InputAction activation = new InputAction("Activation", InputActionType.Button);
    public int countDownValue = 10;
    /// <summary>
    /// Fires each time the key is pressed while countdown value is above zero
    /// </summary>
    public UnityEvent onCountDownKey;
    /// <summary>
    /// Fires when the countdown reaches zero after the final key press
    /// </summary>
    public UnityEvent onCountLimitKey;
    private int originalCountDownValue;

    [SerializeField]
    private TextMeshProUGUI countDownnNumberText; // Reference to the TextMeshPro text field


    // Gives a newly added component a working default binding.
    private void Reset()
    {
        activation = new InputAction("Activation", InputActionType.Button);
        activation.AddBinding("<Keyboard>/space");
    }

    private void OnEnable()
    {
        activation.performed += OnActivated;
        activation.Enable();
    }

    private void OnDisable()
    {
        activation.performed -= OnActivated;
        activation.Disable();
    }

    private void OnActivated(InputAction.CallbackContext context)
    {
        if (countDownValue <= 0) return;

        onCountDownKey?.Invoke();
        countDownValue--;
        UpdateCountdownNumberText();

        if (countDownValue == 0)
        {
            onCountLimitKey?.Invoke();
        }
    }

    private void UpdateCountdownNumberText()
    {
        if (countDownnNumberText != null)
        {
            countDownnNumberText.text = countDownValue.ToString(); // Convert int to string
        }
    }
}
