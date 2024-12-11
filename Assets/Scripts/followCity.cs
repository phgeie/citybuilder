using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class followCity : MonoBehaviour
{
    public Transform target;
    private Vector3 localOffset;

    void Start()
    { 
        localOffset = target.InverseTransformPoint(transform.position);
    }

    void Update()
    {
        if (target != null)
        {
            transform.position = target.TransformPoint(localOffset);
            // Die Rotation des Ziels direkt übernehmen
            transform.rotation = target.rotation;
        }
    }
}
