﻿using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

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
    public AudioSource backgroundAudio;
    public VideoPlayer baselinePlayer;
    public Button baselineButton;
    public Button tasksButton;

    // definitions

    const float PAUSE_BETWEEN_BLOCKS = 2f;

    // internal defs

    OrientationTask _orientation;
    PlayingLadyTask _playingLady;
    HRClient _hrClient;
    Log _log;

    GazePoint _gazePoint;
    GazeClient _gazeClient;

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

        baselinePlayer.loopPointReached += onBaselineStopped;

        FillParticipantIDs();
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            if (baselinePlayer.isPlaying)
            {
                baselinePlayer.Stop();
                onBaselineStopped(null);
            }
        }
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

    public void StartBaseline()
    {
        _gazeClient.HideUI();
        baselinePlayer.Play();

        _hrClient.StartBaseline();
    }

    public void StartTasks()
    {
        _gazeClient.HideUI();

        backgroundAudio.Play();

        infoDisplay.text = "starting...";

        Invoke("PlayingLadyNextBlock", 0.5f);
        // Invoke("OrientationNextBlock", 0.5f);

        _hrClient.StartTasks();
    }

    public void onParticipantIDChanged(Dropdown aDropdown)
    {
        Debug.Log($"session ID = {participantIDDropdown.options[participantIDDropdown.value]}");
    }

    public void Finish()
    {
        // sessionDone.Play();
        _hrClient.Stop();
        _log.Close();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // private methods

    void FillParticipantIDs()
    {
        var orderFiles = System.IO.Directory.EnumerateFiles(Trials<PlayingLadyTrial>.FOLDER)
            .Where(file => file.Contains("orientation"))
            //.Select((file, i) => (i + 1).ToString())
            .Select(file => file.Split('/', '\\').Last())
            .Select(file => file.Split('_')[0])
            .ToList();

        orderFiles.Sort();

        participantIDDropdown.ClearOptions();
        participantIDDropdown.AddOptions(orderFiles);
    }

    void onGazeClientStart(object sender, EventArgs e)
    {
        bool allLoaded = true;

        var sessionID = participantIDDropdown.options[participantIDDropdown.value].text;

        allLoaded = _playingLady.Load($"{sessionID}_order_") && allLoaded;
        allLoaded = _orientation.Load($"{sessionID}_order_") && allLoaded;

        if (!allLoaded)
        {
            infoDisplay.text = "Error in loading the task order file(s)";
            _log.Error(infoDisplay.text);
        }
        else
        {
            baselineButton.interactable = true;
            tasksButton.interactable = true;
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

    void onBaselineStopped(VideoPlayer player)
    {
        _gazeClient.ShowUI();
        _hrClient.StopBaseline();
    }
}
