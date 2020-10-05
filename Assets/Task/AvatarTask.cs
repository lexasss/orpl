using UnityEngine;
using UnityEngine.UI;

public class AvatarTask : MonoBehaviour
{
    // to set in inspector

    public AudioSource pupuAudioDirect;
    public AudioSource pupuAudioAvert;
    public AudioSource nalleAudioDirect;
    public AudioSource nalleAudioAvert;
    public Image curtain;
    public Color flashColor = new Color(1f, 1f, 1f);
    public float audioDelay = 5f;
    public float modeDuration = 30f;
    public float flashDuration = 0.2f;

    // vars

    HRClient _hrClient;
    AudioSource _audioToFinish = null;
    bool _isWaitingForFinish = false;
    char _avatarID;
    char _modeID;
    bool _isAvertMode;
    Color _curtainColor;

    void Start()
    {
        _hrClient = GetComponent<HRClient>();
        _curtainColor = curtain.color;
    }

    void Update()
    {
        if (_isWaitingForFinish && _audioToFinish)
        {
            if (!_audioToFinish.isPlaying)
            {
                _isWaitingForFinish = false;

                _hrClient.StopAvatarTask(_avatarID, _modeID);
                CancelInvoke("Signal");

                _audioToFinish = null;
                curtain.gameObject.SetActive(false);
            }
        }
    }

    public void StartPupuDirectTask()
    {
        _audioToFinish = pupuAudioDirect;
        _avatarID = 'P';
        _modeID = 'd';

        Invoke("StartListeningForFinish", audioDelay);

        curtain.gameObject.SetActive(true);
    }

    public void StartPupuAvertTask()
    {
        _audioToFinish = pupuAudioAvert;
        _avatarID = 'P';
        _modeID = 'a';

        Invoke("StartListeningForFinish", audioDelay);

        curtain.gameObject.SetActive(true);
    }

    public void StartNalleDirectTask()
    {
        _audioToFinish = nalleAudioDirect;
        _avatarID = 'N';
        _modeID = 'd';

        Invoke("StartListeningForFinish", audioDelay);

        curtain.gameObject.SetActive(true);
    }

    public void StartNalleAvertTask()
    {
        _audioToFinish = nalleAudioAvert;
        _avatarID = 'N';
        _modeID = 'a';

        Invoke("StartListeningForFinish", audioDelay);

        curtain.gameObject.SetActive(true);
    }

    private void StartListeningForFinish()
    {
        _isWaitingForFinish = true;

        _hrClient.StartAvatarTask(_avatarID, _modeID);

        _audioToFinish.Play();

        _isAvertMode = _modeID == 'a';
        Invoke("Signal", modeDuration);

        curtain.color = flashColor;
        Invoke("SetCurtain", flashDuration);
    }

    private void Signal()
    {
        _isAvertMode = !_isAvertMode;
        _hrClient.AvatarChangeInteraction(_avatarID, _isAvertMode ? 'a' : 'd');

        Invoke("Signal", modeDuration);

        // flashing at each signal
        // curtain.color = flashColor;
        // Invoke("SetCurtain", FlashDuration);
    }

    private void SetCurtain()
    {
        curtain.color = _curtainColor;
    }
}
