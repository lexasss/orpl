using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FocusDetector : MonoBehaviour
{
    // public members

    public event EventHandler Focused = delegate { };

    // definitions

    const int GAZE_POINTS_TO_ACTIVATE_OBJECT = 18;

    // internal members

    bool _enabled = false;
    bool _isFocused = false;
    Rect _rect = new Rect();

    GazeClient _gaze;

    int _gazeOnObjectCount = 0;

    Text _debug = null;

    // overrides

    void Start()
    {
        _gaze = FindObjectOfType<GazeClient>();
        _gaze.Sample += onSample;
    }

    // public methods

    /// <summary>
    /// Sets or resets the tracking object
    /// </summary>
    /// <param name="aObject">object to track gaze focus, or null</param>
    public void SetTrackingObject(GameObject aObject)
    {
        if (aObject != null)
        {
            var rc = aObject.transform as RectTransform;
            _rect = new Rect(rc.offsetMin.x, rc.offsetMin.y, rc.sizeDelta.x, rc.sizeDelta.y);
            Debug.Log($"Tracking object: {_rect.xMin}, {_rect.yMin}, {_rect.xMax}, {_rect.yMax} | {_rect.x}, {_rect.y}");
            _enabled = true;
        }
        else
        {
            _enabled = false;
        }

        _isFocused = false;
        _gazeOnObjectCount = 0;
    }

    public void SetDebugOutput(Text aDebug)
    {
        _debug = aDebug;
    }

    // internal methods

    void onSample(object sender, EventArgs e)
    {
        if (_enabled && !_isFocused)
        {
            var gazePos = _gaze.location;
            bool isInObject = IsInRect(gazePos);
            var newGazePointCount = _gazeOnObjectCount + (isInObject ? 1 : -1);
            if (newGazePointCount == GAZE_POINTS_TO_ACTIVATE_OBJECT)
            {
                _isFocused = true;
                Focused(this, new EventArgs());
            }

            _gazeOnObjectCount = Math.Max(0, newGazePointCount);

            if (_debug != null)
            {
                _debug.text = _gazeOnObjectCount.ToString();
            }
        }
    }

    bool IsInRect(RawPoint aPoint)
    {
        return _rect.xMin < aPoint.x && aPoint.x < _rect.xMax &&
               _rect.yMin < aPoint.y && aPoint.y < _rect.yMax;
    }
}
