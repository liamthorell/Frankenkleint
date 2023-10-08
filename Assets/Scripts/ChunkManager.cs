using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEngine.Serialization;

public class ChunkManager : MonoBehaviour
{
    static public int ViewDistance = 3;
    static public int HeightDistance = 5;
    
    private GameObject[,,] chunks = new GameObject[3, 5, 3];

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
        CreateChunk(new Vector3Int(1, 2, 1), data);
        FetchAllChunks();
    }

    private Vector3Int ConvertPositionToRelativeZero(Vector3Int pos)
    {
        var view = (ViewDistance - 1) / 2;
        var height = (HeightDistance - 1) / 2;
        return new Vector3Int(pos.x - view, pos.y - height, pos.z - view);
    }

    private void FetchAllChunks()
    {
        int height = (HeightDistance - 1) / 2;

        for (int i = -height; i <= height; i++)
        {
            if (i == 0) continue;
            chunkQueue.Add(new Vector3Int(1,i + height,1));
            conn.Move("0", i + "i");
        }
    }

    public void HandleMove(IDictionary data)
    {
        if (!isRendering) return;
        
        Vector3Int chunkPos = chunkQueue[0];
        chunkQueue.RemoveAt(0);
        
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
