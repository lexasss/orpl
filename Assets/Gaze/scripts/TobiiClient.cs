using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tobii.Research;
using GazeIO;

public class TobiiClient : MonoBehaviour
{
    public enum Eye { Left, Right, Both }

    public Eye eye { get; set; } = Eye.Both;

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

    public void Close()
    {
        if (_eyeTracker != null)
        {
            _eyeTracker.Dispose();
            _eyeTracker = null;
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
            Sample sample;
            lock (lastSample)
            {
                sample = Sample.Copy(lastSample);
                lastSample = null;
            }

            Data(this, sample);
        }
    }

    void onGazeDataReceived(object sender, GazeDataEventArgs e)
    {
        var left = e.LeftEye;
        var right = e.RightEye;
        var gpLeft = left.GazePoint.PositionOnDisplayArea;
        var gpRight = right.GazePoint.PositionOnDisplayArea;

        Sample sample = new Sample();
        sample.type = MessageType.sample;
        sample.ts = (ulong)(e.SystemTimeStamp / 1000);

        var leftIsValid = !float.IsNaN(gpLeft.X) && !float.IsNaN(gpLeft.Y);
        var rightIsValid = !float.IsNaN(gpRight.X) && !float.IsNaN(gpRight.Y);

        if ((eye == Eye.Left && leftIsValid) ||
            (eye == Eye.Both && !rightIsValid))
        {
            sample.p = float.IsNaN(left.Pupil.PupilDiameter) ? 0 : left.Pupil.PupilDiameter;
            sample.x = gpLeft.X * Screen.width;
            sample.y = gpLeft.Y * Screen.height;
        }
        else if ((eye == Eye.Right && rightIsValid) ||
                 (eye == Eye.Both && !leftIsValid))
        {
            sample.p = float.IsNaN(right.Pupil.PupilDiameter) ? 0 : right.Pupil.PupilDiameter;
            sample.x = gpRight.X * Screen.width;
            sample.y = gpRight.Y * Screen.height;
        }
        else if (eye == Eye.Both && leftIsValid && rightIsValid)
        {
            if (float.IsNaN(right.Pupil.PupilDiameter))
            {
                sample.p = left.Pupil.PupilDiameter;
            }
            else if (float.IsNaN(left.Pupil.PupilDiameter))
            {
                sample.p = right.Pupil.PupilDiameter;
            }
            else
            {
                sample.p = (left.Pupil.PupilDiameter + right.Pupil.PupilDiameter) / 2;
            }

            sample.x = (gpLeft.X + gpRight.X) / 2 * Screen.width;
            sample.y = (gpLeft.Y + gpRight.Y) / 2 * Screen.height;
        }
        else
        {
            sample.x = -Screen.width;
            sample.y = -Screen.height;
        }

        sample.x = (float)Math.Round(sample.x);
        sample.y = (float)Math.Round(sample.y);

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
