using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoordinateSystemHandler : MonoBehaviour
{
    public Camera targetCam;
    
    private void Start()
    {
    }

    private void Update()
    {
        gameObject.transform.rotation = targetCam.transform.rotation;
    }
}
