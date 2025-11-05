using UnityEngine;

/// <summary>
/// Simple utility component that continuously rotates a GameObject at a specified speed
/// </summary>
public class SimpleRotate : MonoBehaviour
{
    [Tooltip("Rotation speed in degrees per second")]
    public Vector3 rotationSpeed = new Vector3(0, 90, 0);

    void Update()
    {
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }
}
