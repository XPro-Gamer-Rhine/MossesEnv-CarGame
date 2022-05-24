using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CrowdAI : MonoBehaviour
{

    public Animator anime;
    public NavMeshAgent navMeshAgent;
    public Transform NodesParent;
    public List<Transform> Nodes;

    public int NodeIndex = 1;
    // Start is called before the first frame update
    void Start()
    {
            anime = this.GetComponent<Animator>();
            navMeshAgent = this.GetComponent<NavMeshAgent>();
            //NodesParent = GameObject.Find("NodesParent").transform;
            Nodes = new List<Transform>(NodesParent.GetComponentsInChildren<Transform>());
            navMeshAgent.SetDestination(Nodes[NodeIndex].transform.position);
    }
    private void Move()
    {
        if (navMeshAgent.velocity.magnitude > 0)
        {
            anime.SetFloat("InputMagnitude", (navMeshAgent.velocity.magnitude) / navMeshAgent.speed);
        }
        if (Vector3.Distance(this.gameObject.transform.position, Nodes[NodeIndex].position) < 1.3f)
        {
            if (NodeIndex >= Nodes.Count - 1)
            {
                NodeIndex = 1;
            }
            else
            {
                NodeIndex++;
            }

            navMeshAgent.SetDestination(Nodes[NodeIndex].transform.position);
        }
    }

    void Update()
    {
            Move();
    }
}
