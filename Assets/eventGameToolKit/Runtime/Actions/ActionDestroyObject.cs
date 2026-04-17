using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Destroys one or more GameObjects, with an optional delay.
/// By default destroys the GameObject this component is on.
/// Add objects to Targets to destroy other objects instead (or in addition).
/// Common use: Clicking an object to remove it, destroying enemies on death, removing collectibles.
/// </summary>
[HelpURL("https://caseyfarina.github.io/egtk-docs/")]
public class ActionDestroyObject : MonoBehaviour
{
    [Tooltip("Objects to destroy. If empty, destroys the GameObject this component is on.")]
    [SerializeField] private List<GameObject> targets = new List<GameObject>();

    [Tooltip("Seconds to wait before destroying. 0 = immediate.")]
    [SerializeField] private float delay = 0f;

    [Header("Events")]
    /// <summary>
    /// Fires just before destruction (use for particle effects, sounds, etc.)
    /// </summary>
    public UnityEvent onBeforeDestroy;

    /// <summary>
    /// Destroys the target objects (or this GameObject if no targets are set).
    /// Call this from any UnityEvent.
    /// </summary>
    public void DestroyObject()
    {
        onBeforeDestroy.Invoke();

        if (targets.Count == 0)
        {
            if (delay <= 0f)
                Destroy(gameObject);
            else
                StartCoroutine(DestroyAfterDelay(gameObject, delay));
        }
        else
        {
            foreach (var target in targets)
            {
                if (target == null) continue;
                if (delay <= 0f)
                    Destroy(target);
                else
                    StartCoroutine(DestroyAfterDelay(target, delay));
            }
        }
    }

    private IEnumerator DestroyAfterDelay(GameObject target, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (target != null)
            Destroy(target);
    }
}
