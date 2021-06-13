using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    public Action OnReady;
    public Action<List<RoomInfo>> OnLobbyListUpdate;
    public new Action<Room> OnJoinedLobby;
    public Action<Room> OnPlayerJoinedLobby;
    public Action<Room> OnPlayerLeftLobby;

    public void Initialize()
    {
        PhotonNetwork.ConnectUsingSettings();
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();
        if (PhotonNetwork.CurrentLobby == null)
        {
            PhotonNetwork.JoinLobby();
            OnReady?.Invoke();
        }
    }

    public void LeaveLobby()
    {
        PhotonNetwork.LeaveRoom();
    }

    public void CreateLobby(string name)
    {
        RoomOptions roomOptions = new RoomOptions()
        {
            MaxPlayers = 2,
            EmptyRoomTtl = 1
        };
        PhotonNetwork.CreateRoom(name, roomOptions);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        base.OnRoomListUpdate(roomList);
        OnLobbyListUpdate?.Invoke(roomList);
    }

    public void JoinLobby(string name)
    {
        PhotonNetwork.JoinRoom(name);
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        OnPlayerJoinedLobby?.Invoke(PhotonNetwork.CurrentRoom);
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        OnJoinedLobby?.Invoke(PhotonNetwork.CurrentRoom);
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);
        OnPlayerLeftLobby?.Invoke(PhotonNetwork.CurrentRoom);
    }

    public void SetNickName(string nickName)
    {
        PhotonNetwork.LocalPlayer.NickName = nickName;
    }

    public void StartGame()
    {
        PhotonNetwork.LoadLevel("In Game");
    }
}
