using System;
using UnityEngine;

public class OrientationTask : MonoBehaviour
{
    // to set in inspector

    public GameObject background;
    public GameObject attentionGrabber;
    public AudioSource[] sounds;

    // public members

    public event EventHandler<BlockFinishedEventArgs> BlockFinished = delegate { };

    // definitions

    const string TRIALS_FILENAME = "orientation.txt";
    const int BLOCK_SIZE = 3;
    const int VARIABLE_COUNT = 3;

    const float DWELL_TIME_ATTENTION_GRABBER = 1f;
    const float GAZE_DOWNWARD_DURATION = 2f;
    const float GAZE_UPWARD_DURATION = 3f;
    const float INTER_TRIAL_MIN_DURATION = 1f;

    enum TaskState
    {
        NotStarted,
        AttentionGrabber,
        GazeDown,
        GazeUp,
        Finished,
    }

    // internal members

    TaskState _taskState = TaskState.NotStarted;

    OrientationImage _image;
    FocusDetector _focusDetector;
    HRClient _hrClient;
    Log _log;

    Trials<OrientationTrial> _trials;
    OrientationTrial _trial = null;
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
        _focusDetector.Focused += onAttentionGrabberFocused;

        _image = GetComponentInChildren<OrientationImage>();
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

            if (_taskState == TaskState.NotStarted || _taskState == TaskState.AttentionGrabber)
            {
                NextState();
            }
        }
    }

    // public methods

    public bool Load(string aFileNamePrefix = "")
    {
        _trials = new Trials<OrientationTrial>(aFileNamePrefix + TRIALS_FILENAME, BLOCK_SIZE, VARIABLE_COUNT);
        return _trials.IsValid;
    }

    public void NextBlock()
    {
        background.SetActive(true);
        _isEnabled = true;

        _log.StartBlock("Orientation");
    }

    // internal methods

    void NextState()
    {
        if (_taskState == TaskState.NotStarted)
        {
            SetState(TaskState.AttentionGrabber);
        }
        else if (_taskState == TaskState.AttentionGrabber)
        {
            SetState(TaskState.GazeDown);
        }
        else if (_taskState == TaskState.GazeDown)
        {
            SetState(TaskState.GazeUp);
        }
        else if (_taskState == TaskState.GazeUp)
        {
            SetState(TaskState.Finished);
        }
        else
        {
            throw new IndexOutOfRangeException($"NextState: orientation task is in unsupported state: {_taskState}");
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
        else if (_taskState == TaskState.AttentionGrabber)
        {
            attentionGrabber.SetActive(true);
            attentionGrabber.GetComponent<AttentionGrabber>().Run();

            _focusDetector.SetTrackingObject(attentionGrabber);
            _hrClient.AttentionGrabber();
        }
        else if (_taskState == TaskState.GazeDown)
        {
            attentionGrabber.GetComponent<AttentionGrabber>().Stop();
            attentionGrabber.SetActive(false);

            var actor = _trial.Actor;
            var head = _trial.Head;

            _image.Show($"{actor}-{head}-down");
            // _focusDetector.SetTrackingObject(_image.faceImage);

            _hrClient.OrientationGazeDownward(actor, head);

            Invoke("NextState", GAZE_DOWNWARD_DURATION);
        }
        else if (_taskState == TaskState.GazeUp)
        {
            var actor = _trial.Actor;
            var head = _trial.Head;
            var gaze = _trial.Gaze;

            _image.Show($"{actor}-{head}-up-{gaze[0]}");
            // _focusDetector.SetTrackingObject(_image.faceImage);

            if (gaze == "direct")
            {
                _hrClient.OrientationGazeDirect(actor, head);
            }
            else
            {
                _hrClient.OrientationGazeAverted(actor, head);
            }

            Invoke("NextState", GAZE_UPWARD_DURATION);
        }
        else if (_taskState == TaskState.Finished)
        {
            _image.Finish();
            _log.TrialFinished(_trials.CurrentIndex);

            Invoke("ResetState", INTER_TRIAL_MIN_DURATION);
        }
        else
        {
            throw new IndexOutOfRangeException($"SetState: task is in unsupported state: {_taskState}");
        }
    }

    void onAttentionGrabberFocused(object sender, EventArgs args)
    {
        _focusDetector.SetTrackingObject(null);
        if (_taskState == TaskState.AttentionGrabber)
        {
            Invoke("NextState", DWELL_TIME_ATTENTION_GRABBER);
        }
    }
}
