using System;
using System.Runtime.InteropServices;
using FMOD;
using OdinNative.Core;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Odin_FMod
{
    public class FMODMicrophoneReader : MonoBehaviour
    {
        private FMOD.CREATESOUNDEXINFO recordingSoundInfo = new CREATESOUNDEXINFO();

        private FMOD.Sound recordingSound;
        private FMOD.Channel recordingChannel;

        private uint recordingSoundLength = 0;
        private uint lastRecordPosition = 0;

        private int nativeRate;
        private int nativeChannels;

        private void Start()
        {
            FMODUnity.RuntimeManager.CoreSystem.getRecordDriverInfo(0, out _, 0, out _, out nativeRate, out _,
                out nativeChannels, out _);


            /*
            Create user sound to record into, then start recording.
        */
            recordingSoundInfo.cbsize = Marshal.SizeOf(typeof(FMOD.CREATESOUNDEXINFO));
            recordingSoundInfo.numchannels = nativeChannels;
            recordingSoundInfo.defaultfrequency = nativeRate;
            recordingSoundInfo.format = FMOD.SOUND_FORMAT.PCMFLOAT;
            recordingSoundInfo.length = (uint)(nativeRate * sizeof(float) * nativeChannels);

            FMODUnity.RuntimeManager.CoreSystem.createSound("", FMOD.MODE.LOOP_NORMAL | FMOD.MODE.OPENUSER,
                ref recordingSoundInfo,
                out recordingSound);

            var result = FMODUnity.RuntimeManager.CoreSystem.recordStart(0, recordingSound, true);
            Debug.Log("Record Start result: " + result);
            
            recordingSound.getLength(out recordingSoundLength, FMOD.TIMEUNIT.PCM);
        }

        private void FixedUpdate()
        {
            if (!OdinHandler.Instance || OdinHandler.Instance.Rooms.Count == 0)
                return;

            /*
          Determine how much has been recorded since we last checked
      */
            FMODUnity.RuntimeManager.CoreSystem.getRecordPosition(0, out var recordPosition);

            uint recordDelta = (recordPosition >= lastRecordPosition)
                ? (recordPosition - lastRecordPosition)
                : (recordPosition + recordingSoundLength - lastRecordPosition);
            if (0 == recordDelta)
                return;

            int recordedArraySize = (int) recordDelta;
            float[] micBuffer = new float[recordedArraySize];

            // int sizeToCheckAgainst = bufferSize * sizeof(float);
            // if (recordDelta < sizeToCheckAgainst - 1)
            // {
            //     return;
            // }

            // TODO: Add small delay before reading, to ensure that the last read position is always behind the record position.

            IntPtr data1, data2;
            uint length1, length2;
            recordingSound.@lock(lastRecordPosition, recordDelta, out data1, out data2,
                out length1, out length2);

            int actualDataLength = (int)length1;
            Marshal.Copy(data1, micBuffer, 0, actualDataLength);
            recordingSound.unlock(data1, data2, length1, length2);

            // Debug.Log( "Recorddelta: " + recordDelta + " Read data length: " + actualDataLength);

            foreach (var room in OdinHandler.Instance.Rooms)
            {
                if (null == room.MicrophoneMedia)
                {
                    room.CreateMicrophoneMedia(new OdinMediaConfig((MediaSampleRate)nativeRate,
                        (MediaChannels)nativeChannels));
                }

                if (null != room?.MicrophoneMedia)
                    room.MicrophoneMedia.AudioPushData(micBuffer, actualDataLength);
            }
            
            lastRecordPosition = recordPosition;

        }
    }
}