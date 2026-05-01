using UnityEngine;

public class applicationFPSLimiting : MonoBehaviour
{

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Application.targetFrameRate = 6;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
