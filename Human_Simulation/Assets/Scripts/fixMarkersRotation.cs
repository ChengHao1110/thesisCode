using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class fixMarkersRotation : MonoBehaviour
{
    Quaternion rotation;
    void Awake()
    {
        rotation = transform.rotation;
    }
    void LateUpdate()
    {
        transform.rotation = rotation;
    }
}
