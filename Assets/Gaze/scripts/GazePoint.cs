using UnityEngine;
using UnityEngine.UI;

public class GazePoint : MonoBehaviour
{
    Image _image;

    int _correctionY = 0;

    void Start()
    {
        _image = GetComponent<Image>();

        var gazeSimulator = FindObjectOfType<GazeSimulator>();

        _correctionY = gazeSimulator.Enabled ? GazeSimulator.TOOLBAR_HEIGHT : 0;
    }

    public void MoveTo(GazeIO.Sample aGazePoint)
    {
        _image.transform.localPosition = new Vector3(aGazePoint.x - Screen.width / 2, Screen.height / 2 - aGazePoint.y + _correctionY, 0);
    }
}
