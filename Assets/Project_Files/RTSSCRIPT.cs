using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RTSSCRIPT : MonoBehaviour
{

    float speed = 0.04f;
    float zoomSpeed = 10.0f;
    float rotateSPeed = 1f;

    float maxHeight = 50f;
    float minHeight = 0f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKey(KeyCode.LeftShift)){
            speed = 0.04f;
            zoomSpeed = 10.0f;
        }
        else{
            speed = 0.02f;
            zoomSpeed = 5.0f;
        }
        float hsp = transform.position.y * speed * Input.GetAxis("Horizontal");
        float vsp = transform.position.y * speed * Input.GetAxis("Vertical");
        float scrollSp = Mathf.Log(transform.position.y) * -zoomSpeed * Input.GetAxis("Mouse ScrollWheel");

        if ((transform.position.y >= maxHeight) && (scrollSp >0))
        {
            scrollSp = 0;
        }
        else if ((transform.position.y <= minHeight) && (scrollSp <0))
        {
            scrollSp = 0;
        }

        Vector3 verticalMove = new Vector3(0,scrollSp,0);
        Vector3 lateralMove = hsp * transform.right;
        Vector3 forwardMove = transform.forward;
        forwardMove.y = 0;
        forwardMove.Normalize();
        forwardMove *= vsp;

        Vector3 move = verticalMove + lateralMove + forwardMove;

        transform.position += move;
    }
}
