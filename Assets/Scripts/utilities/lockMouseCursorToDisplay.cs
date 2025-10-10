using UnityEngine;

/// <summary>
/// Confines the mouse cursor to the game window with toggle controls via Escape and mouse clicks.
/// Common use: First-person games, strategy games requiring cursor confinement, or windowed game applications.
/// </summary>
public class lockMouseCursorToDisplay : MonoBehaviour
{
    [SerializeField] private bool lockOnStart = true;
    [SerializeField] private CursorLockMode lockMode = CursorLockMode.Confined;
    
    void Start()
    {
        if (lockOnStart)
        {
            LockCursor();
        }
    }
    
    void Update()
    {
        // Optional: Allow user to toggle cursor lock with Escape key
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UnlockCursor();
        }
        
        // Optional: Re-lock cursor when clicking back into the game window
        if (Input.GetMouseButtonDown(0) && Cursor.lockState != lockMode)
        {
            LockCursor();
        }
    }
    
    /// <summary>
    /// Locks the cursor to the display window using the configured lock mode
    /// </summary>
    public void LockCursor()
    {
        Cursor.lockState = lockMode;
        Cursor.visible = true; // Keep cursor visible while confined
    }

    /// <summary>
    /// Unlocks the cursor allowing it to leave the display window
    /// </summary>
    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}