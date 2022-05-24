using UnityEngine;
using Photon.Pun;

public class CamSidePosition : MonoBehaviour
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
            cam.sideView = transform;
            cam.SetSideView(transform);
            print("Got Cam's SidePosition");
        }
        
    }
}