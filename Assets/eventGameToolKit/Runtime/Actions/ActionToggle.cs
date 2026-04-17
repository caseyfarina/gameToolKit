using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Toggles the active state of a list of GameObjects independently.
/// Each object is flipped based on its own current state when Toggle() is called —
/// objects that are active become inactive, and vice versa.
/// Use AllOn() or AllOff() to force a known state regardless of current state.
/// Common use: Showing/hiding groups of objects, toggling hazards, switching between UI panels.
/// </summary>
[HelpURL("https://caseyfarina.github.io/egtk-docs/")]
public class ActionToggle : MonoBehaviour
{
    [Tooltip("GameObjects whose active state will be toggled. Each object flips independently based on its own current state.")]
    [SerializeField] private List<GameObject> targets = new List<GameObject>();

    /// <summary>
    /// Flips the active state of each GameObject in the list independently.
    /// Objects that are currently active become inactive, and inactive objects become active.
    /// </summary>
    public void Toggle()
    {
        foreach (var go in targets)
        {
            if (go == null) continue;
            go.SetActive(!go.activeSelf);
        }
    }

    /// <summary>
    /// Sets all GameObjects in the list to active.
    /// </summary>
    public void AllOn()
    {
        foreach (var go in targets)
        {
            if (go == null) continue;
            go.SetActive(true);
        }
    }

    /// <summary>
    /// Sets all GameObjects in the list to inactive.
    /// </summary>
    public void AllOff()
    {
        foreach (var go in targets)
        {
            if (go == null) continue;
            go.SetActive(false);
        }
    }
}
