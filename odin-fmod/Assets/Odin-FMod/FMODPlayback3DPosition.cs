using System;
using System.Collections;
using System.Collections.Generic;
using FMOD;
using UnityEngine;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;

[RequireComponent(typeof(FMODPlaybackComponent))]
public class FMODPlayback3DPosition : MonoBehaviour
{
    private FMODPlaybackComponent _playbackComponent;

    private void Awake()
    {
        _playbackComponent = GetComponent<FMODPlaybackComponent>();
        Assert.IsNotNull(_playbackComponent);
    }

    private IEnumerator Start()
    {
        // wait for playback component initialization
        while (!(_playbackComponent.FMODPlaybackChannel.hasHandle() && _playbackComponent.FMODPlaybackSound.hasHandle()))
        {
            yield return null;
        }

        // init 3d sound effects
        _playbackComponent.FMODPlaybackChannel.setMode(MODE._3D);
        _playbackComponent.FMODPlaybackChannel.set3DLevel(1);
        _playbackComponent.FMODPlaybackSound.setMode(MODE._3D);
        
    }

    private void FixedUpdate()
    {
        if (_playbackComponent.FMODPlaybackChannel.hasHandle())
        {
            // update the 3d position of the playback in FMOD
            ATTRIBUTES_3D attributes3D = FMODUnity.RuntimeUtils.To3DAttributes(transform);
            _playbackComponent.FMODPlaybackChannel.set3DAttributes(ref attributes3D.position, ref attributes3D.velocity);
        }
    }
}
