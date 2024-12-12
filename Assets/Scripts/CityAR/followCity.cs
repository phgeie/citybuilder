using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class followCity : MonoBehaviour
{
    public Transform target;
    private Vector3 localOffset;
    private Quaternion startRotationOffset;

    void Start()
    { 
        localOffset = target.InverseTransformPoint(transform.position);
        startRotationOffset = Quaternion.Inverse(target.rotation) * transform.rotation;
    }

    void Update()
    {
        if (target != null){
            transform.position = target.TransformPoint(localOffset);
            transform.rotation = target.rotation * startRotationOffset;
        }
    }
}