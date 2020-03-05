using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GazeClient : MonoBehaviour
{
    // to be set in inspector

    public bool simulate;

    public GameObject gazeControls;
    public Button options;
    public Button calibrate;
    public Button toggleTracking;
    public Text deviceName;

    // public members

    public event EventHandler Start = delegate { };
    public event EventHandler Stop = delegate { };
    public event EventHandler State = delegate { };
    public event EventHandler Sample = delegate { };

    public bool isTracking { get; private set; } = false;
    public GazeIO.Sample lastSample { get; private set; }
    public RawPoint location { get; private set; } = new RawPoint(0, 0f, 0f);
    bool simulated { get { return simulate/* || Environment.UserName == "olequ"*/; } }

    // internal members

    WebSocketSharp.WebSocket _ws = null;
    GazeSimulator _simulator = null;

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

        _smoother = new Smoother<RawPoint>();
        _smoother.saccadeThreshold = 30;
        _smoother.timeWindow = 150;
        _smoother.dampFixation = 700;

        if (simulated)
        {
            _simulator = FindObjectOfType<GazeSimulator>();
            _simulator.Sample += onSimulatorSample;
            _simulator.State += onSimulatorState;
            _simulator.Device += onSimulatorDevice;
            _simulator.Initialize();
            return;
        }

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
            _ws.Close();
    }

    void OnApplicationQuit()
    {
        if (_ws != null && isTracking && _hasInitiatedTracking)
            _ws.Send(GazeIO.Request.toggleTracking);
    }

    // public methods

    public void ShowOptions()
    {
        if (!simulated)
            _ws.Send(GazeIO.Request.showOptions);
    }

    public void Calibrate()
    {
        if (!simulated)
            _ws.Send(GazeIO.Request.calibrate);
    }

    public void ToggleTracking()
    {
        if (!isTracking)
        {
            _hasInitiatedTracking = true;
        }

        if (simulated)
            _simulator.ToggleTracking();
        else
            _ws.Send(GazeIO.Request.toggleTracking);
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
        toggleTracking.interactable = aState.isConnected && aState.isCalibrated && !aState.isBusy;
        toggleTracking.GetComponentInChildren<Text>().text = isTracking ? "Stop" : "Start";
        gazeControls.SetActive(!isTracking);

        State(this, new EventArgs());

        if (isTracking && !_trackingInitialized)
            InitializeTracking();

        if (trackingChanged)
        {
            // input module
            GetComponent<StandaloneInputModule>().enabled = !isTracking;

            if (isTracking)
                Start(this, new EventArgs());
            else
                Stop(this, new EventArgs());
        }
    }

    void UpdateCursorLocation(GazeIO.Sample aSample)
    {
        Vector2 location = GazeToGameWindow(aSample);

        this.location = _smoother.Feed(new RawPoint(aSample.ts, location.x, location.y));

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
                rc.y + (rc.height - Screen.height) / 2 + 17 // toolbar
            );
        }
        catch (Exception) { }

        _trackingInitialized = true;
    }

    // Simulator
    void onSimulatorDevice(object aHandler, GazeSimulator.DeviceArgs aArgs)
    {
        UpdateDeviceInfo(aArgs.device);
    }

    void onSimulatorState(object aHandler, GazeSimulator.StateArgs aArgs)
    {
        UpdateState(aArgs.state);
    }

    void onSimulatorSample(object aHandler, GazeSimulator.SampleArgs aArgs)
    {
        lastSample = aArgs.sample;
        UpdateCursorLocation(aArgs.sample);
    }
}
