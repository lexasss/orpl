using System;
using System.Text;
using System.Net.Sockets;
using System.Collections.Generic;
using UnityEngine;

public class NetStation : MonoBehaviour
{
    // to be set in inspector

    public string Host = "10.0.0.42";
    public int Port = 55513;

    // definitions

    const int MAX_SYCH_ITERATIONS = 5; // 100
    const float SYNC_LIMIT = 0.0025f;   // seconds

    public enum State
    {
        NOT_INITIALIZED = 0,
        NOT_CONNECTED,
        CONNECTING,
        CONNECTED,
        FAILED_TO_CONNECT,
        FAILED_TO_CHECK_ECI_VERSION,
        UNSUPPORTED_ECI_VERIONS,
        SYNCH_ING,
        READY,
    }

    public class StateChangedEventArgs : EventArgs
    {
        public State State { get; private set; }
        public string Message { get; private set; }
        public StateChangedEventArgs(State aState, string aMessage)
        {
            State = aState;
            Message = aMessage;
        }
    }

    // public members

    public bool IsConnected { get { return _tcp != null ? _tcp.Connected : false; } }
    public bool IsReady { get; private set; } = false;

    public event EventHandler<StateChangedEventArgs> Message = delegate { };

    public float Timestamp { get { return _syncEpoch == 0f ? -1f : Time.time - _syncEpoch; } }

    // internal members

    TcpClient _tcp;
    NetworkStream _stream;

    float _syncEpoch = 0;
    bool _isRecording = false;

    // public methods

    ~NetStation()
    {
        Disconnect();
    }

    public void Connect()
    {
        if (_tcp != null && _tcp.Connected)
        {
            return;
        }

        _tcp = new TcpClient();

        try
        {
            Debug.Log("NS: Connecting");
            _tcp.BeginConnect(Host, Port, new AsyncCallback(ConnectCallback), null);
        }
        catch (Exception ex)
        {
            Debug.LogError($"NS: {ex.Message}");
            Message(this, new StateChangedEventArgs(State.FAILED_TO_CONNECT, "failed to connect"));
        }
    }

    public void Debug1()
    {
        _isRecording = true;
        Event("OrPLs");
    }

    public void Begin()
    {
        if (!_isRecording && IsConnected)
        {
            SndAndRcv("B");
            _isRecording = true;
        }
    }

    public void End()
    {
        if (_isRecording)
        {
            Wait(.5f);
            SndAndRcv("E");
            _isRecording = false;
        }
    }

    public void Disconnect()
    {
        if (IsConnected)
        {
            End();

            Wait(1f);
            SndAndRcv("X");

            Wait(.5f);

            _tcp.Close();
            Debug.Log("NS: Disconnect");
            Message(this, new StateChangedEventArgs(State.NOT_CONNECTED, "not connected"));
        }
    }

    public void Event(string aMessage)
    {
        if (!_isRecording)
        {
            return;
        }

        var msg = aMessage.PadRight(4);
        if (msg.Length > 4)
        {
            Debug.LogWarning("NS: NetStation accepts messages 4 char max!");
            msg = aMessage.Substring(0, 4);
        }

        var start = Timestamp;
        uint duration = 1;

        /*
        byte[][] data = new byte[][] { BitConverter.GetBytes((UInt16)15),
            BitConverter.GetBytes((Int32)(start * 1000)),
            BitConverter.GetBytes((UInt32)duration),
            Encoding.ASCII.GetBytes(msg),
            BitConverter.GetBytes((Int16)0),
            new byte[] { 0 }
        };

        List<byte> bytes = new List<byte>();
        bytes.Add((byte)'D');

        foreach( var s in data )
        {
            for (int i = s.Length - 1; i >=0; i--)
            {
                bytes.Add(s[i]);
            }
        }
        */

        List<byte> bytes = new List<byte>();
        bytes.Add((byte)'D');
        bytes.AddNet((short)15);
        bytes.AddNet((int)(start * 1000));
        bytes.AddNet((int)duration);
        bytes.AddRange(Encoding.ASCII.GetBytes(msg));
        bytes.AddNet((short)0);
        bytes.Add(0);

        SndAndRcv(bytes.ToArray());
    }

