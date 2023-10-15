using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HGS.CallLimiter;
//using System.Diagnostics;
public class InputManager : MonoBehaviour
{
    private ChunkManager chunkManager;
    private Connection conn;
    private PlayerController playerController;
    public Camera mainCamera;
    
    Throttle _moveThrottle = new Throttle();
    //Stopwatch sw = new Stopwatch();
    private float jumpTimer = 0f;
    
    
    private void Awake()
    {
        //sw.Start();
        chunkManager = GetComponent<ChunkManager>();
        conn = GetComponent<Connection>();
        playerController = GetComponent<PlayerController>();
    }

    private void HandleLeftClick()
    {
        RaycastHit hit;
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        
        if (Physics.Raycast(ray, out hit)) {
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

            Transform objectHit = hit.transform;
            var position = Vector3Int.FloorToInt(objectHit.position);
            
            if (position.x > 1 ||  position.x < -1 || position.y > 1 || position.y < -1 || position.z > 1 || position.z < -1) return;
            
            conn.Interact("-1", position.x.ToString(), position.z.ToString(), position.y.ToString());
            
            var chunkController = objectHit.parent.GetComponent<ChunkController>();
            
            chunkManager.UpdateSingleChunk(chunkController.chunkPosition.x, chunkController.chunkPosition.y, chunkController.chunkPosition.z);
            
            Destroy(objectHit.gameObject);
        }
    }
    
    private void HandleRightClick()
    {
        RaycastHit hit;
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        
        if (Physics.Raycast(ray, out hit)) {
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

            Transform objectHit = hit.transform;
            var position = Vector3Int.FloorToInt(objectHit.position + hit.normal);

            conn.Interact(playerController.currentSlot, position.x.ToString(), position.z.ToString(), position.y.ToString());

            var chunkController = objectHit.parent.GetComponent<ChunkController>();
            
            chunkManager.UpdateSingleChunk(chunkController.chunkPosition.x, chunkController.chunkPosition.y, chunkController.chunkPosition.z);
        }
    }

    void Update()
    {
        //if (sw.Elapsed.TotalSeconds < 0.3) return;
        //sw.Reset();
        //sw.Start();
        
        int x = 0;
        int z = 0;
        int y = 0;
        int xi = 0;
        jumpTimer -= Time.deltaTime;
        
        if (Input.GetKeyDown(KeyCode.Mouse0) && !Input.GetKeyDown(KeyCode.Mouse1))
        {
            HandleLeftClick();
        }
        if (Input.GetKeyDown(KeyCode.Mouse1) && !Input.GetKeyDown(KeyCode.Mouse0))
        {
            HandleRightClick();
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
        if (Input.GetKey(KeyCode.Comma))
        {
            xi--;
        }
        if (Input.GetKey(KeyCode.Period))
        {
            xi++;
        }

        if (Input.GetKey(KeyCode.Space))
        {
            jumpTimer = 0.5f;
        }
        
        if (x != 0 || z != 0 || xi != 0)
        {
            var localY = mainCamera.transform.localEulerAngles.y;

            (x, z) = localY switch
            {
                < 315 and > 225 => (-z, x),
                < 225 and > 135 => (-x, -z),
                < 135 and > 45 => (z, -x),
                _ => (x, z)
            };

            if (jumpTimer > 0f) y += 1;

            MoveWithThrottle(x.ToString(), y.ToString(), z.ToString(), xi.ToString());

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

    private void MoveWithThrottle(string x, string y, string z, string xi = "0")
    {
        _moveThrottle.Run(() => chunkManager.MoveAndUpdate(x, y, z, xi), 0.3f);
    }
}
