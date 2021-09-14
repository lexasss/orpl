using UnityEngine;

public class AttentionGrabber : MonoBehaviour
{
    // definitions

    const float MIN_SCALE = 0.4f;
    const float SCALE_PHASE_STEP = 0.01f;

    // internal members

    UnityEngine.UI.Image _image;
    bool _isPulsing = false;
    float _scale = 1f;
    float _scalePhase = 0;

    float _scaleOrigin;
    float _scaleAmplitude;

    // overrides

    void Start()
    {
        _scaleAmplitude = (1f - MIN_SCALE) / 2f;
        _scaleOrigin = 1f - _scaleAmplitude;

        _image = GetComponent<UnityEngine.UI.Image>();
    }

    void Update()
    {
        if (!_isPulsing)
        {
            return;
        }

        _scalePhase += SCALE_PHASE_STEP;
        var amplitude = Mathf.Cos(2f * Mathf.PI * _scalePhase) * _scaleAmplitude;

        SetScale(_scaleOrigin + amplitude);
    }

    // public methods

    public void Run()
    {
        _scalePhase = 0;
        _isPulsing = true;
    }

    public void Stop()
    {
        _isPulsing = false;
        SetScale(1f);
    }

    // internal methods

    void SetScale(float aScale)
    {
        _scale = aScale;

        _image.transform.localScale = new Vector3(_scale, _scale, 1f);

        //Debug.Log(_scale);
    }
}
