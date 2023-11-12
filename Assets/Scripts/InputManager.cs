using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HGS.CallLimiter;
using Newtonsoft.Json;

//using System.Diagnostics;
public class InputManager : MonoBehaviour
{
    private ChunkManager chunkManager;
    private Connection conn;
    private PlayerController playerController;
    public UIController uiController;
    public Camera mainCamera;
    public FreeCam freecam;
    
    Throttle _moveThrottle = new Throttle();
    //Stopwatch sw = new Stopwatch();
    private float jumpTimer = 0f;

    public bool wasd = true;
    
    private void Awake()
    {
        //sw.Start();
        chunkManager = GetComponent<ChunkManager>();
        conn = GetComponent<Connection>();
        playerController = GetComponent<PlayerController>();
    }

    private float CalcBlockPos(float input)
    {
        return Mathf.Abs(input) <= 0.5f ? 0 : Mathf.Sign(input);
    }

    private Vector3 GetBlockPosFromPos(Vector3 pos, Vector3 normal, bool getClickedBlock)
    {
        Vector3 blockPos = new ((int)Math.Floor(pos.x), (int)Math.Floor(pos.y), (int)Math.Floor(pos.z));

        if (getClickedBlock)
        {
            if (normal.x == -1)
            {
                normal.x = 0;
            }

            // todo why doesnt this handle for y? not needed??
            
            if (normal.z == -1)
            {
                normal.z = 0;
            }

            blockPos -= normal;
        }

        return blockPos;
    }

    private void HandleLeftClick()
    {
        RaycastHit hit;
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        
        if (Physics.Raycast(ray, out hit)) {
            // prevent clicking through ui elements
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

            
            var interactPos = GetBlockPosFromPos(hit.point, hit.normal, true);

            if (Mathf.Abs(interactPos.x) > 1 || Mathf.Abs(interactPos.y) > 1 || Mathf.Abs(interactPos.z) > 1)
            {
                return;
            }

            var itemType = chunkManager.GetBlockAtPosition(new Vector3Int((int)interactPos.x, (int)interactPos.y, (int)interactPos.z));
            
            conn.Interact(playerController.GetPickUpSlot(itemType), interactPos.x.ToString(), interactPos.z.ToString(), interactPos.y.ToString());

            var chunkController = hit.transform.parent.GetComponent<ChunkController>();
            
            chunkManager.UpdateSingleChunk(chunkController.chunkPosition.x, chunkController.chunkPosition.y, chunkController.chunkPosition.z);
            
            //Destroy(objectHit.gameObject);
        }
    }

    private void HandleShiftRightClick()
    {
        RaycastHit hit;
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit))
        {
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

            var interactPos = GetBlockPosFromPos(hit.point, hit.normal, true);
            
            if (hit.transform.parent.TryGetComponent<ChunkController>(out var controller))
            {
                var res = new Vector2Int(((int)interactPos.x + 7) % 15, ((int)interactPos.z + 7) % 15);
                
                if (res.x < 0)
                    res.x += 15;

                if (res.y < 0)
                    res.y += 15;
                
                Dictionary<string, object> entity = null;

                foreach (var e in controller.entities)
                {
                    int x = int.Parse((string)e["x"]) + 7;
                    int y = int.Parse((string)e["y"]) + 7;

                    if (x == res.x && y == res.y)
                    {
                        entity = e;
                    }
                }
                

                if (entity == null)
                {
                    var block = controller.map[res.y, res.x];
                    //block.Add("x", res.x.ToString());
                    //block.Add("y", res.y.ToString());
                    uiController.mods.ShowDetailPane(ConvertObject<Dictionary<string, object>>(block));
                    print(block["type"]);
                }
                else
                {
                    print($"entity type: {entity["type"]}");
                    uiController.mods.ShowDetailPane(entity);
                }
            }

            //
            //print(lol["type"]);
            
            //print($"Requested data for {interactPos}");
        }
    }
    
    private void HandleRightClick()
    {
        RaycastHit hit;
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        
        if (Physics.Raycast(ray, out hit)) {
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

            var interactPos = GetBlockPosFromPos(hit.point, hit.normal, false);

            if (Mathf.Abs(interactPos.x) > 1 || Mathf.Abs(interactPos.y) > 1 || Mathf.Abs(interactPos.z) > 1)
            {
                return;
            }
            
            conn.Interact(playerController.GetCurrentSlot(), interactPos.x.ToString(), interactPos.z.ToString(), interactPos.y.ToString());

            var chunkController = hit.transform.parent.GetComponent<ChunkController>();
            
            chunkManager.UpdateSingleChunk(chunkController.chunkPosition.x, chunkController.chunkPosition.y + (int)hit.normal.y, chunkController.chunkPosition.z);
        }
    }

    void Update()
    {
        if (chunkManager.hasNotStarted) return;
        
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
            if (Input.GetKey(KeyCode.LeftShift))
            {
                HandleShiftRightClick();
            }
            else
            {
                HandleRightClick();
            }
        }
        if ((Input.GetKey(KeyCode.Mouse1) && Input.GetKey(KeyCode.Mouse0)) || Input.GetKey(KeyCode.Mouse2))
        {
            freecam.active = true;
            wasd = false;
        }
        else
        {
            freecam.active = false;
            wasd = true;
        }
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            uiController.ToggleModMenu();
        }
        
        if (wasd)
        {
            if (Input.GetKey(KeyCode.W))
            {
                z++;
            }
            if (Input.GetKey(KeyCode.S))
            {
                z--;
            }
            if (Input.GetKey(KeyCode.D))
            {
                x++;
            }
            if (Input.GetKey(KeyCode.A))
            {
                x--;
            }
        }
        else
        {
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
            Vector3Int newPos = TransformXZWithCamera(x, z);
            x = newPos.x;
            z = newPos.z;

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

    public Vector3Int TransformXZWithCamera(int x, int z)
    {
        var localY = mainCamera.transform.localEulerAngles.y;

        (x, z) = localY switch
        {
            < 315 and > 225 => (-z, x),
            < 225 and > 135 => (-x, -z),
            < 135 and > 45 => (z, -x),
            _ => (x, z)
        };

        return new Vector3Int(x, 0, z);
    }

    private void MoveWithThrottle(string x, string y, string z, string xi = "0")
    {
        _moveThrottle.Run(() => chunkManager.MoveAndUpdate(x, y, z, xi), 0.25f);
    }
    
    private static TValue ConvertObject<TValue>(object obj)
    {       
        var json = JsonConvert.SerializeObject(obj);
        var res = JsonConvert.DeserializeObject<TValue>(json);   
        return res;
    }
}