using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public interface Trial
{
    void Parse(string[] aVariables);
}

public class OrientationTrial : Trial
{
    public enum Actors { Anne, Jerita, Karoliina, _1Clock, _2Clock, _3Clock }
    public enum HeadDirections { Left, Right }
    public enum GazeDirections { Straight, Averted, Forward, Backward }

    static readonly string[] ACTORS = Enum.GetNames(typeof(Actors)).Select(s => s.Substring(s[0] == '_' ? 1 : 0).ToLower()).ToArray();    // new string[] { "anne", "jerita", "karoliina", "1clock", "2clock", "3clock" };
    static readonly string[] HEAD_DIRECTIONS = Enum.GetNames(typeof(HeadDirections)).Select(s => s.ToLower()).ToArray();                  // new string[] { "left", "right" };
    static readonly string[] GAZE_DIRECTIONS = Enum.GetNames(typeof(GazeDirections)).Select(s => s.ToLower()).ToArray();                  // new string[] { "straight", "averted", "forward", "backward" };

    public string Actor { get; private set; }
    public string Head { get; private set; }
    public string Gaze { get; private set; }

    public OrientationTrial() { }

    public OrientationTrial(string aActor, string aHead, string aGaze)
    {
        Set(aActor, aHead, aGaze);
    }

    public void Parse(string[] aVariables)
    {
        Set(aVariables[0], aVariables[1], aVariables[2]);
    }

    void Set(string aActor, string aHead, string aGaze)
    {
        if (!ACTORS.Contains(aActor))
        {
            throw new ArgumentException("unknown actor");
        }
        if (!HEAD_DIRECTIONS.Contains(aHead))
        {
            throw new ArgumentException("unknown head direction");
        }
        if (!GAZE_DIRECTIONS.Contains(aGaze))
        {
            throw new ArgumentException("unknown gaze direction");
        }

        Actor = aActor;
        Head = aHead;
        Gaze = aGaze;
    }
}

public class PlayingLadyTrial : Trial
{
    public enum CarColor { Red, Blue }
    public enum CarDirections { Left, Right }
    public enum GazeDirections { Down, Averted, Straight }

    static readonly int[] SLIDES = new int[] { 1, 2 };
    static readonly int[] CAR_RUNS = new int[] { 1, 2, 3 };
    static readonly string[] GAZE_DIRECTIONS = Enum.GetNames(typeof(GazeDirections)).Select(s => s.ToLower()).ToArray();  // new string[] { "down", "averted", "straight" };
    static readonly string[] CAR_DIRECTIONS = Enum.GetNames(typeof(CarDirections)).Select(s => s.ToLower()).ToArray();    // new string[] { "left", "right" };
    static readonly string[] CAR_COLORS = Enum.GetNames(typeof(CarColor)).Select(s => s.ToLower()).ToArray();             // new string[] { "red", "blue" };

    public string Gaze { get; private set; }
    public string Direction { get; private set; }
    public int Slide { get; private set; }
    public string Color { get; private set; }
    public int RunCount { get; private set; }

    public PlayingLadyTrial() { }

    public PlayingLadyTrial(string aGaze, string aCarDirection, int aSlide, string aColor, int aRunCount)
    {
        Set(aGaze, aCarDirection, aSlide, aColor, aRunCount);
    }

    public void Parse(string[] aVariables)
    {
        Set(aVariables[0], aVariables[1], int.Parse(aVariables[2]), aVariables[3], int.Parse(aVariables[4]));
    }

    void Set(string aGaze, string aCarDirection, int aSlide, string aColor, int aRunCount)
    {
        if (!GAZE_DIRECTIONS.Contains(aGaze))
        {
            throw new ArgumentException("invalid gaze direction");
        }
        if (!CAR_DIRECTIONS.Contains(aCarDirection))
        {
            throw new ArgumentException("invalid car direction");
        }
        if (!SLIDES.Contains(aSlide))
        {
            throw new ArgumentException("invalid slide ID");
        }
        if (!CAR_COLORS.Contains(aColor))
        {
            throw new ArgumentException("invalid car color");
        }
        if (!CAR_RUNS.Contains(aRunCount))
        {
            throw new ArgumentException("invalid car run count");
        }

        Gaze = aGaze;
        Direction = aCarDirection;
        Slide = aSlide;
        Color = aColor;
        RunCount = aRunCount;
    }
}


public static class Files
{
    public static readonly string FOLDER = "order";
    public static readonly char DELIMETER = '_';

    public static string[] ReadLines(string aFileName, int aBlockSize = 1)
    {
        var filename = $"{FOLDER}\\{aFileName}";

        string[] lines;
        try
        {
            using (StreamReader reader = new StreamReader(filename))
            {
                lines = reader.ReadToEnd().Split('\n').Select(line => line.Trim().ToLower()).Where(line => line.Length > 0).ToArray();
            }

            if (lines.Length % aBlockSize != 0)
            {
                throw new Exception($"The last block is not complete: each block must consist of {aBlockSize} lines");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to read '{aFileName}' file: {ex.Message}");
            return null;
        }

        return lines;
    }

}

public class Trials<T> where T : Trial, new()
{

    // public methods

    public bool HasMoreTrials { get { return _index < (_trials.Count - 1); } }
    public bool HasMoreBlockTrials { get { return _index < (_nextBlockStartIndex - 1); } }
    public bool HasMoreBlocks { get { return _index < (_trials.Count - _blockSize); } }

    public int CurrentIndex { get { return _index; } }

    public bool IsValid { get; private set; } = false;

    // internal members

    int _blockSize;
    int _variableCount;

    List<T> _trials = new List<T>();
    int _index = -1;
    int _nextBlockStartIndex = 0;

    Log _log;

    // public methods

    public Trials(string aFileName, int aBlockSize, int aVariableCount)
    {
        _blockSize = aBlockSize;
        _variableCount = aVariableCount;

        _log = GameObject.FindObjectOfType<Log>();

        string[] lines = Files.ReadLines(aFileName, aBlockSize);
        if (lines == null)
        {
            return;
        }

        foreach (var line in lines)
        {
            if (line.Length == 0)
            {
                continue;
            }

            var variables = line.Split(Files.DELIMETER);
            try
            {
                if (variables.Length != _variableCount)
                {
                    throw new Exception($"the line must consist of {_variableCount} variable separated by '{Files.DELIMETER}'");
                }
                else
                {
                    var item = new T();
                    item.Parse(variables);
                    _trials.Add(item);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Invalid line '{line}' in '{aFileName}': {ex.Message}");
                return;
            }
        }

        IsValid = true;
        _log.Loaded(aFileName);
    }

    public T StartBlock()
    {
        _nextBlockStartIndex += _blockSize;
        return Next();
    }

    public T Next()
    {
        var index = _index + 1;
        if (index < _trials.Count && index < _nextBlockStartIndex)
        {
            // _log.NextTrial(_index);
            _index = index;
            return _trials[_index];
        }

        return default(T);
    }

    public void Reset()
    {
        _index = -1;
    }
}
