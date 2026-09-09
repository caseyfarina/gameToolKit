using UnityEngine;

/// <summary>
/// A player controller that can be moved directly to a position.
///
/// Teleporters and checkpoints use this so they work with any controller without naming
/// a specific one. Implementing it means a new controller works with the existing
/// teleport and respawn components with no changes to those components.
/// </summary>
public interface ITeleportableCharacter
{
    /// <summary>
    /// Moves the character to a world position, bypassing normal movement and
    /// cancelling any momentum.
    /// </summary>
    void TeleportTo(Vector3 position);
}
