using UnityEngine;
using Photon.Pun;
public class GameManager : MonoBehaviour
{

    [SerializeField]
    private Transform m_target;
    [SerializeField]
    private Transform m_spawnPoint;
    public PhotonView view;
    void Update()
    {
        if (view.IsMine)
        {
            if(Input.GetKeyDown(KeyCode.R))
            {
                m_target.position = m_spawnPoint.position;
                m_target.rotation = m_spawnPoint.rotation;
            }
        }
    }
}
