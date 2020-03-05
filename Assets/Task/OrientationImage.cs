using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class OrientationImage : MonoBehaviour
{
    // initialized in inspector

    public Image[] _faces;

    // public props

    public GameObject faceImage { get { return _face?.gameObject; } }

    // internal vars

    Image _face = null;

    // overrides 

    void Start()
    {
    }

    void Update()
    {

    }

    // public

    public void Show(string aFace)
    {
        if (_face != null)
        {
            _face.gameObject.SetActive(false);
        }

        _face = _faces.Single(face => face.name == aFace);
        _face.gameObject.SetActive(true);
    }


    public void Finish()
    {
        _face.gameObject.SetActive(false);
        _face = null;
    }

    // internal
}
