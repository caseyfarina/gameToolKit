using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Reloads the current scene, resetting all game state to initial conditions.
/// Common use: Retry buttons after game over, level restart functionality, or debugging/testing tools.
/// </summary>
public class ActionRestartScene : MonoBehaviour
{
    /// <summary>
    /// Restart the current scene - call this from UnityEvents or UI buttons
    /// </summary>
    public void RestartScene()
    {
        Time.timeScale = 1f; // Ensure scene can load properly
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}