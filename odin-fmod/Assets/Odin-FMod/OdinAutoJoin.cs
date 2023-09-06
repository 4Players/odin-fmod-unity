using System;
using System.Collections;
using System.Collections.Generic;
using OdinNative.Odin.Room;
using UnityEngine;
using UnityEngine.Assertions;

public class OdinAutoJoin : MonoBehaviour
{
    [SerializeField] private string roomName;
    [SerializeField] private FMODPlaybackComponent playbackPrefab;


    private readonly List<FMODPlaybackComponent> _instantiatedObjects = new List<FMODPlaybackComponent>();

    private void Awake()
    {
        Assert.IsNotNull(playbackPrefab);
    }

    // Start is called before the first frame update
    void Start()
    {
        OdinHandler.Instance.JoinRoom(roomName);
        OdinHandler.Instance.OnMediaAdded.AddListener(OnMediaAdded);
        OdinHandler.Instance.OnMediaRemoved.AddListener(OnMediaRemoved);
    }


    private void OnDestroy()
    {
        if (OdinHandler.Instance)
        {
            OdinHandler.Instance.OnMediaAdded.RemoveListener(OnMediaAdded);
            OdinHandler.Instance.OnMediaRemoved.RemoveListener(OnMediaRemoved);
        }
    }

    private void OnMediaRemoved(object roomObject, MediaRemovedEventArgs mediaRemovedEventArgs)
    {
        if (roomObject is Room room)
        {
            for (int i = _instantiatedObjects.Count - 1; i >= 0; i--)
            {
                var comp = _instantiatedObjects[i];
                if (comp.RoomName == room.Config.Name && comp.PeerId == mediaRemovedEventArgs.Peer.Id &&
                    comp.MediaStreamId == mediaRemovedEventArgs.MediaStreamId)
                {
                    _instantiatedObjects.RemoveAt(i);
                    Destroy(comp.gameObject);
                }
            }
        }
    }

    private void OnMediaAdded(object roomObject, MediaAddedEventArgs mediaAddedEventArgs)
    {
        if (roomObject is Room room)
        {
            FMODPlaybackComponent newPlayback = Instantiate(playbackPrefab);
            newPlayback.RoomName = room.Config.Name;
            newPlayback.PeerId = mediaAddedEventArgs.PeerId;
            newPlayback.MediaStreamId = mediaAddedEventArgs.Media.Id;
            _instantiatedObjects.Add(newPlayback);
        }
    }
}