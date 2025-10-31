using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns a single prefab instance at this object's position and rotation when triggered.
/// Common use: Manual spawn points, button-triggered creation, reward drops, or projectile firing systems.
/// </summary>
public class ActionSpawnObject : MonoBehaviour
{

    public GameObject prefabToSpawn;

    /// <summary>
    /// Spawns the assigned prefab at this object's position and rotation
    /// </summary>
    public void spawnSinglePrefab()
    {

        if(prefabToSpawn != null)
        {
            Instantiate(prefabToSpawn, transform.position, transform.rotation);
        }


    }
}
