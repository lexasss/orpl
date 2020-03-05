using System;
using System.Collections.Generic;
using UnityEngine;

public class HRClient : MonoBehaviour
{
    // to be set in inspector

    public UnityEngine.UI.Text status;

    // internal members

    NetStation _netStation;
    Log _log;

    // overides

    void Start()
    {
        _netStation = GetComponent<NetStation>();
        _netStation.Message += onNetStationMessage;

        _log = FindObjectOfType<Log>();
    }

    // public methods

    public void Connect()
    {
        status.text = $"connecting to {_netStation.Host}:{_netStation.Port}...";
        _netStation.Connect();
    }

    public void Run()
    {
        _netStation.Begin();
        SendEvent("OrPL");
    }

    public void Stop()
    {
        _netStation.End();
        _netStation.Disconnect();
    }

    public void AttentionGrabber()
    {
        SendEvent("AtGr");
    }

    public void OrientationGazeDownward(string aActor, string aHead)
    {
        SendEvent("Do" + aActor.ToUpper()[0] + aHead[0]);
    }

    public void OrientationGazeDirect(string aActor, string aHead)
    {
        SendEvent("Di" + aActor.ToUpper()[0] + aHead[0]);
    }

    public void OrientationGazeAverted(string aActor, string aHead)
    {
        SendEvent("Av" + aActor.ToUpper()[0] + aHead[0]);
    }

    public void PlayingLadyStarts(int aSlideCount)
    {
        SendEvent($"Lst{aSlideCount}");
    }

    public void PlayingLadyPauses()
    {
        SendEvent("Lpau");
    }

    public void PlayingLadyDown(int aSlide, string aDirection)
    {
        SendEvent("Do" + aSlide.ToString() + aDirection[0]);
    }

    public void PlayingLadyDirect(int aSlide, string aDirection)
    {
        SendEvent("Di" + aSlide.ToString() + aDirection[0]);
    }

    public void PlayingLadyAverted(int aSlide, string aDirection)
    {
        SendEvent("Av" + aSlide.ToString() + aDirection[0]);
    }

    public void FixationOnFace()
    {
        SendEvent("FAOI");
    }

    // internal methods

    void SendEvent(string aMessage)
    {
        _log.HR(aMessage);
        _netStation.Event(aMessage);
    }

    void onNetStationMessage(object sender, NetStation.StateChangedEventArgs e)
    {
        status.text = e.Message;
    }
}
