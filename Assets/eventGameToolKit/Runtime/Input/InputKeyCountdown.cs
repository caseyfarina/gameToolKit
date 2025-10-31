using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;


/// <summary>
/// Counts down each time a key is pressed, triggering an event when the countdown reaches zero.
/// Common use: Multi-press unlock sequences, button mashing challenges, or limited-use abilities.
/// </summary>
public class InputKeyCountdown : MonoBehaviour
{

    public KeyCode  thisKey = KeyCode.Space;
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


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(thisKey))
        {
            //countDown the number of clicks
            if (countDownValue > 0)
            {
                //send the event
                onCountDownKey?.Invoke();

                countDownValue = countDownValue - 1;

                UpdateCountdownNumberText();

                if (countDownValue == 0)
                {
                    onCountLimitKey?.Invoke();
                }
            }
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
