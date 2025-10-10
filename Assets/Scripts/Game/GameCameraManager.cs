using UnityEngine;
using UnityEngine.Events;
using Unity.Cinemachine;

/// <summary>
/// Switches between Cinemachine virtual cameras by name, ensuring only one is active at a time.
/// Common use: Cutscene transitions, gameplay camera angles, boss fight cameras, or cinematic sequences.
/// </summary>
public class GameCameraManager : MonoBehaviour
{
    [System.Serializable]
    public class CameraSetup
    {
        [Tooltip("Descriptive name for the camera - NO SPACES (e.g., 'Player', 'Victory', 'BossCamera')")]
        public string cameraName;

        [Tooltip("GameObject with CinemachineCamera component")]
        public GameObject cameraGameObject;
    }

    [Header("Camera Configuration")]
    [Tooltip("NOTE: Use camera names without spaces (e.g., 'Player', 'Victory', 'BossCamera')")]
    [SerializeField] private CameraSetup[] cameras;
    [SerializeField] private string defaultCameraName = "Player";

    [Header("Events")]
    /// <summary>
    /// Fires when the active camera is switched to a different camera
    /// </summary>
    public UnityEvent onCameraChanged;

    private CinemachineCamera currentCamera;

    public string CurrentCameraName { get; private set; }
    public CinemachineCamera CurrentCamera => currentCamera;

    private void Start()
    {
        // Disable all cameras first
        DisableAllCameras();

        // Enable default camera
        if (!string.IsNullOrEmpty(defaultCameraName))
        {
            EnableCamera(defaultCameraName);
        }
        else if (cameras.Length > 0)
        {
            EnableCamera(cameras[0].cameraName);
        }
    }

    /// <summary>
    /// Enable a camera by name while disabling all others
    /// </summary>
    public void EnableCamera(string cameraName)
    {
        if (string.IsNullOrEmpty(cameraName))
        {
            Debug.LogWarning("Camera name is empty!");
            return;
        }

        CameraSetup targetCamera = FindCameraByName(cameraName);
        if (targetCamera == null)
        {
            Debug.LogWarning($"Camera '{cameraName}' not found! Available cameras: {GetAvailableCameraNames()}");
            return;
        }

        // Disable all cameras first
        DisableAllCameras();

        // Enable the target camera
        targetCamera.cameraGameObject.SetActive(true);
        currentCamera = targetCamera.cameraGameObject.GetComponent<CinemachineCamera>();
        CurrentCameraName = cameraName;

        Debug.Log($"Switched to camera: {cameraName}");
        onCameraChanged.Invoke();
    }

    /// <summary>
    /// Disable all cameras
    /// </summary>
    public void DisableAllCameras()
    {
        foreach (var cameraSetup in cameras)
        {
            if (cameraSetup.cameraGameObject != null)
            {
                cameraSetup.cameraGameObject.SetActive(false);
            }
        }
        currentCamera = null;
        CurrentCameraName = "";
    }

    private CameraSetup FindCameraByName(string cameraName)
    {
        foreach (var cameraSetup in cameras)
        {
            if (cameraSetup.cameraName.Equals(cameraName, System.StringComparison.OrdinalIgnoreCase))
            {
                return cameraSetup;
            }
        }
        return null;
    }

    private string GetAvailableCameraNames()
    {
        if (cameras.Length == 0) return "None";

        string[] names = new string[cameras.Length];
        for (int i = 0; i < cameras.Length; i++)
        {
            names[i] = cameras[i].cameraName;
        }
        return string.Join(", ", names);
    }

    #region Student Helper Methods

    /// <summary>
    /// Check if a camera with the given name exists
    /// </summary>
    public bool HasCamera(string cameraName)
    {
        return FindCameraByName(cameraName) != null;
    }

    /// <summary>
    /// Get all available camera names
    /// </summary>
    public string[] GetCameraNames()
    {
        string[] names = new string[cameras.Length];
        for (int i = 0; i < cameras.Length; i++)
        {
            names[i] = cameras[i].cameraName;
        }
        return names;
    }

    #endregion

    #region Debug Methods

    [Header("Debug Tools")]
    [SerializeField] private bool showDebugInfo = false;

    private void OnGUI()
    {
        if (showDebugInfo && cameras.Length > 0)
        {
            GUILayout.BeginArea(new Rect(10, 200, 250, 200));
            GUILayout.Label($"Current Camera: {CurrentCameraName}");

            GUILayout.Space(10);
            GUILayout.Label("Switch to:");

            foreach (var cameraSetup in cameras)
            {
                if (GUILayout.Button(cameraSetup.cameraName))
                {
                    EnableCamera(cameraSetup.cameraName);
                }
            }

            GUILayout.EndArea();
        }
    }

    #endregion
}