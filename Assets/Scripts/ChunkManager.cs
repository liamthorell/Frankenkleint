using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEngine.Serialization;

public class ChunkManager : MonoBehaviour
{
     public static int ViewDistance = 3;
     public static int HeightDistance = 7;
     public static int ViewDelta = (ViewDistance - 1) / 2;
     public static int HeightDelta = (HeightDistance - 1) / 2;
    
    
    private GameObject[,,] chunks = new GameObject[ViewDistance, HeightDistance, ViewDistance];

    public GameObject chunkObject;
    public GameObject chunksGameObject;

    public Material blockMaterial;

    private Connection conn;

    private List<Vector3Int> chunkQueue = new List<Vector3Int>();

    public bool isRendering;

    public void Awake()
    {
        conn = GetComponent<Connection>();
    }

    public void CreateChunk(Vector3Int position, IDictionary data)
    {
        if (chunks[position.x, position.y, position.z] != null)
        {
            Destroy(chunks[position.x, position.y, position.z]);
        }

        var chunk = Instantiate(chunkObject, chunksGameObject.transform);
        var chunkController = chunk.GetComponent<ChunkController>();
        chunkController.blockMaterial = blockMaterial;
        chunkController.map = ConvertObject<Dictionary<string,string>[,]>(data["map"]);
        chunkController.entities = ConvertObject<Dictionary<string,object>[]>(data["entities"]);
        chunkController.chunkPosition = ConvertPositionToRelativeZero(position);

        chunkController.Init();
        chunkController.GenerateBlocks();
        chunkController.GenerateEntities();
        
        chunks[position.x, position.y, position.z] = chunk;
    }

    public void HandleTick(IDictionary data)
    {
        isRendering = true;
        CreateChunk(new Vector3Int(ViewDelta, HeightDelta, ViewDelta), data);
        FetchAllChunks();
    }

    private Vector3Int ConvertPositionToRelativeZero(Vector3Int pos)
    {
        return new Vector3Int(pos.x - ViewDelta, pos.y - HeightDelta, pos.z - ViewDelta);
    }

    private void  FetchAllChunks()
    {
        for (int y = -HeightDelta; y <= HeightDelta; y++)
        {
            for (int x = -ViewDelta; x <= ViewDelta; x++)
            {
                if (y == 0 && x == 0) continue;
                chunkQueue.Add(new Vector3Int(x + ViewDelta,y + HeightDelta,0 + ViewDelta));
                conn.Move((15 * x).ToString(), y + "i");
            }

            /*if (y != 0) continue;
            
            for (int z = -ViewDelta; z <= ViewDelta; z++)
            {
                if (z == 0) continue;
                for (int x = -ViewDelta; x <= ViewDelta; x++)
                {
                    chunkQueue.Add(new Vector3Int(x + ViewDelta,0 + HeightDelta,z + ViewDelta));
                    conn.Move((15 * x).ToString(), (15 * z).ToString());
                }
            }*/
        }
    }

    public void HandleMove(IDictionary data)
    {
        if (!isRendering) return;
        
        Vector3Int chunkPos = chunkQueue[0];
        chunkQueue.RemoveAt(0);

        if (chunkPos.x == -1) return;
        
        CreateChunk(new Vector3Int(chunkPos.x, chunkPos.y, chunkPos.z), data);
        
        // in the end
        if (chunkQueue.Count == 0) isRendering = false;
    }
    
    private static TValue ConvertObject<TValue>(object obj)
    {       
        var json = JsonConvert.SerializeObject(obj);
        var res = JsonConvert.DeserializeObject<TValue>(json);   
        return res;
    }
}
