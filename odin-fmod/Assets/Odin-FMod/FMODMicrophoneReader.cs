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

        private readonly uint minReadSamples = 0;
        private readonly uint distanceToMicReadPosition = 0;

        private void Start()
        {
            FMODUnity.RuntimeManager.CoreSystem.getRecordDriverInfo(0, out _, 0, out _, out nativeRate, out _,
                out nativeChannels, out _);
            // Create user sound to record into, then start recording.
            recordingSoundInfo.cbsize = Marshal.SizeOf(typeof(CREATESOUNDEXINFO));
            recordingSoundInfo.numchannels = nativeChannels;
            recordingSoundInfo.defaultfrequency = nativeRate;
            recordingSoundInfo.format = SOUND_FORMAT.PCMFLOAT;
            recordingSoundInfo.length = (uint)(nativeRate * sizeof(float) * nativeChannels);

            FMODUnity.RuntimeManager.CoreSystem.createSound("", MODE.LOOP_NORMAL | MODE.OPENUSER,
                ref recordingSoundInfo,
                out recordingSound);

            var result = FMODUnity.RuntimeManager.CoreSystem.recordStart(0, recordingSound, true);
            Debug.Log("Record Start result: " + result);

            recordingSound.getLength(out recordingSoundLength, TIMEUNIT.PCM);
            Debug.Log("Recording sound length: " + recordingSoundLength);
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

            if (recordDelta < distanceToMicReadPosition)
                return;

            uint targetRecordArraySize = recordDelta - distanceToMicReadPosition;
            if (targetRecordArraySize < minReadSamples)
                return;

            float[] micBuffer = new float[targetRecordArraySize];

            uint recordArrayByteSize = targetRecordArraySize * sizeof(float);

            IntPtr micDataPointer, unusedData;
            uint readMicDataLength, unusedDataLength;

            recordingSound.@lock(lastRecordPosition * sizeof(float), recordArrayByteSize, out micDataPointer, out unusedData,
                out readMicDataLength, out unusedDataLength);
            Marshal.Copy(micDataPointer, micBuffer, 0, (int)readMicDataLength / sizeof(float));
            recordingSound.unlock(micDataPointer, unusedData, readMicDataLength, unusedDataLength);

            if (targetRecordArraySize > 0)
            {
                foreach (var room in OdinHandler.Instance.Rooms)
                {
                    if (null == room.MicrophoneMedia)
                    {
                        room.CreateMicrophoneMedia(new OdinMediaConfig((MediaSampleRate)nativeRate,
                            (MediaChannels)nativeChannels));
                    }

                    if (null != room?.MicrophoneMedia)
                        room.MicrophoneMedia.AudioPushData(micBuffer, (int)targetRecordArraySize);
                }
            }


            Debug.Log("Current Record Pos: " + recordPosition + " Last Record Pos: " + lastRecordPosition +
                      " read data: " + targetRecordArraySize);

            lastRecordPosition += targetRecordArraySize;
            if (lastRecordPosition > recordingSoundLength)
                lastRecordPosition -= recordingSoundLength;
        }
    }
}