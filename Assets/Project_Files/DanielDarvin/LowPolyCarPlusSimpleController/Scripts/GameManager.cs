using UnityEngine;

public class GameManager : MonoBehaviour
{

    [SerializeField]
    private Transform m_target;
    [SerializeField]
    private Transform m_spawnPoint;

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.R))
        {
            m_target.position = m_spawnPoint.position;
            m_target.rotation = m_spawnPoint.rotation;
        }
    }
}
