using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class Lobby : MonoBehaviourPunCallbacks
{
    public TMP_InputField _createInput;
    public TMP_InputField _joinInput;

    public byte _maxPlayers = 10;

    private void Start()
    {
        PhotonNetwork.JoinLobby();
    }

    public void CreateRoom()
    {
        RoomOptions room = new RoomOptions();
        room.MaxPlayers = _maxPlayers;

        PhotonNetwork.CreateRoom(_createInput.text, room);
    }

    public void JoinRoom()
    {
        PhotonNetwork.JoinRoom(_joinInput.text);
    }

    public override void OnJoinedRoom()
    {
        PhotonNetwork.LoadLevel("MainGame");
    }
}
