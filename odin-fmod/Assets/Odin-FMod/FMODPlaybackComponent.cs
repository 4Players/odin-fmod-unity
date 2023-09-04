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

    private PlaybackStream OdinMedia => OdinHandler.Instance.Rooms[RoomName]?
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
            PlaybackMedia = OdinMedia;
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
            PlaybackMedia = OdinMedia;
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
            PlaybackMedia = OdinMedia;
        }
    }

    private FMOD.CREATESOUNDEXINFO _createSoundInfo = new FMOD.CREATESOUNDEXINFO();
    private FMOD.Sound _playbackSound;
    private FMOD.Channel _playbackChannel;
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
        /*
          Create user sound to record into, then start recording.
      */
        _createSoundInfo.cbsize = Marshal.SizeOf(typeof(FMOD.CREATESOUNDEXINFO));
        _createSoundInfo.numchannels = numChannels;
        _createSoundInfo.defaultfrequency = playBackRate;
        _createSoundInfo.format = FMOD.SOUND_FORMAT.PCM16;
        _pcmreadCallback = new SOUND_PCMREAD_CALLBACK(PcmReadCallback);
        _createSoundInfo.pcmreadcallback = _pcmreadCallback;
        _createSoundInfo.length = (uint)(playBackRate * sizeof(short) * numChannels);

        FMODUnity.RuntimeManager.CoreSystem.createStream("", FMOD.MODE.OPENUSER | MODE.LOOP_NORMAL,
            ref _createSoundInfo, out _playbackSound);

        FMODUnity.RuntimeManager.CoreSystem.getMasterChannelGroup(out FMOD.ChannelGroup masterChannelGroup);
        RESULT playSoundResult =
            FMODUnity.RuntimeManager.CoreSystem.playSound(_playbackSound, masterChannelGroup, false,
                out _playbackChannel);
        Debug.Log("PlaysoundResult: " + playSoundResult);
    }


    [AOT.MonoPInvokeCallback(typeof(FMOD.SOUND_PCMREAD_CALLBACK))]
    private RESULT PcmReadCallback(IntPtr sound, IntPtr data, uint dataLength)
    {
        int dataArrayLength = (int) dataLength / sizeof(short);
        if (_readBuffer.Length < dataArrayLength)
        {
            _readBuffer = new float[dataArrayLength];
        }
        
        if (data == IntPtr.Zero)
        {
            // Handle error
            return FMOD.RESULT.ERR_INVALID_PARAM;
        }

        if (OdinHandler.Instance.HasConnections && !PlaybackMedia.IsInvalid)
        {
            uint readResult = PlaybackMedia.AudioReadData(_readBuffer, (int)dataArrayLength);
            if (Utility.IsError(readResult))
            {
                Debug.LogWarning(
                    $"{nameof(FMODPlaybackComponent)} AudioReadData failed with error code {readResult}");
            }
            else
            {
                int readLength = Mathf.Min(_readBuffer.Length, (int)dataArrayLength);
                // Convert float[] _ReadBuffer to short[]
                short[] shortData = new short[readLength];
                for (int i = 0; i < readLength; i++)
                {
                    shortData[i] = (short)(_readBuffer[i] * 32767); // float to short
                }


                Marshal.Copy(shortData, 0, data, readLength);

                // Copy shortData to data
            }
        }

        return FMOD.RESULT.OK;
    }


    private void OnDestroy()
    {
        _playbackSound.release();
    }
}