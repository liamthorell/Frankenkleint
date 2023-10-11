using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEngine.Serialization;
using System.Linq;
using HGS.CallLimiter;

public class ChunkManager : MonoBehaviour
{
     public static int ViewDistance = 3;
     public static int HeightDistance = 3;
     public static int ViewDelta = (ViewDistance - 1) / 2;
     public static int HeightDelta = (HeightDistance - 1) / 2;
    
    
    private GameObject[,,] chunks = new GameObject[ViewDistance, HeightDistance, ViewDistance];

    public GameObject chunkObject;
    public GameObject chunksGameObject;

    public Material blockMaterial;

    private Connection conn;

    private List<Vector3Int> chunkQueue = new();

    // very temporary way of doing it
    public BlockTypes blockTypesObject; 
    public List<BlockTypes.BlockType> blockTypes;
    
    Debounce _updateChunksDebounce = new Debounce();
    
    public void Awake()
    {
        conn = GetComponent<Connection>();

        blockTypes = ParseBlockTypes(blockTypesObject);
    }

    public void MoveAndUpdate(string x, string y, string z, string xi = "0")
    {
        
        //Debug.LogWarning("move and update");
        
        chunkQueue.Add(new Vector3Int(-1,-1,-1));
        conn.Move(x, z, y, xi);
        //InvalidateChunkQueue();

        //_updateChunksDebounce.Run(AddAllChunksToQueue, 0.5f, this);

        AddAllChunksToQueue();
    }

    private void InvalidateChunkQueue()
    {
        int chunkCount = chunkQueue.Count;
        chunkQueue = new List<Vector3Int>();

        for (int i = 0; i < chunkCount; i++)
        {
            chunkQueue.Add(new Vector3Int(-1,-1,-1));
        }
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

        // benchmark this!
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
        CreateChunk(new Vector3Int(ViewDelta,HeightDelta,ViewDelta), data);
    }

    private Vector3Int ConvertPositionToRelativeZero(Vector3Int pos)
    {
        return new Vector3Int(pos.x - ViewDelta, pos.y - HeightDelta, pos.z - ViewDelta);
    }

    private void  AddAllChunksToQueue()
    {
        /*for (int y = -HeightDelta; y <= HeightDelta; y++)
        {
            for (int x = -ViewDelta; x <= ViewDelta; x++)
            {
                for (int z = -ViewDelta; z <= ViewDelta; z++)
                {
                    //if (y == 0 && x == 0 && x == 0) continue;
                    chunkQueue.Add(new Vector3Int(x + ViewDelta, y + HeightDelta, z + ViewDelta));
                    conn.Move((15 * x).ToString(), (15 * z).ToString(), y.ToString());
                }
            }
        }*/
        
        List<int[]> permutations = new List<int[]>();

        for (int x = -ViewDelta; x <= ViewDelta; x++)
        {
            for (int z = -ViewDelta; z <= ViewDelta; z++)
            {
                permutations.Add(new int[] { x, z });
            }
        }
        
        var allXZ = permutations.OrderBy(arr => Math.Abs(arr[0]) + Math.Abs(arr[1])).ToList();

        foreach (var xz in allXZ)
        {
            for (int y = -HeightDelta; y <= HeightDelta; y++)
            {
                int x = xz[0];
                int z = xz[1];
                
                if (y == 0 && z == 0 && x == 0) continue;
                chunkQueue.Add(new Vector3Int(x + ViewDelta, y + HeightDelta, z + ViewDelta));
                conn.Move((15 * x).ToString(), (15 * z).ToString(), y.ToString());
            }
        }
    }

    public void HandleMove(IDictionary data)
    {
        Vector3Int chunkPos = chunkQueue[0];
        chunkQueue.RemoveAt(0);

        if (chunkPos.x == -1) return;

        CreateChunk(new Vector3Int(chunkPos.x, chunkPos.y, chunkPos.z), data);
    }
    
    private static TValue ConvertObject<TValue>(object obj)
    {       
        var json = JsonConvert.SerializeObject(obj);
        var res = JsonConvert.DeserializeObject<TValue>(json);   
        return res;
    }
}
