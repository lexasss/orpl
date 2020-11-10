using UnityEngine;

public class GazeIOControls : MonoBehaviour
{
    // internal members

    GazeClient _gazeClient;

    // overrides

    void Start()
    {
        _gazeClient = FindObjectOfType<GazeClient>();
    }

    // public methods

    public void Options()
    {
        _gazeClient.ShowOptions();
    }

    public void Calibrate()
    {
        _gazeClient.Calibrate();
    }

    public void ToggleTracking()
    {
        _gazeClient.ToggleTracking();
    }
}