    // internal methods

    void CheckVersion()
    {
        if (!IsConnected)
        {
            return;
        }

        byte result = SndAndRcv("QMAC-");
        if (result == 'F')
        {
            var message = "version cannot be checked";
            ThreadDispatcher.RunOnMainThread(() =>
            {
                Message(this, new StateChangedEventArgs(State.FAILED_TO_CHECK_ECI_VERSION, message));
            });
            throw new Exception(message);
        }
        else if (result == 'I')
        {
            byte[] buffer = new byte[256];
            _stream.Read(buffer, 0, 1);

            var version = buffer[0];
            if (version != 1 && version != '1')
            {
                var message = $"unknown version: {(char)version} (\\x{version})";
                ThreadDispatcher.RunOnMainThread(() =>
                {
                    Message(this, new StateChangedEventArgs(State.UNSUPPORTED_ECI_VERIONS, message));
                });
                throw new Exception(message);
            }
        }
    }

    void Sync()
    {
        if (!IsConnected)
        {
            return;
        }

        Message(this, new StateChangedEventArgs(State.SYNCH_ING, "sync'ing"));

        _syncEpoch = Time.time;

        float df = 10000;
        int iteration = 0;
        while (df > SYNC_LIMIT && iteration++ < MAX_SYCH_ITERATIONS)
        {
            SndAndRcv("A");

            var now = Timestamp;

            List<byte> bytes = new List<byte>();
            bytes.Add((byte)'T');
            bytes.AddNet((Int32)(now * 1000));
            SndAndRcv(bytes.ToArray());

            var ack = Timestamp;
            df = ack - now;
        }

        if (iteration >= MAX_SYCH_ITERATIONS)
        {
            Debug.LogWarning($"NS SYNC: synchronization did not succeed within {SYNC_LIMIT} ms. Synchronizatoin accuracy is {df} ms");
        }

        IsReady = true;

        Message(this, new StateChangedEventArgs(State.READY, "ready" + (iteration >= MAX_SYCH_ITERATIONS ? " (low sync accuracy)" : "")));
    }


    byte SndAndRcv(string aMessage)
    {
        return SndAndRcv(Encoding.ASCII.GetBytes(aMessage));
    }

    byte SndAndRcv(byte[] aData)
    {
        if (!IsConnected)
        {
            return 0;
        }

        _stream.Write(aData, 0, aData.Length);

        List<string> msg = new List<string>();
        foreach (var b in aData) { msg.Add(b.ToString("X").PadLeft(2, '0')); }
        Debug.Log($"sent '{String.Join(" ", msg)}', waiting for reply...");

        byte[] buffer = new byte[16];
        _stream.Read(buffer, 0, 1);

        Debug.Log("  ok");

        return buffer[0];
    }

    void Wait(float aSeconds)
    {
        System.Threading.Thread.Sleep((int)(aSeconds * 1000));
    }

    void ConnectCallback(IAsyncResult ar)
    {
        try
        {
            _tcp.EndConnect(ar);
            _stream = _tcp.GetStream();
            
            Debug.Log($"NS CON: Socket connected to {_tcp.Client.RemoteEndPoint.ToString()}");
            ThreadDispatcher.RunOnMainThread(() => {
                Message(this, new StateChangedEventArgs(State.CONNECTED, "connected, checking version..."));
            });

            CheckVersion();

            ThreadDispatcher.RunOnMainThread(() =>
            {
                Sync();
            });
        }
        catch (Exception ex)
        {
            Debug.LogError($"NS CON: {ex.Message}");
            ThreadDispatcher.RunOnMainThread(() => {
                Message(this, new StateChangedEventArgs(State.FAILED_TO_CONNECT, $"failed to connect: {ex.Message}"));
            });

            _tcp.Close();
        }
    }
}
