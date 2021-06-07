using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class PlayingLadyTask : MonoBehaviour
{
    // to set in inspector

    public bool showVideoBetweenTrials = false;
    public VideoPlayer restingVideoPlayer;
    public GameObject headArea;
    public AudioSource[] sounds;
    public Text debug;
    public float maxGazeWaitingTime = 5f;

    // public members

    public event EventHandler<BlockFinishedEventArgs> BlockFinished = delegate { };
    public event EventHandler<bool> Cancelled = delegate { };   // bool: requests to display interruption media

    public bool IsRunning { get { return _isRunning; } }

    // definitions

    const string TRIALS_FILENAME = "playinglady.txt";
    const int BLOCK_SIZE = 2;
    const int VARIABLE_COUNT = 5;

    const float DWELL_TIME_SECOND_PART = 0.3f;
    const float INTER_TRIAL_MIN_DURATION = 1f;

    enum TaskState
    {
        NotStarted,
        First,
        WaitingFaceGazed,
        Second,
        Finished,
    }

    // internal members

    TaskState _taskState = TaskState.NotStarted;

    PlayingLadyPlayer _player;
    FocusDetector _focusDetector;
    HRClient _hrClient;
    Log _log;
    RestingImages _restingImages;

    Trials<PlayingLadyTrial> _trials;
    PlayingLadyTrial _trial = null;
    bool _isRunning = false;
    bool _isEnabled = false;

    AudioSource trialDone { get { return sounds[0]; } }
    AudioSource blockDone { get { return sounds[1]; } }

    // overrides

    void Start()
    {
        _hrClient = GetComponent<HRClient>();
        _log = GetComponent<Log>();
        _restingImages = GetComponent<RestingImages>();

        _focusDetector = FindObjectOfType<FocusDetector>();
        // _focusDetector.SetDebugOutput(debug);
        _focusDetector.Focused += onHeadAreaFocused;

        _player = FindObjectOfType<PlayingLadyPlayer>();
        _player.Started += onClipStarted;
        _player.Stopped += onClipStopped;
    }

    void Update()
    {
        if (!_isEnabled)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            _isRunning = true;

            if (_taskState == TaskState.NotStarted)
            {
                HideRestingMedia();
                NextState();
            }
        }

        if (_taskState != TaskState.NotStarted && _taskState != TaskState.Finished)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                RevertTrial();
            }
            else if (Input.GetKeyDown(KeyCode.I))
            {
                CancelTrial();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelTrial(false);
            }
        }
    }

    // public methods

    public bool Load(string aFileNamePrefix = "")
    {
        _trials = new Trials<PlayingLadyTrial>(aFileNamePrefix + TRIALS_FILENAME, BLOCK_SIZE, VARIABLE_COUNT);
        return _trials.IsValid;
    }

    public void NextBlock()
    {
        _isEnabled = true;

        _log.StartBlock("PlayingLady");

        _taskState = TaskState.NotStarted;
        _trial = _trials.StartBlock();

        DisplayRestingMedia();
    }

    // internal methods

    void NextState()
    {
        if (_taskState == TaskState.NotStarted)
        {
            SetState(TaskState.First);
        }
        else if (_taskState == TaskState.First)
        {
            SetState(TaskState.WaitingFaceGazed);
        }
        else if (_taskState == TaskState.WaitingFaceGazed)
        {
            SetState(TaskState.Second);
        }
        else if (_taskState == TaskState.Second)
        {
            SetState(TaskState.Finished);
        }
        else
        {
            throw new IndexOutOfRangeException($"NextState: playing-lady task is in unsupported state: {_taskState}");
        }
    }

    void ResetState()
    {
        _trial = _trials.Next();

        if (_trial != null)
        {
            trialDone.Play();
            SetState(TaskState.NotStarted);
        }
        else
        {
            _isRunning = false;
            HideRestingMedia();

            blockDone.Play();

            _isEnabled = false;
            BlockFinished(this, new BlockFinishedEventArgs(!_trials.HasMoreTrials));
        }
    }

    void SetState(TaskState aState)
    {
        _taskState = aState;
        Debug.Log($"task state: {_taskState}");

        if (_taskState == TaskState.NotStarted)
        {
            // do nothing
        }
        else if (_taskState == TaskState.First)
        {
            var gaze = _trial.Gaze;
            var direction = _trial.Direction;
            var slide = _trial.Slide;
            var color = _trial.Color;
            var runCount = _trial.RunCount;

            _hrClient.PlayingLadyStarts(runCount);

            _player.SetClips($"{gaze}_{direction}_{slide}_{color}", runCount.ToString());
            _player.PlayFirst();
        }
        else if (_taskState == TaskState.WaitingFaceGazed)
        {
            _hrClient.PlayingLadyPauses();

            _focusDetector.SetTrackingObject(headArea);

            Invoke("NextState", maxGazeWaitingTime);
        }
        else if (_taskState == TaskState.Second)
        {
            var gaze = _trial.Gaze;
            var direction = _trial.Direction;
            var slide = _trial.Slide;

            if (gaze == "direct")
            {
                _hrClient.PlayingLadyDirect(slide, direction);
            }
            else if (gaze == "down")
            {
                _hrClient.PlayingLadyDown(slide, direction);
            }
            else
            {
                _hrClient.PlayingLadyAverted(slide, direction);
            }

            _player.PlaySecond();
        }
        else if (_taskState == TaskState.Finished)
        {
            _focusDetector.SetTrackingObject(null);
            _player.Stop();
            _log.TrialFinished(_trials.CurrentIndex);
            _hrClient.TrialFinished();

            DisplayRestingMedia();

            Invoke("ResetState", INTER_TRIAL_MIN_DURATION);
        }
        else
        {
            throw new IndexOutOfRangeException($"SetState: task is in unsupported state: {_taskState}");
        }
    }

    void DisplayRestingMedia()
    {
        if (_trials.HasMoreBlockTrials)
        {
            if (showVideoBetweenTrials)
            {
                restingVideoPlayer.Play();
            }
            else
            {
                _restingImages.Show();
            }
        }
    }

    void HideRestingMedia()
    {
        if (showVideoBetweenTrials)
        {
            restingVideoPlayer.Stop();
        }
        else
        {
            _restingImages.Hide();
        }
    }

    void RevertTrial()
    {
        CancelInvoke();

        _focusDetector.SetTrackingObject(null);
        _player.Stop();
        
        _log.Restart();
        _hrClient.TrialRestarted();

        DisplayRestingMedia();
        
        SetState(TaskState.NotStarted);
    }

    void CancelTrial(bool showInterruptionMedia = true)
    {
        CancelInvoke();

        _focusDetector.SetTrackingObject(null);
        _player.Stop();

        _log.Cancelled();
        _hrClient.TrialCancelled();

        if (!showInterruptionMedia)
        {
            DisplayRestingMedia();
        }

        SetState(TaskState.NotStarted);
        ResetState();

        Cancelled(this, showInterruptionMedia);
    }

    void onClipStarted(object sender, EventArgs e)
    {
        // background.SetActive(false);
    }

    void onClipStopped(object sender, EventArgs e)
    {
        NextState();
    }

    void onHeadAreaFocused(object sender, EventArgs args)
    {
        _focusDetector.SetTrackingObject(null);

        if (_taskState == TaskState.WaitingFaceGazed)
        {
            CancelInvoke("NextState");

            _hrClient.FixationOnFace();

            Invoke("NextState", DWELL_TIME_SECOND_PART);
        }
        else if (_taskState == TaskState.Second)
        {
            _hrClient.FixationOnFace();
        }
    }
}
