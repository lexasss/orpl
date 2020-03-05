using System;
using System.Collections.Generic;
using UnityEngine;

/**
  * <summary>Smoothes gaze point.
  * Smoothing can be controlling via <see cref="dampFixation"/> and <see cref="dampSaccade"/> (smoothing strenghs) parameters.
  * <see cref="timeWindow"/> should be long enough to contain at least 6 samples.
  * <see cref="saccadeThreshold"/> affects the fixation/saccade damping mode: 
  * with smaller values saccade damping (mild smoothing) is applied more often.
  * </summary>
  * <typeparam name="T">Data type (point or vector)</typeparam>
  */
public class Smoother<T> where T : IRawData
{
    /** <summary>Gaze state</summary> */
    private enum GazeState
    {
        /** <summary>gaze state is not know yet</summary> */
        Unknown,
        /** Gaze is in the fixation state */
        Fixation,
        /** Gaze is in the saccade state */
        Saccade
    }

    /** <summary>Data damping (smoothing strength) during fixations. Default is 300.</summary> */
    public uint dampFixation { get; set; } = 100;

    /** <summary>Data damping (smoothing strength) during saccades. Default is 10</summary> */
    public uint dampSaccade { get; set; } = 1;

    /** <summary>Buffer time window, ms. Should be long enough to contain at least 6 samples</summary> */
    public uint timeWindow { get; set; } = 100;

    /**
      * <summary>Distance of the average values of two parts of the buffer are compared against this threshold to determine the gaze state
      * (fixation / saccade) and therefore smoothing strength.</summary>
      */
    public double saccadeThreshold { get; set; } = 0.02;

    /** <summary>Sampling interval, ms. If the interval is set to 0 initially (default) then it is computed automatically.</summary> */
    public ulong interval { get; set; } = 0u;

    #region Internal variables

    private Queue<T> _buffer = new Queue<T>();
    private bool _isBufferFull = false;
    private T _current = default(T);
    private GazeState _state = GazeState.Fixation;

    private System.Reflection.MethodInfo Copier = typeof(T).GetMethod("copyFrom", new Type[] { typeof(T) });

    private uint damp { get { return _state == GazeState.Fixation ? dampFixation : dampSaccade; } }

    #endregion

    /** <summary>Resets the internal state</summary> */
    public void Reset()
    {
        _buffer.Clear();
        _isBufferFull = false;
        _current = default(T);
        interval = 0u;
        _state = GazeState.Unknown;
    }

    /**
      * <summary>Take raw data and outputs smoothed data</summary>
      * <param name="aData">Data to smooth</param>
      * <returns>Smoothed data</returns>
      * */
    public T Feed(T aData)
    {
        bool isBufferFull = AddToBuffer(aData);
        if (!isBufferFull)
        {
            _current = (T)Copier.Invoke(null, new object[] { aData });
            return _current;
        }

        _state = EstimateState();
        if (_state == GazeState.Unknown)
        {
            _current = (T)Copier.Invoke(null, new object[] { aData });
            return _current;
        }

        if (interval == 0u)
        {
            interval = EstimateInterval(aData);
            _current = (T)Copier.Invoke(null, new object[] { aData });
        }

        double alfa = (double)damp / interval;
        _current.shift(aData, alfa, aData.timestamp);

        return _current;
    }

    #region Internal methods

    private bool AddToBuffer(T aData)
    {
        _buffer.Enqueue(aData);

        ulong firstTimestamp = _buffer.Peek().timestamp;

        if (!_isBufferFull)
            _isBufferFull = aData.timestamp - firstTimestamp >= timeWindow && _buffer.Count > 3;

        while (aData.timestamp - _buffer.Peek().timestamp >= timeWindow)
            _buffer.Dequeue();

        return _isBufferFull;
    }

    private GazeState EstimateState()
    {
        float avgXB = 0f;
        float avgYB = 0f;
        float avgXA = 0f;
        float avgYA = 0f;
        float ptsBeforeCount = 0f;
        float ptsAfterCount = 0f;

        ulong oldestTimestamp = _buffer.Peek().timestamp;

        foreach (T data in _buffer)
        {
            ulong dt = data.timestamp - oldestTimestamp;
            if (dt > timeWindow / 2)
            {
                avgXB += data.x;
                avgYB += data.y;
                ptsBeforeCount++;
            }
            else
            {
                avgXA += data.x;
                avgYA += data.y;
                ptsAfterCount++;
            }
        }

        if (ptsBeforeCount > 0 && ptsAfterCount > 0)
        {
            avgXB = avgXB / ptsBeforeCount;
            avgYB = avgYB / ptsBeforeCount;
            avgXA = avgXA / ptsAfterCount;
            avgYA = avgYA / ptsAfterCount;

            var dx = avgXB - avgXA;
            var dy = avgYB - avgYA;
            var dist = Math.Sqrt(dx * dx + dy * dy);

            return dist > saccadeThreshold ? GazeState.Saccade : GazeState.Fixation;
        }

        return GazeState.Unknown;
    }

