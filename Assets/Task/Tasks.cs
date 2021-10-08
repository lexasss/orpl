using System;
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
    public enum MediaType
    {
        Image,
        Video
    }

    // to set in inspector

    public Text infoDisplay;
    public AudioSource sessionDone;
    public Dropdown participantIDDropdown;
    public AudioSource backgroundAudio;
    public VideoPlayer baselinePlayer;
    public VideoPlayer socialVideoPlayer;
    public Button baselineButton;
    public Button tasksButton;
    public VideoPlayer playingLadyPlayer;
    public MediaType interruptionMedia;
    public GameObject interruptionImage;
    public VideoPlayer interruptionPlayer;

    // definitions

    const float PAUSE_BETWEEN_BLOCKS = 2f;

    // internal defs

    OrientationTask _orientation;
    PlayingLadyTask _playingLady;
    HRClient _hrClient;
    Log _log;

    GazePoint _gazePoint;
    GazeClient _gazeClient;

    bool _isLastBlock = false;
    bool _isWaitingSocialVideo = false;
    bool _socialVideoOnly;
    SocialVideos _socialVideos;

    ITask _currentTask = null;

    // overrides

    void Start()
    {
        _orientation = GetComponent<OrientationTask>();
        _orientation.BlockFinished += onOrientationBlockFinished;
        _orientation.Cancelled += onOrientationTrialCancelled;

        _playingLady = GetComponent<PlayingLadyTask>();
        _playingLady.BlockFinished += onPlayingLadyBlockFinished;
        _playingLady.Cancelled += onPlayingLadyTrialCancelled;

        _hrClient = GetComponent<HRClient>();
        _log = GetComponent<Log>();

        _socialVideos = GetComponent<SocialVideos>();

        _gazePoint = FindObjectOfType<GazePoint>();

        _gazeClient = FindObjectOfType<GazeClient>();
        _gazeClient.Start += onGazeClientStart;
        _gazeClient.Sample += onGazeClientSample;

        baselinePlayer.loopPointReached += onBaselineStopped;
        socialVideoPlayer.loopPointReached += onSocialVideoStopped;

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
            else if (socialVideoPlayer.isPlaying)
            {
                socialVideoPlayer.Stop();
                onSocialVideoStopped(null);
            }
            else if (interruptionImage.activeSelf)
            {
                interruptionImage.SetActive(false);
                _currentTask?.DisplayRestingMedia(true);
            }
            else if (interruptionPlayer.isPlaying)
            {
                interruptionPlayer.Stop();
                _currentTask?.DisplayRestingMedia(true);
            }
        }
        else if (Input.GetKey(KeyCode.Space))
        {
            if (interruptionImage.activeSelf)
            {
                interruptionImage.SetActive(false);
            }
            else if (interruptionPlayer.isPlaying)
            {
                interruptionPlayer.Stop();
            }
            else if (_isWaitingSocialVideo)
            {
                _isWaitingSocialVideo = false;
                socialVideoPlayer.clip = _socialVideos.Next();
                PlaySocialVideo();
            }
        }
    }

    private void onGazeClientSample(object sender, EventArgs e)
    {
        _gazePoint.MoveTo(_gazeClient.lastSample);
    }

    // public methods

    // Response to UI events

    public void StartBaseline()
    {
        _gazeClient.HideUI();

        baselinePlayer.gameObject.SetActive(true);
        baselinePlayer.Play();

        _hrClient.StartBaseline();
    }

    public void StartTasks()
    {
        if (!LoadTasksFromFile())
        {
            return;
        }

        _gazeClient.HideUI();
        playingLadyPlayer.gameObject.SetActive(true);

        backgroundAudio.Play();

        infoDisplay.text = "starting...";

        Invoke(nameof(PlayingLadyNextBlock), 0.5f);
        // Invoke(nameof(OrientationNextBlock), 0.5f);

        _isLastBlock = false;
        _socialVideoOnly = false;
        _socialVideos.Reset();

        _hrClient.StartTasks();
    }

    public void StartSocialVideo(VideoClip videoClip)
    {
        _gazeClient.HideUI();
        _socialVideoOnly = true;

        socialVideoPlayer.clip = videoClip;

        PlaySocialVideo();
    }

    public void onParticipantIDChanged(Dropdown aDropdown)
    {
        Debug.Log($"session ID = {participantIDDropdown.options[participantIDDropdown.value]}");
    }

    // To be invoked

    public void PlayingLadyNextBlock()
    {
        _currentTask = _playingLady;
        _playingLady.NextBlock();
        infoDisplay.text = "";
    }

    public void OrientationNextBlock()
    {
        _currentTask = _orientation;
        _orientation.NextBlock();
        infoDisplay.text = "";
    }

    public void Finish()
    {
        if (_gazeClient.isTracking)
        {
            _gazeClient.ToggleTracking();
        }

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
        var orderFiles = System.IO.Directory.EnumerateFiles(Files.FOLDER)
            .Where(file => file.Contains("orientation"))
            //.Select((file, i) => (i + 1).ToString())
            .Select(file => file.Split('/', '\\').Last())
            .Select(file => file.Split('_')[0])
            .ToList();

        orderFiles.Sort();

        participantIDDropdown.ClearOptions();
        participantIDDropdown.AddOptions(orderFiles);
    }

    bool LoadTasksFromFile()
    {
        bool allLoaded = true;

        var sessionID = participantIDDropdown.options[participantIDDropdown.value].text;

        allLoaded = _playingLady.Load($"{sessionID}_order_") && allLoaded;
        allLoaded = _orientation.Load($"{sessionID}_order_") && allLoaded;
        allLoaded = _socialVideos.Load($"{sessionID}_order_") && allLoaded;

        if (!allLoaded)
        {
            infoDisplay.text = "Error in loading the task order file(s)";
            _log.Error(infoDisplay.text);
        }

        return allLoaded;
    }

    void PlaySocialVideo()
    {
        backgroundAudio.mute = true;

        socialVideoPlayer.gameObject.SetActive(true);
        socialVideoPlayer.Play();

        var name = socialVideoPlayer.clip.name;
        _hrClient.StartSocialVideo(name[name.Length - 1]);
    }

    void StartInterruption()
    {
        if (interruptionMedia == MediaType.Image)
        {
            interruptionImage.SetActive(true);
        }
        else
        {
            interruptionPlayer.Play();
        }
    }

    void onGazeClientStart(object sender, EventArgs e)
    {
        // LoadTasksFromFile();

        var buttons = FindObjectsOfType(typeof(Button)).Where(btn => (btn as Button).tag == "only-gaze-active");
        foreach (var btn in buttons)
        {
            (btn as Button).interactable = true;
        }
    }

    void onPlayingLadyBlockFinished(object sender, BlockFinishedEventArgs e)
    {
        _currentTask = null;
        Invoke(nameof(OrientationNextBlock), PAUSE_BETWEEN_BLOCKS);
    }

    void onPlayingLadyTrialCancelled(object sender, bool showInterruptionMedia)
    {
        if (showInterruptionMedia)
        {
            Invoke(nameof(StartInterruption), 0.5f);
        }
    }

    void onOrientationBlockFinished(object sender, BlockFinishedEventArgs e)
    {
        _currentTask = null;
        _isLastBlock = e.IsLastBlock;
        _isWaitingSocialVideo = true;
    }

    void onOrientationTrialCancelled(object sender, bool showInterruptionMedia)
    {
        if (showInterruptionMedia)
        {
            Invoke(nameof(StartInterruption), 0.5f);
        }
    }

    void onSocialVideoStopped(VideoPlayer player)
    {
        var name = socialVideoPlayer.clip.name;
        _hrClient.StopSocialVideo(name[name.Length - 1]);

        socialVideoPlayer.gameObject.SetActive(false);

        backgroundAudio.mute = false;

        if (_socialVideoOnly)
        {
            _gazeClient.ShowUI();
        }
        else if (_isLastBlock)
        {
            _gazeClient.ShowUI();
            backgroundAudio.Stop();
            playingLadyPlayer.gameObject.SetActive(false);
        }
        else
        {
            Invoke(nameof(PlayingLadyNextBlock), PAUSE_BETWEEN_BLOCKS);
        }
    }

    void onBaselineStopped(VideoPlayer player)
    {
        _gazeClient.ShowUI();
        _hrClient.StopBaseline();
        baselinePlayer.gameObject.SetActive(false);
    }
}
