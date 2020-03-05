using UnityEngine;

public class GazeIOControls : MonoBehaviour
{
    // internal members

    GazeClient _gazeClient;

    bool _blockInput = false;

    // overrides

    void Start()
    {
        _gazeClient = FindObjectOfType<GazeClient>();
    }

    void Update()
    {
        if (_gazeClient.isTracking)
        {
            if (!_blockInput && Input.GetKey(KeyCode.Escape))
            {
                _blockInput = true;
                ToggleTracking();
            }
        }
        else
        {
            _blockInput = false;
        }
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
