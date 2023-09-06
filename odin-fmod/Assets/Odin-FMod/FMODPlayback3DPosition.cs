using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(FMODPlaybackComponent))]
public class FMODPlayback3DPosition : MonoBehaviour
{
    private FMODPlaybackComponent _playbackComponent;

    private void Awake()
    {
        _playbackComponent = GetComponent<FMODPlaybackComponent>();
        Assert.IsNotNull(_playbackComponent);
    }

    private void Update()
    {
        if (_playbackComponent.FMODPlaybackSound.hasHandle())
        {
            
        }
    }
}
