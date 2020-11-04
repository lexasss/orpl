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
    public float flashDuration = 0.2f;
    public float flashDelay = 5f;
    public Color flashColor = new Color(1f, 1f, 1f);
    public float audioDelay = 5f;
    public float modeDuration = 30f;

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
        if (Input.GetKey(KeyCode.Escape))
        {
            if (_audioToFinish.isPlaying)
            {
                _audioToFinish.Stop();
            }
        }

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

        Invoke("Flash", flashDelay);

        curtain.gameObject.SetActive(true);
    }

    public void StartPupuAvertTask()
    {
        _audioToFinish = pupuAudioAvert;
        _avatarID = 'P';
        _modeID = 'a';

        Invoke("Flash", flashDelay);

        curtain.gameObject.SetActive(true);
    }

    public void StartNalleDirectTask()
    {
        _audioToFinish = nalleAudioDirect;
        _avatarID = 'N';
        _modeID = 'd';

        Invoke("Flash", flashDelay);

        curtain.gameObject.SetActive(true);
    }

    public void StartNalleAvertTask()
    {
        _audioToFinish = nalleAudioAvert;
        _avatarID = 'N';
        _modeID = 'a';

        Invoke("Flash", flashDelay);

        curtain.gameObject.SetActive(true);
    }

    private void Flash()
    {
        curtain.color = flashColor;
        Invoke("SetCurtain", flashDuration);

        Invoke("StartListeningForFinish", audioDelay);
        _hrClient.StartAvatarTask(_avatarID, _modeID);
    }

    private void StartListeningForFinish()
    {
        _isWaitingForFinish = true;

        _isAvertMode = _modeID == 'a';
        _hrClient.AvatarChangeInteraction(_avatarID, _isAvertMode ? 'a' : 'd');

        _audioToFinish.Play();

        Invoke("Signal", modeDuration);
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
