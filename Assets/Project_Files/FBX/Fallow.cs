using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class Fallow : MonoBehaviour 
{
 
 [SerializeField] private Vector3 offset;
 [SerializeField] private Transform target;
 [SerializeField] private float translateSpeed;
 [SerializeField] private float rotationSpeed;

 private void FixedUpdate()
 {
     HandlerTranslation();
     HandleRotation();

 }

 private void HandlerTranslation()
 {
    var targetPosition = target.TransformPoint(offset);
    transform.position = Vector3.Lerp(transform.position, targetPosition, translateSpeed * Time.deltaTime);
     
 }

 private void HandleRotation()
 {
     var direction = target.position - transform.position;
     var rotation = Quaternion.LookRotation(direction, Vector3.up);
     transform.rotation = Quaternion.Lerp(transform.rotation, rotation, rotationSpeed * Time.deltaTime);
 }
    
}