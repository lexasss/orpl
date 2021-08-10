using System;
using System.Linq;
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
    List<VideoClip> _videos = new List<VideoClip>();

    const string SOCIAL_FILENAME = "social.txt";

    // public methods

    public bool Load(string aFileNamePrefix = "")
    {
        var lines = Files.ReadLines(aFileNamePrefix + SOCIAL_FILENAME);
        if (lines == null)
        {
            return false;
        }

        _videos.Clear();

        foreach (var line in lines)
        {
            var name = line.Trim();
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            try
            { 
                var socialVideo = clips.First(clip => clip.name.ToLower() == name);
                _videos.Add(socialVideo);
            }
            catch
            {
                return false;
            }
        }

        return true;
    }
    
    public void Reset()
    {
        _index = 0;
        /*
        var videos = new List<VideoClip>();
        videos.AddRange(clips);

        _videos = videos.ToArray();

        System.Random rnd = new System.Random((int)DateTime.Now.Ticks);
        rnd.Shuffle(_videos);
        */
    }

    public VideoClip Next()
    {
        var result = _videos[_index];
        if (++_index == _videos.Count)
        {
            _index = 0;
        }

        return result;
    }
}
