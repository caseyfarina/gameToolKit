using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Teleports the player to a specified Transform destination.
/// Works with CharacterControllerCC or Rigidbody-based players.
/// Includes immunity window to prevent teleport loops in two-way portal systems.
/// Common use: Portal systems, fast travel, puzzle teleporters, level transitions.
/// </summary>
public class ActionTeleportToTransform : MonoBehaviour
{
    // Static dictionary tracks recent teleports per-object to prevent loops
    private static Dictionary<GameObject, float> recentTeleports = new Dictionary<GameObject, float>();
    [Header("Destination")]
    [Tooltip("The Transform to teleport the player to")]
    [SerializeField] private Transform destination;

    [Tooltip("Apply the destination's rotation to the player")]
    [SerializeField] private bool useDestinationRotation = true;

    [Header("Player Detection")]
    [Tooltip("Tag used to find the player if not explicitly assigned")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("Player object to teleport. If null, searches for playerTag.")]
    [SerializeField] private GameObject playerObject;

    [Header("Teleport Settings")]
    [Tooltip("Delay before teleporting (allows effects to play)")]
    [SerializeField] private float teleportDelay = 0f;

    [Tooltip("Reset player velocity after teleporting")]
    [SerializeField] private bool resetVelocity = true;

    [Tooltip("Cooldown between teleports to prevent rapid re-triggering")]
    [SerializeField] private float teleportCooldown = 0.5f;

    [Tooltip("Time after teleporting during which the object cannot be teleported again (prevents loops in two-way portals)")]
    [SerializeField] private float teleportImmunityDuration = 0.5f;

    [Header("Visual Effects")]
    [Tooltip("Prefab to spawn at player's origin position before teleporting")]
    [SerializeField] private GameObject departureEffect;

    [Tooltip("Prefab to spawn at destination when player arrives")]
    [SerializeField] private GameObject arrivalEffect;

    [Tooltip("Duration to keep effect objects alive")]
    [SerializeField] private float effectDuration = 2f;

    [Header("Events")]
    /// <summary>
    /// Fires when the teleport process begins (before delay)
    /// </summary>
    public UnityEvent onTeleportStarted;

    /// <summary>
    /// Fires when the player arrives at the destination
    /// </summary>
    public UnityEvent onTeleportCompleted;

    /// <summary>
    /// Fires when teleport fails (no player or destination found)
    /// </summary>
    public UnityEvent onTeleportFailed;

    /// <summary>
    /// Fires with the destination position when teleport completes
    /// </summary>
    public UnityEvent<Vector3> onTeleportedToPosition;

    private bool isTeleporting = false;
    private float cooldownTimer = 0f;

    /// <summary>
    /// Checks if an object has teleport immunity (recently teleported)
    /// </summary>
    private bool HasTeleportImmunity(GameObject obj)
    {
        if (obj == null) return false;

        // Clean up expired entries while checking
        if (recentTeleports.TryGetValue(obj, out float immuneUntil))
        {
            if (Time.time < immuneUntil)
            {
                return true;
            }
            else
            {
                // Immunity expired, remove entry
                recentTeleports.Remove(obj);
            }
        }

        return false;
    }

    /// <summary>
    /// Grants teleport immunity to an object for the configured duration
    /// </summary>
    private void GrantTeleportImmunity(GameObject obj)
    {
        if (obj == null) return;

        float immuneUntil = Time.time + teleportImmunityDuration;

        if (recentTeleports.ContainsKey(obj))
        {
            recentTeleports[obj] = immuneUntil;
        }
        else
        {
            recentTeleports.Add(obj, immuneUntil);
        }
    }

    private void Start()
    {
        // Find player if not assigned
        if (playerObject == null)
        {
            playerObject = GameObject.FindGameObjectWithTag(playerTag);
        }
    }

