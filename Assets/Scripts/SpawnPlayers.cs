using UnityEngine;
using Photon.Pun;

public class SpawnPlayers : MonoBehaviour
{
    public GameObject playerPrefab;
    public Transform[] spawnPoints;
    private Transform currentPosition;
    private int currentPositionIndex;
    private void Start()
    {
        print("Instaniate");
        currentPositionIndex = Random.Range (0, spawnPoints.Length);
        currentPosition = spawnPoints[currentPositionIndex];
        PhotonNetwork.Instantiate(playerPrefab.name, currentPosition.position, currentPosition.rotation);
    }
}
