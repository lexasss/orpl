using System;
using UnityEngine;

public class PlayingLadyTask : MonoBehaviour
{
    // to set in inspector

    public GameObject background;
    public GameObject headArea;
    public AudioSource[] sounds;
    
    // public members

    public event EventHandler<BlockFinishedEventArgs> BlockFinished = delegate { };

    // definitions

    const string TRIALS_FILENAME = "playinglady.txt";
    const int BLOCK_SIZE = 3;
    const int VARIABLE_COUNT = 5;

    const float DWELL_TIME_SECOND_PART = 0.3f;
    const float MAX_WAITING_GAZING_FACE = 5f;
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

        _focusDetector = FindObjectOfType<FocusDetector>();
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

        bool spacePressed = Input.GetKeyDown(KeyCode.Space);
        if (spacePressed)
        {
            if (!_isRunning)
            {
                _isRunning = true;
                _taskState = TaskState.NotStarted;
                _trial = _trials.StartBlock();
                background.SetActive(false);
            }

            if (_taskState == TaskState.NotStarted)
            {
                NextState();
            }
        }

        bool enterPressed = Input.GetKeyDown(KeyCode.Return);
        if (enterPressed && _taskState != TaskState.NotStarted && _taskState != TaskState.Finished)
        {
            RevertTrial();
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
        background.SetActive(true);
        _isEnabled = true;

        _log.StartBlock("PlayingLady");
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
            // background.SetActive(false);

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

            Invoke("NextState", MAX_WAITING_GAZING_FACE);
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

            // background.SetActive(true);

            Invoke("ResetState", INTER_TRIAL_MIN_DURATION);
        }
        else
        {
            throw new IndexOutOfRangeException($"SetState: task is in unsupported state: {_taskState}");
        }
    }

    void RevertTrial()
    {
        CancelInvoke();

        _focusDetector.SetTrackingObject(null);
        _player.Stop();
        // background.SetActive(true);

        SetState(TaskState.NotStarted);

        _log.Restart();
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