    private void Update()
    {
        // Update cooldown timer
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }
    }

    /// <summary>
    /// Teleports the player to the destination Transform (call via UnityEvent)
    /// </summary>
    public void TeleportPlayer()
    {
        if (isTeleporting || cooldownTimer > 0f)
        {
            return;
        }

        if (destination == null)
        {
            Debug.LogWarning("ActionTeleportToTransform: No destination assigned!");
            onTeleportFailed.Invoke();
            return;
        }

        GameObject player = GetPlayerObject();
        if (player == null)
        {
            Debug.LogWarning("ActionTeleportToTransform: No player found!");
            onTeleportFailed.Invoke();
            return;
        }

        // Check immunity to prevent teleport loops
        if (HasTeleportImmunity(player))
        {
            return;
        }

        StartCoroutine(TeleportCoroutine(player));
    }

    /// <summary>
    /// Teleports the player immediately without delay
    /// </summary>
    public void TeleportPlayerImmediate()
    {
        if (isTeleporting || cooldownTimer > 0f)
        {
            return;
        }

        if (destination == null)
        {
            Debug.LogWarning("ActionTeleportToTransform: No destination assigned!");
            onTeleportFailed.Invoke();
            return;
        }

        GameObject player = GetPlayerObject();
        if (player == null)
        {
            Debug.LogWarning("ActionTeleportToTransform: No player found!");
            onTeleportFailed.Invoke();
            return;
        }

        // Check immunity to prevent teleport loops
        if (HasTeleportImmunity(player))
        {
            return;
        }

        StartCoroutine(TeleportCoroutine(player, 0f));
    }

    /// <summary>
    /// Teleports a specific GameObject to the destination
    /// </summary>
    public void TeleportObject(GameObject objectToTeleport)
    {
        if (isTeleporting || cooldownTimer > 0f)
        {
            return;
        }

        if (destination == null)
        {
            Debug.LogWarning("ActionTeleportToTransform: No destination assigned!");
            onTeleportFailed.Invoke();
            return;
        }

        if (objectToTeleport == null)
        {
            Debug.LogWarning("ActionTeleportToTransform: No object to teleport!");
            onTeleportFailed.Invoke();
            return;
        }

        // Check immunity to prevent teleport loops
        if (HasTeleportImmunity(objectToTeleport))
        {
            return;
        }

        StartCoroutine(TeleportCoroutine(objectToTeleport));
    }

    private IEnumerator TeleportCoroutine(GameObject target, float? customDelay = null)
    {
        isTeleporting = true;
        onTeleportStarted.Invoke();

        Vector3 originPosition = target.transform.position;

        // Spawn departure effect
        if (departureEffect != null)
        {
            GameObject effect = Instantiate(departureEffect, originPosition, Quaternion.identity);
            Destroy(effect, effectDuration);
        }

        // Wait for delay
        float delay = customDelay ?? teleportDelay;
        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }

        // Perform teleport
        Vector3 destinationPosition = destination.position;
        Quaternion destinationRotation = useDestinationRotation ? destination.rotation : target.transform.rotation;

        // Try CharacterControllerCC first (preferred method)
        CharacterControllerCC characterCC = target.GetComponent<CharacterControllerCC>();
        if (characterCC != null)
        {
            characterCC.TeleportTo(destinationPosition, destinationRotation);
        }
        else
        {
            // Try CharacterController
            CharacterController characterController = target.GetComponent<CharacterController>();
            if (characterController != null)
            {
                characterController.enabled = false;
                target.transform.position = destinationPosition;
                target.transform.rotation = destinationRotation;
                characterController.enabled = true;
            }
            else
            {
                // Fallback to direct transform manipulation
                target.transform.position = destinationPosition;
                target.transform.rotation = destinationRotation;
            }

            // Handle Rigidbody velocity reset
            if (resetVelocity)
            {
                Rigidbody rb = target.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }

        // Spawn arrival effect
        if (arrivalEffect != null)
        {
            GameObject effect = Instantiate(arrivalEffect, destinationPosition, Quaternion.identity);
            Destroy(effect, effectDuration);
        }

        // Grant immunity to prevent teleport loops in two-way systems
        GrantTeleportImmunity(target);

        Debug.Log($"ActionTeleportToTransform: Teleported {target.name} to {destination.name}");

        onTeleportCompleted.Invoke();
        onTeleportedToPosition.Invoke(destinationPosition);

        cooldownTimer = teleportCooldown;
        isTeleporting = false;
    }

    /// <summary>
    /// Sets the destination Transform at runtime
    /// </summary>
    public void SetDestination(Transform newDestination)
    {
        destination = newDestination;
    }

    /// <summary>
    /// Sets the player object at runtime
    /// </summary>
    public void SetPlayerObject(GameObject newPlayer)
    {
        playerObject = newPlayer;
    }

    /// <summary>
    /// Sets the teleport delay at runtime
    /// </summary>
    public void SetTeleportDelay(float newDelay)
    {
        teleportDelay = Mathf.Max(0f, newDelay);
    }

    /// <summary>
    /// Clears teleport immunity for the player, allowing immediate re-teleportation
    /// </summary>
    public void ClearPlayerImmunity()
    {
        GameObject player = GetPlayerObject();
        if (player != null && recentTeleports.ContainsKey(player))
        {
            recentTeleports.Remove(player);
        }
    }

    /// <summary>
    /// Clears teleport immunity for a specific object
    /// </summary>
    public static void ClearImmunity(GameObject obj)
    {
        if (obj != null && recentTeleports.ContainsKey(obj))
        {
            recentTeleports.Remove(obj);
        }
    }

    /// <summary>
    /// Checks if the player currently has teleport immunity
    /// </summary>
    public bool IsPlayerImmune()
    {
        GameObject player = GetPlayerObject();
        return player != null && HasTeleportImmunity(player);
    }

    private GameObject GetPlayerObject()
    {
        if (playerObject != null)
        {
            return playerObject;
        }

        // Try to find by tag
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            playerObject = player;
        }

        return playerObject;
    }

    /// <summary>
    /// Returns true if a teleport is currently in progress
    /// </summary>
    public bool IsTeleporting => isTeleporting;

    /// <summary>
    /// Returns true if the teleport is on cooldown
    /// </summary>
    public bool IsOnCooldown => cooldownTimer > 0f;

    /// <summary>
    /// Returns the current destination Transform
    /// </summary>
    public Transform Destination => destination;
}
