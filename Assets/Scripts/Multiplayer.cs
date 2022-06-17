using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
public class Multiplayer : MonoBehaviourPunCallbacks
{
    // Start is called before the first frame update
    public GameObject player;
    void Start()
    {
        PhotonNetwork.Instantiate(player.name,new Vector3(Random.Range(141f, 160f), 53, Random.Range(-63f, -75f)), Quaternion.identity);
    }

    
}
