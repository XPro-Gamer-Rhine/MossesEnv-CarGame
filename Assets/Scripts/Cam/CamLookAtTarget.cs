using UnityEngine;
using Photon.Pun;

public class CamLookAtTarget : MonoBehaviour
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
            cam.lookAtTarget = transform;
            cam.SetLookAtTarget(transform);
            print("Got Cam's LookAtTarget");
        }
        
    }
}
