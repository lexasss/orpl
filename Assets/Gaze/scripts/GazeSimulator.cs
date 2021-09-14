using System;
using UnityEngine;

public class GazeSimulator : MonoBehaviour
{
    // static

    public static readonly float SAMPLING_INTERVAL = 0.03333f;
    public static readonly int TOOLBAR_HEIGHT = 17;

    // definitions

    public class SampleArgs : EventArgs
    {
        public readonly GazeIO.Sample sample;
        public SampleArgs(ulong aTimestamp, float aX, float aY, float aPupil)
        {
            sample = new GazeIO.Sample();
            sample.type = GazeIO.MessageType.sample;
            sample.ts = aTimestamp;
            sample.x = aX;
            sample.y = aY;
            sample.p = aPupil;
        }
    }

    public class StateArgs : EventArgs
    {
        public readonly GazeIO.State state;
        public StateArgs(GazeIO.State aState)
        {
            state = aState;
        }
    }

    public class DeviceArgs : EventArgs
    {
        public readonly GazeIO.Device device;
        public DeviceArgs(string aDeviceName)
        {
            device = new GazeIO.Device();
            device.type = GazeIO.MessageType.device;
            device.name = aDeviceName;
        }
    }

    // public methods

    public event EventHandler<SampleArgs> Sample = delegate { };
    public event EventHandler<StateArgs> State = delegate { };
    public event EventHandler<DeviceArgs> Device = delegate { };

    public bool Enabled { get; private set; } = false;

    // internal members

    GazeIO.State _state = new GazeIO.State();
    Vector2 _offset;
    ulong _timeStamp = 0;

    // overrides

    void Awake()
    {
        Rect rc = Utils.GetWindowRect();

        _offset = new Vector2(
            rc.x + (rc.width - Screen.width) / 2,
            rc.y + (rc.height - Screen.height) / 2 + TOOLBAR_HEIGHT
        ); ;

        _state.type = GazeIO.MessageType.state;
        _state.value = (int)GazeIO.StateValue.Connected | (int)GazeIO.StateValue.Calibrated;
    }

    // public methods

    public void ToggleTracking()
    {
        if ((_state.value & (int)GazeIO.StateValue.Tracking) == 0)
        {
            _state.value |= (int)GazeIO.StateValue.Tracking;
            InvokeRepeating(nameof(EmitSample), SAMPLING_INTERVAL, SAMPLING_INTERVAL);
        }
        else
        {
            _state.value &= ~(int)GazeIO.StateValue.Tracking;
            _timeStamp = 0;
            CancelInvoke();
        }

        State(this, new StateArgs(_state));
    }

    public void Initialize()
    {
        Enabled = true;
        Device(this, new DeviceArgs("Simulator"));
        State(this, new StateArgs(_state));
    }

    // internal methods

    void EmitSample()
    {
        _timeStamp += (ulong)(SAMPLING_INTERVAL * 1000);

        float x, y;
        MouseToGaze(out x, out y);
        
        Sample(this, new SampleArgs(_timeStamp, x, y, 6.0f));
    }

    void MouseToGaze(out float aX, out float aY)
    {
        aX = Input.mousePosition.x + _offset.x;
        aY = (Screen.height - Input.mousePosition.y) + _offset.y;
    }
}
