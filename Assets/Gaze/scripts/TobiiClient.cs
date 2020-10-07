using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tobii.Research;
using GazeIO;

public class TobiiClient : MonoBehaviour
{
    public enum Eye { Left, Right, Both }

    public Eye eye = Eye.Both;

    public event EventHandler<string> Error = delegate { };
    public event EventHandler<string> Ready = delegate { };
    public event EventHandler<bool> Toggled = delegate { };
    public event EventHandler<Sample> Data = delegate { };

    private IEyeTracker _eyeTracker = null;
    private bool _isStreaming = false;
    private Sample lastSample = null;

    void Start()
    {
        SearchEyeTrackers();
    }

    public void ToggleTracking()
    {
        if (_eyeTracker != null)
        {
            if (!_isStreaming)
            {
                _eyeTracker.GazeDataReceived += onGazeDataReceived;
                InvokeRepeating("StreamData", 0.1f, 0.033f);
            }
            else
            {
                CancelInvoke("StreamData");
                _eyeTracker.GazeDataReceived -= onGazeDataReceived;
            }

            _isStreaming = !_isStreaming;
            Toggled(this, _isStreaming);
        }
    }

    // internal methods
    async void SearchEyeTrackers()
    {
        var collection = await EyeTrackingOperations.FindAllEyeTrackersAsync();
        if (collection.Count > 0)
        {
            var tracker = collection[0];

            try
            {
                _eyeTracker = EyeTrackingOperations.GetEyeTracker(tracker.Address);
            }
            catch (Exception ex)
            {
                Error(this, ex.Message);
            }

            if (_eyeTracker != null)
            {
                Ready(this, _eyeTracker.DeviceName);
            }
        }
        else
        {
            Invoke("SearchEyeTrackers", 5);
        }
    }

    void StreamData()
    {
        if (lastSample != null)
        {
            lock (lastSample)
            {
                Data(this, lastSample);
                lastSample = null;
            }
        }
    }

    void onGazeDataReceived(object sender, GazeDataEventArgs e)
    {
        var left = e.LeftEye;
        var right = e.RightEye;

        Sample sample = new Sample();
        sample.type = MessageType.sample;
        sample.ts = (ulong)(e.SystemTimeStamp / 1000);

        if ((eye == Eye.Left && left.GazePoint.Validity == Validity.Valid) ||
            (eye == Eye.Both && right.GazePoint.Validity == Validity.Invalid))
        {
            sample.p = left.Pupil.PupilDiameter;
            sample.x = left.GazePoint.PositionOnDisplayArea.X * Screen.width;
            sample.y = left.GazePoint.PositionOnDisplayArea.Y * Screen.height;
        }
        else if ((eye == Eye.Right && right.GazePoint.Validity == Validity.Valid) ||
                 (eye == Eye.Both && left.GazePoint.Validity == Validity.Invalid))
        {
            sample.p = right.Pupil.PupilDiameter;
            sample.x = right.GazePoint.PositionOnDisplayArea.X * Screen.width;
            sample.y = right.GazePoint.PositionOnDisplayArea.Y * Screen.height;
        }
        else if (eye == Eye.Both && left.GazePoint.Validity == Validity.Valid && right.GazePoint.Validity == Validity.Valid)
        {
            sample.p = (left.Pupil.PupilDiameter + right.Pupil.PupilDiameter) / 2;
            sample.x = (left.GazePoint.PositionOnDisplayArea.X + right.GazePoint.PositionOnDisplayArea.X) / 2 * Screen.width;
            sample.y = (left.GazePoint.PositionOnDisplayArea.Y + right.GazePoint.PositionOnDisplayArea.Y) / 2 * Screen.height;
        }
        else
        {
            sample.x = -Screen.width;
            sample.y = -Screen.height;
        }

        if (lastSample != null)
        {
            lock (lastSample)
            {
                lastSample = sample;
            }
        }
        else
        {
            lastSample = sample;
        }
    }
}
