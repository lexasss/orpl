using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PupuTask : MonoBehaviour
{
    // to set in inspector

    public AudioSource pupuAudioDirect;
    public AudioSource pupuAudioAvert;
    public Image curtain;
    public Color flashColor = new Color(1f, 1f, 1f);
    public float AudioDelay = 5f;
    public float SignalPause = 30f;
    public float FlashDuration = 0.2f;

    // vars

    HRClient _hrClient;
    AudioSource _audioToFinish = null;
    Button _pressedButton = null;
    bool _isWaitingForFinish = false;
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

                _pressedButton.interactable = true;
                _pressedButton = null;

                _hrClient.StopPupu(_audioToFinish == pupuAudioDirect ? "d" : "a");
                CancelInvoke("Signal");

                _audioToFinish = null;
                curtain.gameObject.SetActive(false);
            }
        }
    }

    public void StartPupuDirectTask()
    {
        _audioToFinish = pupuAudioDirect;
        _pressedButton = FindObjectsOfType<Button>().Single(btn => btn.name.Contains("Direct"));
        _pressedButton.interactable = false;

        Invoke("StartListeningForFinish", AudioDelay);  
    }

    public void StartPupuAvertTask()
    {
        _audioToFinish = pupuAudioAvert;
        _pressedButton = FindObjectsOfType<Button>().Single(btn => btn.name.Contains("Avert"));
        _pressedButton.interactable = false;

        Invoke("StartListeningForFinish", AudioDelay);
    }

    private void StartListeningForFinish()
    {
        _isWaitingForFinish = true;
        _hrClient.StartPupu(_audioToFinish == pupuAudioDirect ? "d" : "a");
        _audioToFinish.Play();

        _isAvertSignal = _audioToFinish != pupuAudioDirect;
        Invoke("Signal", SignalPause);

        curtain.gameObject.SetActive(true);
    }

    private void Signal()
    {
        if (_isAvertSignal)
        {
            _hrClient.PupuAvert();
        }
        else
        {
            _hrClient.PupuDirect();
        }

        curtain.color = flashColor;

        _isAvertSignal = !_isAvertSignal;
        Invoke("Signal", SignalPause);
        Invoke("SetCurtain", FlashDuration);
    }

    private void SetCurtain()
    {
        curtain.color = _curtainColor;
    }
}
