using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestinationChange : MonoBehaviour {
    
    public int xPos;
    public int zPos;
    public int yPos;

    void OnTriggerEnter(Collider other)
    {

        if (other.tag == "Townsman")
        {
            xPos = Random.Range(20, 120);
            zPos = Random.Range(-100, 192);
            yPos = Random.Range(0,1);
            this.gameObject.transform.position = new Vector3(xPos, yPos, zPos);
        }
    }
  
}



