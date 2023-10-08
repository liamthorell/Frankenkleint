using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    private Connection conn;
    
    private void Awake ()
    {
        conn = GetComponent<Connection>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            print(Input.mousePosition);
        }
        
        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            print(Input.mousePosition);
        }
        
        if (Input.GetKeyDown(KeyCode.W))
        {
            print("W is pressed");
            conn.Move("0", "1");
        }
        
        if (Input.GetKeyDown(KeyCode.S))
        {
            print("S is pressed");
            conn.Move("0", "-1");
        }
        
        if (Input.GetKeyDown(KeyCode.D))
        {
            print("D is pressed");
            conn.Move("1", "0");
        }
        
        if (Input.GetKeyDown(KeyCode.A))
        {
            print("A is pressed");
            conn.Move("-1", "0");
        }
    }
}
