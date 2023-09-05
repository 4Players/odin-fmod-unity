using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FMOD;
using OdinNative.Core;
using OdinNative.Odin.Media;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class FMODPlaybackComponent : MonoBehaviour
{
    private PlaybackStream _playbackMedia;

    private PlaybackStream PlaybackMedia
    {
        get => _playbackMedia;
        set
        {
            _playbackMedia = value;
            _playbackMedia?.AudioReset();
        }
    }

    /// <summary>
    /// Retrieves the connected media stream based on the room name, peer id and media id.
    /// </summary>
    /// <returns>Media Stream as Playbackstream, if room name, peer id and media id point to a valid stream, or null otherwise.</returns>
    private PlaybackStream FindOdinMediaStream() => OdinHandler.Instance.Rooms[RoomName]?
        .RemotePeers[PeerId]?
        .Medias[MediaStreamId] as PlaybackStream;


    /// <summary>
    ///     Room name for this playback. Change this value to change the PlaybackStream by Rooms from the Client.
    /// </summary>
    /// <remarks>Invalid values will cause errors.</remarks>
    public string RoomName
    {
        get => _roomName;
        set
        {
            _roomName = value;
            PlaybackMedia = FindOdinMediaStream();
        }
    }

    /// <summary>
    ///     Peer id for this playback. Change this value to change the PlaybackStream by RemotePeers in the Room.
    /// </summary>
    /// <remarks>Invalid values will cause errors.</remarks>
    public ulong PeerId
    {
        get => _peerId;
        set
        {
            _peerId = value;
            PlaybackMedia = FindOdinMediaStream();
        }
    }

    /// <summary>
    ///     Media id for this playback. Change this value to pick a PlaybackStream by media id from peers Medias.
    /// </summary>
    /// <remarks>Invalid values will cause errors.</remarks>
    public long MediaStreamId
    {
        get => _mediaStreamId;
        set
        {
            _mediaStreamId = value;
            PlaybackMedia = FindOdinMediaStream();
        }
    }

    private CREATESOUNDEXINFO _createSoundInfo;
    private Sound _playbackSound;
    private Channel _playbackChannel;
    private ulong _peerId;
    private long _mediaStreamId;
    private string _roomName;
    private float[] _readBuffer = Array.Empty<float>();

    private SOUND_PCMREAD_CALLBACK _pcmreadCallback;


    // Start is called before the first frame update
    void Start()
    {
        int playBackRate = (int)OdinHandler.Config.RemoteSampleRate;
        int numChannels = (int)OdinHandler.Config.RemoteChannels;
        
        _createSoundInfo.cbsize = Marshal.SizeOf(typeof(FMOD.CREATESOUNDEXINFO));
        _createSoundInfo.numchannels = numChannels;
        _createSoundInfo.defaultfrequency = playBackRate;
        _createSoundInfo.format = SOUND_FORMAT.PCMFLOAT;
        _pcmreadCallback = new SOUND_PCMREAD_CALLBACK(PcmReadCallback);
        _createSoundInfo.pcmreadcallback = _pcmreadCallback;
        _createSoundInfo.length = (uint)(playBackRate * sizeof(float) * numChannels);

        FMODUnity.RuntimeManager.CoreSystem.createStream("", MODE.OPENUSER | MODE.LOOP_NORMAL,
            ref _createSoundInfo, out _playbackSound);

        FMODUnity.RuntimeManager.CoreSystem.getMasterChannelGroup(out ChannelGroup masterChannelGroup);

        FMODUnity.RuntimeManager.CoreSystem.playSound(_playbackSound, masterChannelGroup, false,
            out _playbackChannel);
    }


    [AOT.MonoPInvokeCallback(typeof(SOUND_PCMREAD_CALLBACK))]
    private RESULT PcmReadCallback(IntPtr sound, IntPtr data, uint dataLength)
    {
        // retrieve array length of data that is requested
        int requestedDataArrayLength = (int)dataLength / sizeof(float);
        // resize read buffer if necessary
        if (_readBuffer.Length < requestedDataArrayLength)
        {
            _readBuffer = new float[requestedDataArrayLength];
        }

        // check data pointer for validity
        if (data == IntPtr.Zero)
        {
            // Handle error
            return RESULT.ERR_INVALID_PARAM;
        }

        // only read if we've joined any room and the connected playback media is valid.
        if (OdinHandler.Instance.HasConnections && !PlaybackMedia.IsInvalid)
        {
            // read voice data from media stream into read buffer
            uint odinReadResult = PlaybackMedia.AudioReadData(_readBuffer, requestedDataArrayLength);
            if (Utility.IsError(odinReadResult))
            {
                Debug.LogWarning(
                    $"{nameof(FMODPlaybackComponent)} AudioReadData failed with error code {odinReadResult}");
            }
            else
            {
                // copy read ODIN data into the FMOD stream
                Marshal.Copy(_readBuffer, 0, data, requestedDataArrayLength);
            }
        }

        return RESULT.OK;
    }


    private void OnDestroy()
    {
        _playbackSound.release();
    }
}