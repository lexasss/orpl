using UnityEngine;
using UnityEngine.UI;

public class GazePoint : MonoBehaviour
{
    Image _image;
    bool _enabled = false;

    int _correctionY = 0;

    void Start()
    {
        _image = GetComponent<Image>();
        _image.enabled = _enabled;

        var gazeSimulator = FindObjectOfType<GazeSimulator>();

        _correctionY = gazeSimulator.Enabled ? GazeSimulator.TOOLBAR_HEIGHT : 0;
    }

    private void Update()
    {
        bool pIsPressed = Input.GetKeyDown(KeyCode.P);
        if (pIsPressed)
        {
            _enabled = !_enabled;
            _image.enabled = _enabled;
        }
    }

    public void MoveTo(GazeIO.Sample aGazePoint)
    {
        if (_enabled)
        {
            _image.transform.localPosition = new Vector3(aGazePoint.x - Screen.width / 2, Screen.height / 2 - aGazePoint.y + _correctionY, 0);
        }
    }
}
