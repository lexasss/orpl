using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GazeClient : MonoBehaviour
{
    // to be set in inspector

    public bool simulate;
    public bool useTobiiSDK = true;

    public GameObject gazeControls;
    public Button options;
    public Button calibrate;
    public Button toggleTracking;
    public Text deviceName;
    public Text tobiiModel;
    public Button tobiiToggleTracking;
    public Dropdown tobiiEye;
    public Text debug;
    public GameObject etudControls;
    public GameObject tobiiControls;

    // public members

    public event EventHandler Start = delegate { };
    public event EventHandler Stop = delegate { };
    public event EventHandler State = delegate { };
    public event EventHandler Sample = delegate { };

    public bool isTracking { get; private set; } = false;
    public GazeIO.Sample lastSample { get; private set; }
    public RawPoint location { get; private set; } = new RawPoint(0, 0f, 0f);

    // internal members

    bool _simulated { get { return simulate/* || Environment.UserName == "olequ"*/; } }

    TobiiClient _tobii = null;
    WebSocketSharp.WebSocket _ws = null;
    GazeSimulator _simulator = null;
    Log _log;

    Queue<string> _messages = new Queue<string>();
    Vector2 _scale = new Vector2(1f, 1f);
    Vector2 _offset = new Vector2(0f, 0f);
    Smoother<RawPoint> _smoother;
    bool _hasInitiatedTracking = false;
    bool _trackingInitialized = false;

    // overrides

    void Awake()
    {
        gazeControls.SetActive(true);

        etudControls.SetActive(!useTobiiSDK);
        tobiiControls.SetActive(useTobiiSDK);

        _log = FindObjectOfType<Log>();

        _smoother = new Smoother<RawPoint>();
        _smoother.saccadeThreshold = 30;
        _smoother.timeWindow = 150;
        _smoother.dampFixation = 700;

        if (_simulated)
        {
            _simulator = FindObjectOfType<GazeSimulator>();
            _simulator.Sample += onSimulatorSample;
            _simulator.State += onSimulatorState;
            _simulator.Device += onSimulatorDevice;
            _simulator.Initialize();
            return;
        }

        if (useTobiiSDK)
        {
            _tobii = GetComponent<TobiiClient>();
            _tobii.Error += onTobiiError;
            _tobii.Ready += onTobiiReady;
            _tobii.Toggled += onTobiiToggled;
            _tobii.Data += onTobiiData;

            tobiiEye.value = (int)_tobii.eye;
        }
        else
        {
            _ws = new WebSocketSharp.WebSocket("ws://localhost:8086/");
            _ws.OnOpen += (sender, e) =>
            {
                print("WS:> Connected");
            };
            _ws.OnClose += (sender, e) =>
            {
                print("WS:> Disconnected");
            };
            _ws.OnError += (sender, e) =>
            {
                print($"WS:> Error {e.Message}");
            };
            _ws.OnMessage += (sender, e) =>
            {
            //print($"WS:> MSG {e.Data}");
            lock (_messages)
                {
                    _messages.Enqueue(e.Data);
                }
            };

            _ws.ConnectAsync();
        }
    }

    void Update()
    {
        lock (_messages)
        {
            while (_messages.Count > 0)
            {
                ParseMessage(_messages.Dequeue());
            }
        }
    }

    void OnDestroy()
    {
        if (_ws != null)
        { 
            _ws.Close();
        }
        if (_tobii != null)
        {
            _tobii.Close();
            _tobii = null;
        }
    }

    void OnApplicationQuit()
    {
        if (_ws != null && isTracking && _hasInitiatedTracking)
        {
            _ws.Send(GazeIO.Request.toggleTracking);
        }
        if (_tobii != null)
        {
            _tobii.Close();
            _tobii = null;
        }
    }

    // public methods

    public void ShowOptions()
    {
        if (!_simulated && !useTobiiSDK)
            _ws.Send(GazeIO.Request.showOptions);
    }

    public void Calibrate()
    {
        if (!_simulated && !useTobiiSDK)
            _ws.Send(GazeIO.Request.calibrate);
    }

    public void ToggleTracking()
    {
        if (_simulated)
        {
            _simulator.ToggleTracking();
        }
        else if (_tobii != null)
        {
            _tobii.ToggleTracking();
        }
        else if (_ws != null)
        {
            if (!isTracking)
            {
                _hasInitiatedTracking = true;
            }
            _ws.Send(GazeIO.Request.toggleTracking);
        }
    }

    public void TobiiSetEye()
    {
        if (_tobii != null)
            _tobii.eye = (TobiiClient.Eye)tobiiEye.value;
    }

    public void HideUI()
    {
        gazeControls.SetActive(false);
        Cursor.visible = false;
    }

    public void ShowUI()
    {
        gazeControls.SetActive(true);
        Cursor.visible = true;
    }

    // internal methods

    void ParseMessage(string aMessage)
    {
        GazeIO.Sample sample = JsonUtility.FromJson<GazeIO.Sample>(aMessage);
        if (sample.isValid)
        {
            lastSample = sample;
            //print($"WS:> sample = {sample.x}, {sample.y}");
            UpdateCursorLocation(sample);
            return;
        }

        GazeIO.State state = JsonUtility.FromJson<GazeIO.State>(aMessage);
        if (state.isValid)
        {
            //print($"WS:> status = {state.value}");
            UpdateState(state);
            return;
        }

        GazeIO.Device device = JsonUtility.FromJson<GazeIO.Device>(aMessage);
        if (device.isValid)
        {
            //print($"WS:> device name = {device.name}");
            UpdateDeviceInfo(device);
            return;
        }
    }

    void UpdateDeviceInfo(GazeIO.Device aDevice)
    {
        deviceName.text = aDevice.name;
    }

    void UpdateState(GazeIO.State aState)
    {
        bool trackingChanged = aState.isTracking != isTracking;

        isTracking = aState.isTracking;

        // gaze ui and controls
        options.interactable = !isTracking && !aState.isBusy;
        calibrate.interactable = !isTracking && aState.isConnected && !aState.isBusy;
        toggleTracking.interactable = aState.isConnected && aState.isCalibrated && !aState.isBusy && !isTracking;
        toggleTracking.GetComponentInChildren<Text>().text = isTracking ? "Stop" : "Start";
        //gazeControls.SetActive(!isTracking);

        State(this, new EventArgs());

        if (isTracking && !_trackingInitialized)
        {
            InitializeTracking();
            _log.ClearEvents();
        }

        if (trackingChanged)
        {
            // input module
            GetComponent<StandaloneInputModule>().enabled = !isTracking;

            if (isTracking)
            {
                Start(this, new EventArgs());
            }
            else
            {
                Stop(this, new EventArgs());
            }
        }
    }

    void UpdateCursorLocation(GazeIO.Sample aSample)
    {
        Vector2 location = GazeToGameWindow(aSample);

        this.location = _smoother.Feed(new RawPoint(aSample.ts, location.x, location.y));
        // debug.text = $"S = {aSample.x:N0} {aSample.y:N0}; F = {this.location.x:N0} {this.location.y:N0}";

        Sample(this, new EventArgs());
    }

    Vector2 GazeToGameWindow(GazeIO.Sample aSample)
    {
        return new Vector2(
            aSample.x - Screen.width / 2 - _offset.x,
            Screen.height / 2 - (aSample.y - _offset.y)
        );
    }

    void InitializeTracking()
    {
        Rect rc = Camera.main.pixelRect;
        _scale.x = rc.width / Screen.currentResolution.width;
        _scale.y = rc.height / Screen.currentResolution.height;

        try
        {
            rc = Utils.GetWindowRect();

            _offset = new Vector2(
                rc.x + (rc.width - Screen.width) / 2,
                rc.y + (rc.height - Screen.height) / 2 + (Application.isEditor ? 17 : 0) // toolbar
            );
        }
        catch (Exception) { }

        _trackingInitialized = true;
    }

    // Tobii
    private void onTobiiError(object sender, string error)
    {
        deviceName.text = error;
        print($"TOBII:> ERROR: {error}");
    }

    private void onTobiiReady(object sender, string model)
    {
        deviceName.text = model;
        tobiiToggleTracking.interactable = true;

        if (!_trackingInitialized)
        {
            InitializeTracking();
        }
    }

    private void onTobiiToggled(object sender, bool isTracking)
    {
        tobiiToggleTracking.GetComponentInChildren<Text>().text = isTracking ? "Stop" : "Start";

        if (isTracking)
        {
            _log.ClearEvents();
            Start(this, new EventArgs());
        }
        else
        {
            Stop(this, new EventArgs());
        }
    }

    private void onTobiiData(object sender, GazeIO.Sample sample)
    {
        lastSample = sample;
        UpdateCursorLocation(sample);
    }

    // Simulator
    void onSimulatorDevice(object aHandler, GazeSimulator.DeviceArgs aArgs)
    {
        if (useTobiiSDK)
        {
            onTobiiReady(null, aArgs.device.name);
        }
        else
        {
            UpdateDeviceInfo(aArgs.device);
        }
    }

    void onSimulatorState(object aHandler, GazeSimulator.StateArgs aArgs)
    {
        if (useTobiiSDK)
        {
            onTobiiToggled(null, aArgs.state.isTracking);
        }
        else
        {
            UpdateState(aArgs.state);
        }
    }

    void onSimulatorSample(object aHandler, GazeSimulator.SampleArgs aArgs)
    {
        lastSample = aArgs.sample;
        UpdateCursorLocation(aArgs.sample);
    }
}
