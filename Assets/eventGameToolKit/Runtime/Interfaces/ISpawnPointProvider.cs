using UnityEngine;

/// <summary>
/// Interface for any system that can provide a spawn point for players.
/// Implement this on checkpoint managers, spawn point selectors, or any system
/// that needs to control where players appear.
/// 
/// This enables decoupled communication between character controllers and spawn systems.
/// The character controller doesn't need to know about checkpoints specifically - 
/// it just asks "is there a spawn point?" at initialization.
/// </summary>
public interface ISpawnPointProvider
{
    /// <summary>
    /// Returns true if this provider has a valid spawn point available.
    /// </summary>
    bool HasSpawnPoint { get; }
    
    /// <summary>
    /// The world position where the player should spawn.
    /// Only valid if HasSpawnPoint is true.
    /// </summary>
    Vector3 SpawnPosition { get; }
    
    /// <summary>
    /// The world rotation the player should have when spawning.
    /// Only valid if HasSpawnPoint is true.
    /// </summary>
    Quaternion SpawnRotation { get; }
    
    /// <summary>
    /// Called by the player after consuming the spawn point.
    /// Allows the provider to clear one-time spawn data if needed.
    /// </summary>
    void OnSpawnPointUsed();
}
