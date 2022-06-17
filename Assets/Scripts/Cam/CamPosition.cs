using UnityEngine;
using Photon.Pun;

public class CamPosition : MonoBehaviour
{
    DriftCamera cam;
    public PhotonView photon;

    private void Awake()
    {
        cam = FindObjectOfType<DriftCamera>();
    }

    private void Start()
    {
        if (photon.IsMine)
        {
            cam.positionTarget = transform;
            cam.SetPositionTarget(transform);
            print("Got Cam's PositionTarget");
        }
        
    }
}
