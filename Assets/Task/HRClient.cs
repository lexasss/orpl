﻿using UnityEngine;
using UnityEngine.UI;

public class HRClient : MonoBehaviour
{
    // to be set in inspector

    public Text status;
    public Button connectButton;

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

    public void Begin()
    {
        _netStation.Begin();
    }

    public void Stop()
    {
        _netStation.End();
        _netStation.Disconnect();
    }

    public void StartTasks()
    {
        SendEvent("OrPL");
    }

    public void StartBaseline()
    {
        SendEvent("BL-s");
    }

    public void StartSocialVideo(char aType)
    {
        SendEvent($"SV{aType}s");
    }

    public void StopBaseline()
    {
        SendEvent("BL-e");
    }

    public void StopSocialVideo(char aType)
    {
        SendEvent($"SV{aType}e");
    }

    public void StartAvatarTask(char aID, char aType)
    {
        SendEvent($"L{aID}s{aType}");
    }

    public void StopAvatarTask(char aID, char aType)
    {
        SendEvent($"L{aID}e{aType}");
    }

    public void AvatarChangeInteraction(char aID, char aType)
    {
        SendEvent($"L{aID}-{aType}");
    }

    public void AttentionGrabber()
    {
        SendEvent("AtGr");
    }

    public void OrientationGazeDownward(string aActor, string aHead)
    {
        var actorType = aActor[0] < 'A' ? 'c' : 'f'; // [c]lock or [f]ace
        SendEvent("O" + actorType + aActor.ToUpper()[0] + aHead[0]);
    }

    public void OrientationGazeStraight(string aActor, string aHead)
    {
        var actorType = aActor[0] < 'A' ? 'e' : 's'; // [e]tenpain(clock) or [s]traight(face)
        SendEvent("O" + actorType + aActor.ToUpper()[0] + aHead[0]);
    }

    public void OrientationGazeAverted(string aActor, string aHead)
    {
        var actorType = aActor[0] < 'A' ? 't' : 'a'; // [t]aaksepain(clock) or [a]verted(face)
        SendEvent("O" + actorType + aActor.ToUpper()[0] + aHead[0]);
    }

    public void PlayingLadyStarts(int aSlideCount)
    {
        SendEvent($"P{aSlideCount}st");
    }

    public void PlayingLadyPauses(int aSlide)
    {
        SendEvent($"P{aSlide}pa");
    }

    public void PlayingLadyDown(int aSlide, string aDirection)
    {
        SendEvent($"P{aSlide}d{aDirection[0]}");
    }

    public void PlayingLadyStraight(int aSlide, string aDirection)
    {
        SendEvent($"P{aSlide}s{aDirection[0]}");
    }

    public void PlayingLadyAverted(int aSlide, string aDirection)
    {
        SendEvent($"P{aSlide}a{aDirection[0]}");
    }

    public void FixationOnFace()
    {
        SendEvent("FAOI");
    }

    public void TrialFinished()
    {
        SendEvent("FNSH");
    }

    public void TrialCancelled()
    {
        SendEvent("CNCL");
    }

    public void TrialRestarted()
    {
        SendEvent("RSTR");
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

        if (e.State == NetStation.State.CONNECTED)
        {
            connectButton.enabled = false;
            Invoke(nameof(Begin), 1);
        }
        else if (e.State == NetStation.State.NOT_CONNECTED || e.State == NetStation.State.FAILED_TO_CONNECT)
        {
            connectButton.enabled = true;
        }
    }
}
