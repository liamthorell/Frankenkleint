using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoordinateSystemHandler : MonoBehaviour
{
    public Camera ownCam;
    public Camera targetCam;

    public Vector2 offset = new();
    
    private Vector2Int resolution = new (0,0);


    private void Start()
    {
        resolution.x = Screen.width;
        resolution.y = Screen.height;
        
        OnScreenResChange();
    }

    private void OnScreenResChange()
    {
        var topRight = ownCam.ScreenToWorldPoint(new Vector3(ownCam.pixelWidth, ownCam.pixelHeight, ownCam.farClipPlane));
            
        print(topRight);

        transform.position = new Vector3(topRight.x + offset.x, topRight.y + offset.y, 1);
    }

    private void Update()
    {
        // on screen res change
        if (resolution.x != Screen.width || resolution.y != Screen.height)
        {
            OnScreenResChange();
        }
        
        // look in forward direction of target cam
        gameObject.transform.rotation = targetCam.transform.rotation;
    }
}
