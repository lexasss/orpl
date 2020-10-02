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
    public float AudioDelay = 5f;
    public float SignalPause = 30f;
    public float FlashDuration = 0.2f;

    // vars

    HRClient _hrClient;
    AudioSource _audioToFinish = null;
    bool _isWaitingForFinish = false;
    char _avatarID;
    char _typeID;
    bool _isAvertSignal;
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

                _hrClient.StopAvatarTask(_avatarID, _typeID);
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
        _typeID = 'd';

        Invoke("StartListeningForFinish", AudioDelay);

        curtain.gameObject.SetActive(true);
    }

    public void StartPupuAvertTask()
    {
        _audioToFinish = pupuAudioAvert;
        _avatarID = 'P';
        _typeID = 'a';

        Invoke("StartListeningForFinish", AudioDelay);

        curtain.gameObject.SetActive(true);
    }

    public void StartNalleDirectTask()
    {
        _audioToFinish = nalleAudioDirect;
        _avatarID = 'N';
        _typeID = 'd';

        Invoke("StartListeningForFinish", AudioDelay);

        curtain.gameObject.SetActive(true);
    }

    public void StartNalleAvertTask()
    {
        _audioToFinish = nalleAudioAvert;
        _avatarID = 'N';
        _typeID = 'a';

        Invoke("StartListeningForFinish", AudioDelay);

        curtain.gameObject.SetActive(true);
    }

    private void StartListeningForFinish()
    {
        _isWaitingForFinish = true;

        _hrClient.StartAvatarTask(_avatarID, _typeID);

        _audioToFinish.Play();

        _isAvertSignal = _typeID == 'a';
        Invoke("Signal", SignalPause);

        curtain.color = flashColor;
        Invoke("SetCurtain", FlashDuration);
    }

    private void Signal()
    {
        _isAvertSignal = !_isAvertSignal;
        _hrClient.AvatarChangeInteraction(_avatarID, _isAvertSignal ? 'a' : 'd');

        Invoke("Signal", SignalPause);

        curtain.color = flashColor;
        Invoke("SetCurtain", FlashDuration);
    }

    private void SetCurtain()
    {
        curtain.color = _curtainColor;
    }
}
