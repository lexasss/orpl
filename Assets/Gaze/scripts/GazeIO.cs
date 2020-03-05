using System;

/**
 * <summary>Definitions of structures and constants used in communication with ETUDriver service</summary>
 * */
namespace GazeIO
{
    /** <summary>State values</summary> */
    [Flags]
    public enum StateValue : int
    {
        Connected = 0x01,
        Calibrated = 0x02,
        Tracking = 0x04,
        Busy = 0x08,
    }

    /** <summary>Request string</summary> */
    public static class Request
    {
        public static string showOptions { get { return "SHOW_OPTIONS"; } }
        public static string calibrate { get { return "CALIBRATE"; } }
        public static string toggleTracking { get { return "TOGGLE_TRACKING"; } }
        public static string setDevice { get { return "SET_DEVICE"; } }
        public static string passValue { get { return "PASS_VALUE"; } }
    }

    /** <summary>Content of the "type" field of JSON object received from the server</summary> */
    public static class MessageType
    {
        public static string sample { get { return "sample"; } }
        public static string state { get { return "state"; } }
        public static string device { get { return "device"; } }
        public static string custom { get { return "custom"; } }
    }

    /** <summary>interface for the JSON message received from the server</summary> */
    interface Message
    {
        /** <summary>Returns true if the "type" field of JSON object corresponds to the structure that implement this interface</summary> */
        bool isValid { get; }
    }

    /** <summary>Message with a device name</summary> */
    public class Device : Message
    {
        public string type = "";
        /** <summary>Device name</summary> */
        public string name = "";

        public bool isValid { get { return type == MessageType.device; } }
    }

    /** <summary>Message with the ETUDriver state</summary> */
    public class State : Message
    {
        public string type = "";
        /** <summary>State flags. Use "isXxxx" properties to get the value of a particular state flag</summary> */
        public int value = -1;

        public bool isValid { get { return type == MessageType.state; } }

        public bool isConnected { get { return (value & (int)StateValue.Connected) > 0; } }
        public bool isCalibrated { get { return (value & (int)StateValue.Calibrated) > 0; } }
        public bool isTracking { get { return (value & (int)StateValue.Tracking) > 0; } }
        public bool isBusy { get { return (value & (int)StateValue.Busy) > 0; } }
    }

    /** <summary>Message with a gaze sample</summary> */
    public class Sample : Message
    {
        /** <summary>Eyes in tracker's camera view</summary> */
        public class EyesInCamera
        {
            /** <summary>Left eye X</summary> */
            public float xl = .0f;
            /** <summary>Left eye Y</summary> */
            public float yl = .0f;
            /** <summary>Right eye X</summary> */
            public float xr = .0f;
            /** <summary>Right eye Y</summary> */
            public float yr = .0f;
        }

        public string type = "";
        /** <summary>Timestamp, ms</summary> */
        public ulong ts = 0;
        /** <summary>Gaze X</summary> */
        public float x = .0f;
        /** <summary>Gaze Y</summary> */
        public float y = .0f;
        /** <summary>Pupil size</summary> */
        public float p = .0f;
        /** <summary>Eyes in camera view</summary> */
        public EyesInCamera ec = new EyesInCamera();

        public bool isValid { get { return type == MessageType.sample; } }
    }
}
