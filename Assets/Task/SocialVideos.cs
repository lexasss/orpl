using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using Extensions;

public class SocialVideos : MonoBehaviour
{
    // to be set in inspector

    public VideoClip[] clips;

    // private

    int _index = 0;
    VideoClip[] _order;

    // overrides

    void Start()
    {
        
    }

    // public methods

    public void Reset()
    {
        _index = 0;

        var videos = new List<VideoClip>();
        videos.AddRange(clips);

        _order = videos.ToArray();

        System.Random rnd = new System.Random((int)DateTime.Now.Ticks);
        rnd.Shuffle(_order);
    }

    public VideoClip Next()
    {
        var result = _order[_index];
        if (++_index == _order.Length)
        {
            _index = 0;
        }

        return result;
    }
}
