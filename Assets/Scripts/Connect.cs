using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class Connect : MonoBehaviourPunCallbacks
{
    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        print("sucessfully connected");
        SceneManager.LoadScene("Lobby");
    }
}
