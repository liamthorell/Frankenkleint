using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HGS.CallLimiter;
using System.Diagnostics;

public class InputManager : MonoBehaviour
{
    private Connection conn;
    private ChunkManager chunkManager;
    public Camera mainCamera;
    
    Throttle _moveThrottle = new Throttle();
    Stopwatch sw = new Stopwatch();
    
    
    private void Awake ()
    {
        sw.Start();
        conn = GetComponent<Connection>();
        chunkManager = GetComponent<ChunkManager>();
    }

    void Update()
    {
        if (chunkManager.isRendering) return;
        //if (sw.Elapsed.TotalSeconds < 0.3) return;
        //sw.Reset();
        //sw.Start();
        
        int x = 0;
        int z = 0;
        
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            //
        }
        if (Input.GetKey(KeyCode.Mouse1))
        {
            //
        }
        if (Input.GetKey(KeyCode.UpArrow))
        {
            z++;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            z--;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            x++;
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            x--;
        }
        
        if (x != 0 || z != 0)
        {
            var localY = mainCamera.transform.localEulerAngles.y;

            (x, z) = localY switch
            {
                < 315 and > 225 => (-z, x),
                < 225 and > 135 => (-x, -z),
                < 135 and > 45 => (z, -x),
                _ => (x, z)
            };
            
            _moveThrottle.Run(() => chunkManager.MoveAndUpdate(x.ToString(), z.ToString()), 0.3f);
            
            //chunkManager.MoveAndUpdate(x.ToString(), z.ToString());
        }
        
        /*if (Input.GetKeyDown(KeyCode.PageUp))
        {
            print("PageUp is pressed");
            chunkManager.IsDoingMove("1i", "0");
        }
        if (Input.GetKeyDown(KeyCode.PageDown))
        {
            print("PageDown is pressed");
            chunkManager.IsDoingMove("-1i", "0");
        }*/
    }
}
