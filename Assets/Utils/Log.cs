using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;

public class Log : MonoBehaviour
{
    // definitions

    const string DELIMITER = "\t";

    // internal members

    GazeClient _gazeClient;
    NetStation _netStation;
    StreamWriter _writer;
    List<string> _lastEvents = new List<string>();
    ulong _gazeTimestamp = 0;

    // overrides
    void Start()
    {
        var now = DateTime.Now;
        var date = String.Join("-", (new int[] { now.Year, now.Month, now.Day }).Select(num => Pad(num, 2)));
        var time = String.Join("-", (new int[] { now.Hour, now.Minute, now.Second }).Select(num => Pad(num, 2)));

        _writer = new StreamWriter($"log/log_{date}_{time}.txt");

        _gazeClient = FindObjectOfType<GazeClient>();
        _gazeClient.Sample += onGazeSample;

        _netStation = FindObjectOfType<NetStation>();
    }

    // public methods

    public void HR(string aEvent)
    {
        PushEvent(aEvent);
    }

    public void Dbg(string aMessage)
    {
        WriteLine("Debug", aMessage);
    }

    public void Error(string aError)
    {
        WriteLine("Error", aError);
    }

    public void Loaded(string aFileName)
    {
        WriteLine("Loaded", aFileName );
    }

    public void StartBlock(string aType)
    {
        PushEvent($"Block {aType}");
    }

    public void NextTrial(int aIndex)
    {
        PushEvent($"Trial {aIndex + 1}");
    }

    public void TrialFinished(int aIndex)
    {
        PushEvent($"Trial {aIndex + 1} finished");
    }

    public void Restart()
    {
        PushEvent("Restart");
    }

    public void ClearEvents()
    {
        lock (_lastEvents)
        {
            _lastEvents.Clear();
        }
    }

    public void Close()
    {
        _gazeClient.Sample -= onGazeSample;

        lock (_lastEvents)
        {
            foreach (var e in _lastEvents)
            {
                WriteLine(e);
            }
            _lastEvents.Clear();
        }

        WriteLine("Finished");

        _writer.Close();
    }

    // internal methods

    void PushEvent(string aEvent)
    {
        lock (_lastEvents)
        {
            _lastEvents.Add(aEvent);
        }
        Debug.Log($"EVT: {aEvent}");
    }

    void WriteLine(string aTitle, string aMessage = "")
    {
        if (_writer != null)
        {
            _writer.WriteLine(String.Join(DELIMITER, new string[] { _netStation.Timestamp.ToString(), _gazeTimestamp.ToString(), aTitle, aMessage }));
            Debug.Log($"{aTitle}: {aMessage}");
        }
        else
        {
            PushEvent($"{aTitle} {aMessage}");
        }
    }

    string Pad(int aValue, int aMinLength)
    {
        string result = aValue.ToString();
        return result.PadLeft(aMinLength, '0');
    }

    void onGazeSample(object sender, EventArgs e)
    {
        var sample = _gazeClient.lastSample;
        _gazeTimestamp = sample.ts;

        lock (_lastEvents)
        {
            var events = String.Join(", ", _lastEvents);
            var fields = new string[] {
                _netStation.Timestamp.ToString(),
                sample.ts.ToString(),
                sample.x.ToString(),
                sample.y.ToString(),
                sample.p.ToString(),
                events,
            };

            _writer.WriteLine(String.Join(DELIMITER, fields));

            _lastEvents.Clear();
        }
    }
}
