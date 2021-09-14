using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class PlayingLadyPlayer : MonoBehaviour
{
    // to be set in inspector

    public VideoClip[] clips;

    // public members

    public event EventHandler Started = delegate { };
    public event EventHandler Stopped = delegate { };

    // internal members

    VideoPlayer[] _players;

    // overrides

    void Start()
    {
        _players = GetComponents<VideoPlayer>();
        foreach (var player in _players)
        {
            player.prepareCompleted += onPlayerStarted;
            player.loopPointReached += onPlayerStoppped;
        }
    }

    // public methods

    public void SetClips(string aFileName, string aFirstClipExtraVariable)
    {
        var clipNames = new string[] {
            aFileName + $"_{aFirstClipExtraVariable}_part1",
            aFileName + "_part2",
        };

        for (var i = 0; i < _players.Length; i++)
        {
            _players[i].clip = clips.First(clip => clip.originalPath.Contains(clipNames[i]));
            _players[i].enabled = true;
        }
    }

    public void PlayFirst()
    {
        Debug.Log($"VIDEO: {_players[0].clip.name}");
        _players[0].Play();
    }

    public void PlaySecond()
    {
        Debug.Log($"VIDEO: {_players[1].clip.name}");
        _players[1].Play();
    }

    public void Stop()
    {
        foreach (var player in _players)
        {
            player.enabled = false;
            player.clip = null;
        }
    }

    // internal methods

    void onPlayerStarted(VideoPlayer source)
    {
        Started(this, new EventArgs());
    }

    void onPlayerStoppped(VideoPlayer vp)
    {
        Stopped(this, new EventArgs());
    }
}
