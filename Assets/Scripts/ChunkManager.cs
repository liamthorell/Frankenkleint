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

    private List<Vector3Int> chunkQueue = new();

    public bool isRendering;

    // very temporary way of doing it
    public BlockTypes blockTypesObject; 
    public List<BlockTypes.BlockType> blockTypes;
    
    public void Awake()
    {
        conn = GetComponent<Connection>();

        blockTypes = ParseBlockTypes(blockTypesObject);
    }

    private List<BlockTypes.BlockType> ParseBlockTypes(BlockTypes types)
    {
        List<BlockTypes.BlockType> res = new();

        foreach (var entry in types.blocks)
        {
            var temp = new BlockTypes.BlockType
            {
                textureIndex = entry.textureIndex,
                name = entry.name,
                modelOverride = entry.modelOverride
            };

            if (entry.modelOverride == null)
            {
                var material = new Material(blockMaterial);
                material.SetFloat("_index", temp.textureIndex);
                temp.material = material;
            }

            res.Add(temp);
        }

        return res;
    }

    public void CreateChunk(Vector3Int position, IDictionary data)
    {
        var map = ConvertObject<Dictionary<string, string>[,]>(data["map"]);
        var entities = ConvertObject<Dictionary<string,object>[]>(data["entities"]);

        var currentChunk = chunks[position.x, position.y, position.z];

        if (currentChunk != null)
        {
            var currentChunkController = currentChunk.GetComponent<ChunkController>();
            
            if (JsonConvert.SerializeObject(currentChunkController.map) == JsonConvert.SerializeObject(map) && JsonConvert.SerializeObject(currentChunkController.entities) == JsonConvert.SerializeObject(entities))
            {
                return;
            } 
            
            Destroy(currentChunk);
        }
        
        

        var chunk = Instantiate(chunkObject, chunksGameObject.transform);
        var chunkController = chunk.GetComponent<ChunkController>();
        chunkController.types = blockTypes;
        chunkController.map = map;
        chunkController.entities = entities;
        chunkController.chunkPosition = ConvertPositionToRelativeZero(position);

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
