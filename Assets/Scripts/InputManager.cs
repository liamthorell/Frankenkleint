using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    private Connection conn;
    private ChunkManager chunkManager;
    
    private void Awake ()
    {
        conn = GetComponent<Connection>();
        chunkManager = GetComponent<ChunkManager>();
    }

    void Update()
    {
        if (chunkManager.isRendering) return;
        
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            print(Input.mousePosition);
        }
        
        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            print(Input.mousePosition);
        }
        
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            print("W is pressed");
            chunkManager.IsDoingMove();
            conn.Move("0", "1");
        }
        
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            print("S is pressed");
            chunkManager.IsDoingMove();
            conn.Move("0", "-1");
        }
        
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            print("D is pressed");
            chunkManager.IsDoingMove();
            conn.Move("1", "0");
        }
        
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            print("A is pressed");
            chunkManager.IsDoingMove();
            conn.Move("-1", "0");
        }
        if (Input.GetKeyDown(KeyCode.PageUp))
        {
            print("PageUp is pressed");
            conn.Move("1i", "0");
        }
        if (Input.GetKeyDown(KeyCode.PageDown))
        {
            print("PageDown is pressed");
            conn.Move("-1i", "0");
        }
    }
}
