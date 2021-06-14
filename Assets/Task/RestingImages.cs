using UnityEngine;
using UnityEngine.UI;

public class RestingImages : MonoBehaviour
{
    // to be set in inspector

    public Image[] images;


    // internal defs

    Image _visibleImage = null;

    // public methods

    public void Show()
    {
        if (_visibleImage == null)
        {
            var index = Random.Range(0, images.Length);
            _visibleImage = images[index];
            _visibleImage.gameObject.SetActive(true);
        }
    }

    public void Hide()
    {
        if (_visibleImage != null)
        {
            _visibleImage.gameObject.SetActive(false);
            _visibleImage = null;
        }
    }
}