    private ulong EstimateInterval(T aData)
    {
        if (_buffer.Count < 2)
            return 0;

        ulong duration = aData.timestamp - _buffer.Peek().timestamp;
        return (ulong)((int)duration / (_buffer.Count - 1));
    }

    #endregion
}

/** <summary>Interface for shoothing data</summary> */
public interface IRawData
{
    /** <summary>Timestamp, ms</summary> */
    ulong timestamp { get; }

    /** <summary>Gaze X</summary> */
    float x { get; }

    /** <summary>Gaze Y</summary> */
    float y { get; }

    /**
      * <summary>Copies values from another data</summary>
      * <param name="aRef">Reference to copy from</param>
      */
    //void static copyFrom(IRawData aRef);

    /**
      * <summary>Applies smooting</summary>
      * <param name="aRef">Latest raw data</param>
      * <param name="aAlfa">Alfa</param>
      * <param name="aTimestamp">New timestamp</param>
      */
    void shift(IRawData aRef, double aAlfa, ulong aTimestamp);
}

/** <summary>Raw 2D gaze point</summary> */
public class RawPoint : IRawData
{
    /** <summary>Timestamp, ms</summary> */
    public ulong timestamp { get; private set; }

    /** <summary>Gaze X</summary> */
    public float x { get; private set; }

    /** <summary>Gaze Y</summary> */
    public float y { get; private set; }

    /**
      * <summary>Constructor</summary>
      * <param name="aTimestamp">Timestamp, ms</param>
      * <param name="aX">Gaze X</param>
      * <param name="aY">Gaze Y</param>
      */
    public RawPoint(ulong aTimestamp, float aX, float aY)
    {
        timestamp = aTimestamp;
        x = aX;
        y = aY;
    }

    /**
      * <summary>Copies values from another data</summary>
      * <param name="aRef">Reference to copy from</param>
      */
    public static RawPoint copyFrom(IRawData aRef)
    {
        return new RawPoint(aRef.timestamp, aRef.x, aRef.y);
    }

    /**
      * <summary>Applies smooting</summary>
      * <param name="aRef">Latest raw data</param>
      * <param name="aAlfa">Alfa</param>
      * <param name="aTimestamp">New timestamp</param>
      */
    public void shift(IRawData aRef, double aAlfa, ulong aTimestamp)
    {
        timestamp = aTimestamp;
        x = (float)((aRef.x + aAlfa * x) / (1.0 + aAlfa));
        y = (float)((aRef.y + aAlfa * y) / (1.0 + aAlfa));
    }
}

/** <summary>Raw 3D gaze point</summary> */
public class RawVector : IRawData
{
    /** <summary>Timestamp, ms</summary> */
    public ulong timestamp { get; private set; }

    /** <summary>Gaze X</summary> */
    public float x { get; private set; }

    /** <summary>Gaze Y</summary> */
    public float y { get; private set; }

    /** <summary>Gaze Z</summary> */
    public float z { get; private set; }

    /** <summary>Original vector</summary> */
    public Vector3 vectorOriginal { get { return _vector; } }

    /** <summary>Shifted vector</summary> */
    public Vector3 vectorShifted { get { return new Vector3(x, y, z); } }

    private Vector3 _vector;

    /**
      * <summary>Constructor</summary>
      * <param name="aTimestamp">Timestamp, ms</param>
      * <param name="aVector">Gaze vector</param>
      */
    public RawVector(ulong aTimestamp, Vector3 aVector)
    {
        _vector = aVector;

        timestamp = aTimestamp;

        x = (float)Math.Tan(Math.Asin(_vector.x));
        y = (float)Math.Tan(Math.Asin(_vector.y));
    }

    /**
      * <summary>Copies values from another data</summary>
      * <param name="aRef">Reference to copy from</param>
      */
    public static RawVector copyFrom(IRawData aRef)
    {
        RawVector r = (RawVector)aRef;
        if (r == null)
            throw new ArgumentException("Cannot copy from the instance of another type");

        return new RawVector(r.timestamp, new Vector3(r.x, r.y, r.z));
    }

    /**
      * <summary>Applies smooting</summary>
      * <param name="aRef">Latest raw data</param>
      * <param name="aAlfa">Alfa</param>
      * <param name="aTimestamp">New timestamp</param>
      */
    public void shift(IRawData aRef, double aAlfa, ulong aTimestamp)
    {
        timestamp = aTimestamp;
        x = (float)((aRef.x + aAlfa * x) / (1.0 + aAlfa));
        y = (float)((aRef.y + aAlfa * y) / (1.0 + aAlfa));
        z = (float)Math.Sqrt(1 - x * x - y * y);
    }
}
