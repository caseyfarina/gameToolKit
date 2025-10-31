using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Automatically spawns prefabs from a list at randomized intervals.
/// Common use: Enemy spawners, item generators, particle effect triggers, or obstacle creation systems.
/// </summary>
public class ActionAutoSpawner : MonoBehaviour
{
    //this will make a field to add the spaw object
    public GameObject[] spawnThings;

    public float spawnRateMin = 1f;
    public float spawnRateMax = 5f;

    private float time = 0f;
    private float nextSpawnTime = 0f;

    public float spawnPositionRange = 0f;
    
    void Start()
    {
      
        nextSpawnTime = Random.Range(spawnRateMin, spawnRateMax);
    }

    private void Update()
    {
        time += Time.deltaTime;

        if (time >= nextSpawnTime)
        {
            spawnObject();
        }
    }

   

    


    void spawnObject()
    {
        //choose a random spawn from the spawn list
        int spawnNumber = (int)Mathf.Floor(Random.Range(0, spawnThings.Length));
        //this is the code for making a new object at the position of this spawnpoint
        if(spawnPositionRange > 0)
        {
            Instantiate(
                        spawnThings[spawnNumber],
                        transform.position + (Random.insideUnitSphere * spawnPositionRange),
                        transform.rotation);

        }
        else
        {
            Instantiate(
                        spawnThings[spawnNumber],
                        transform.position,
                        transform.rotation);
        }

        

        //Set the next spawnTime
        nextSpawnTime = Random.Range(spawnRateMin, spawnRateMax) + time;
    }

    /// <summary>
    /// Sets the minimum time between spawns in seconds
    /// </summary>
    public void setSpawnTimeMinimum(float _spawnTimeMinimum)
    {
        spawnRateMin = _spawnTimeMinimum;
    }

    /// <summary>
    /// Sets the maximum time between spawns in seconds
    /// </summary>
    public void setSpawnTimeMaximum(float _spawnTimeMaximum)
    {
        spawnRateMin = _spawnTimeMaximum;
    }


}
