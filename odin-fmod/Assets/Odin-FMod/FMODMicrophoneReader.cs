using System;
using System.Runtime.InteropServices;
using FMOD;
using OdinNative.Core;
using OdinNative.Odin.Room;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Odin_FMod
{
    public class FMODMicrophoneReader : MonoBehaviour
    {
        /// <summary>
        /// Reference to the recording sound create info.
        /// </summary>
        private FMOD.CREATESOUNDEXINFO _recordingSoundInfo = new CREATESOUNDEXINFO();

        /// <summary>
        /// Reference to the FMOD recording sound
        /// </summary>
        private FMOD.Sound _recordingSound;

        /// <summary>
        /// The recording sound length in PCM Samples
        /// </summary>
        private uint _recordingSoundLength = 0;

        /// <summary>
        /// The current position at which we're reading samples from the FMOD recording sound, in PCM samples.
        /// </summary>
        private uint _currentReadPosition = 0;

        /// <summary>
        /// Native microphone sampling rate in Hz
        /// </summary>
        private int _nativeRate;

        /// <summary>
        /// Number of native microphone channels.
        /// </summary>
        private int _nativeChannels;

        /// <summary>
        /// The device id, from which the microphone data is recorded.
        ///
        /// Needs to be adjusted, if the recording should be done from a microphone
        /// other than the Default Device.
        /// </summary>
        private int _currentDeviceId = 0;

        // 50ms * 48kHz sampling rate = 960 samples
        private float[] _readBuffer = new float[960];


        private void Start()
        {
            // retrieve microphone info like sampling rate and number of channels
            FMODUnity.RuntimeManager.CoreSystem.getRecordDriverInfo(_currentDeviceId, out _, 0, out _, out _nativeRate,
                out _,out _nativeChannels, out _);
            // Setup recording sound that will contain microphone data
            _recordingSoundInfo.cbsize = Marshal.SizeOf(typeof(CREATESOUNDEXINFO));
            _recordingSoundInfo.numchannels = _nativeChannels;
            _recordingSoundInfo.defaultfrequency = _nativeRate;
            _recordingSoundInfo.format = SOUND_FORMAT.PCMFLOAT;
            // make recording sound one second long
            _recordingSoundInfo.length = (uint)(_nativeRate * sizeof(float) * _nativeChannels);

            // Create recording sound
            FMODUnity.RuntimeManager.CoreSystem.createSound("", MODE.LOOP_NORMAL | MODE.OPENUSER,
                ref _recordingSoundInfo,
                out _recordingSound);

            // Start recording
            FMODUnity.RuntimeManager.CoreSystem.recordStart(_currentDeviceId, _recordingSound, true);
            // retrieve recording sound length in pcm samples.
            _recordingSound.getLength(out _recordingSoundLength, TIMEUNIT.PCM);
        }

        private void OnDestroy()
        {
            FMODUnity.RuntimeManager.CoreSystem.recordStop(_currentDeviceId);
            _recordingSound.release();
        }


        private void Update()
        {
            if (!OdinHandler.Instance || !OdinHandler.Instance.HasConnections || OdinHandler.Instance.Rooms.Count == 0)
                return;

            // Determine how much has been recorded since we last checked
            FMODUnity.RuntimeManager.CoreSystem.getRecordPosition(_currentDeviceId, out uint recordPosition);
            uint recordDelta = (recordPosition >= _currentReadPosition)
                ? (recordPosition - _currentReadPosition)
                : (recordPosition + _recordingSoundLength - _currentReadPosition);

            if (recordDelta < 1)
                return;
            
            // update read buffer length, if buffer is too short
            if(_readBuffer.Length < recordDelta)
                _readBuffer = new float[recordDelta];
            
            uint recordArrayByteSize = recordDelta * sizeof(float);

            IntPtr micDataPointer, unusedData;
            uint readMicDataLength, unusedDataLength;

            // read microphone data from fmod sound and copy into _readBuffer
            _recordingSound.@lock(_currentReadPosition * sizeof(float), recordArrayByteSize, out micDataPointer,
                out unusedData,
                out readMicDataLength, out unusedDataLength);
            uint readArraySize = readMicDataLength / sizeof(float);
            Marshal.Copy(micDataPointer, _readBuffer, 0, (int)readArraySize);
            _recordingSound.unlock(micDataPointer, unusedData, readMicDataLength, unusedDataLength);

            if (readMicDataLength > 0)
            {
                foreach (var room in OdinHandler.Instance.Rooms)
                {
                    ValidateMicrophoneStream(room);

                    // push microphone data to odin servers
                    if (null != room.MicrophoneMedia)
                        room.MicrophoneMedia.AudioPushData(_readBuffer, (int)readArraySize);
                }
            }

            _currentReadPosition += readArraySize;
            if (_currentReadPosition >= _recordingSoundLength)
                _currentReadPosition -= _recordingSoundLength;
        }

        /// <summary>
        /// Checks the rooms microphone stream for correct setup and initializes the stream if necessary.
        /// </summary>
        /// <param name="room">Room to check the stream for.</param>
        private void ValidateMicrophoneStream(Room room)
        {
            bool isValidStream = null != room.MicrophoneMedia &&
                                 _nativeChannels == (int)room.MicrophoneMedia.MediaConfig.Channels &&
                                 _nativeRate == (int)room.MicrophoneMedia.MediaConfig.SampleRate;
            if (!isValidStream)
            {
                room.MicrophoneMedia?.Dispose();
                room.CreateMicrophoneMedia(new OdinMediaConfig((MediaSampleRate)_nativeRate,
                    (MediaChannels)_nativeChannels));
            }
        }
    }
}