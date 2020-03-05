﻿using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class BlockFinishedEventArgs : EventArgs
{
    public bool IsLastBlock { get; private set; }
    public BlockFinishedEventArgs(bool aIsLastBlock)
    {
        IsLastBlock = aIsLastBlock;
    }
}

public class Tasks : MonoBehaviour
{
    // to set in inspector

    public Text infoDisplay;
    public AudioSource sessionDone;
    public Dropdown participantIDDropdown;

    // definitions

    const float PAUSE_BETWEEN_BLOCKS = 2f;

    // internal defs

    OrientationTask _orientation;
    PlayingLadyTask _playingLady;
    HRClient _hrClient;
    Log _log;

    GazePoint _gazePoint;
    GazeClient _gazeClient;

    int _participantID = 1;

    // overrides

    void Start()
    {
        _orientation = GetComponent<OrientationTask>();
        _orientation.BlockFinished += onOrientationBlockFinished;

        _playingLady = GetComponent<PlayingLadyTask>();
        _playingLady.BlockFinished += onPlayingLadyBlockFinished;

        _hrClient = GetComponent<HRClient>();
        _log = GetComponent<Log>();

        _gazePoint = FindObjectOfType<GazePoint>();

        _gazeClient = FindObjectOfType<GazeClient>();
        _gazeClient.Start += onGazeClientStart;
        _gazeClient.Sample += onGazeClientSample;

        FillParticipantIDs();
    }

    private void onGazeClientSample(object sender, EventArgs e)
    {
        _gazePoint.MoveTo(_gazeClient.lastSample);
    }

    // public methods

    public void PlayingLadyNextBlock()
    {
        _playingLady.NextBlock();
        infoDisplay.text = "";
    }

    public void OrientationNextBlock()
    {
        _orientation.NextBlock();
        infoDisplay.text = "";
    }

    public void onParticipantIDChanged(Dropdown aDropdown)
    {
        _participantID = aDropdown.value + 1;
        Debug.Log($"ID = {_participantID}");
    }

    // private methods

    void Finish()
    {
        sessionDone.Play();
        _hrClient.Stop();
        _log.Close();
    }

    void FillParticipantIDs()
    {
        var orderFiles = System.IO.Directory.EnumerateFiles(Trials<PlayingLadyTrial>.FOLDER)
            .Where(file => file.Contains("orientation"))
            .Select((file, i) => (i + 1).ToString())
            .ToList();

        participantIDDropdown.ClearOptions();
        participantIDDropdown.AddOptions(orderFiles);
    }

    void onGazeClientStart(object sender, EventArgs e)
    {
        infoDisplay.text = "starting...";

        bool allLoaded = true;
        allLoaded = _playingLady.Load($"{_participantID}_order_") && allLoaded;
        allLoaded = _orientation.Load($"{_participantID}_order_") && allLoaded;

        if (!allLoaded)
        {
            infoDisplay.text = "Error in loading the task order file(s)";
            _log.Error(infoDisplay.text);
        }
        else
        {
            Invoke("PlayingLadyNextBlock", 0.5f);
            // Invoke("OrientationNextBlock", 0.5f);
            _hrClient.Run();
        }
    }

    void onPlayingLadyBlockFinished(object sender, BlockFinishedEventArgs e)
    {
        Invoke("OrientationNextBlock", PAUSE_BETWEEN_BLOCKS);
    }

    void onOrientationBlockFinished(object sender, BlockFinishedEventArgs e)
    {
        if (e.IsLastBlock)
        {
            Invoke("Finish", PAUSE_BETWEEN_BLOCKS);
        }
        else
        {
            Invoke("PlayingLadyNextBlock", PAUSE_BETWEEN_BLOCKS);
        }
    }
}