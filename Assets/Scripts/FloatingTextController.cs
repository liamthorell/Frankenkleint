using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingTextController : MonoBehaviour
{
    public Camera mainCamera;
    void Update()
    {
        transform.rotation =
            Quaternion.LookRotation(transform.position - mainCamera.transform.position);
    }
}