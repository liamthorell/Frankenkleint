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
    public int ViewDistance = 3;
    public int HeightDistance = 7;
    public int ViewDelta = 0;
    public int HeightDelta = 0;
     
    public bool removeOldChunksOnMove = true;
    
    public List<List<List<GameObject>>> chunks = new();

    public GameObject chunkObject;
    public GameObject chunksGameObject;
    public Mods mods;

    public Material blockMaterial;
    public Material blockTransparentMaterial;

    private Connection conn;
    
    private List<Vector3Int> preChunkQueue = new();
    private List<Vector3Int> chunkQueue = new();
    private List<IDictionary> chunkDataQueue = new();

    // very temporary way of doing it
    public BlockTypes blockTypesObject; 
    public List<BlockTypes.BlockType> blockTypes;
    
    Debounce _updateChunksDebounce = new Debounce();

    public void UpdateDistanceDelta()
    {
        ViewDelta = (ViewDistance - 1) / 2;
        HeightDelta = (HeightDistance - 1) / 2;
    }

    public void ResetChunks()
    {
        // Loop through all chunks and destroy them
        foreach (var chunk in chunks)
        {
            foreach (var chunkY in chunk)
            {
                foreach (var chunkZ in chunkY)
                {
                    Destroy(chunkZ);
                }
            }
        }
        
        chunks.Clear();
        InitChunks();
        InvalidateChunkQueue();
        AddAllChunksToQueue();
    }

    public void InitChunks()
    {
        for (int x = 0; x < ViewDistance; x++)
        {
            chunks.Add(new List<List<GameObject>>());
            for (int y = 0; y < HeightDistance; y++)
            {
                chunks[x].Add(new List<GameObject>());
                for (int z = 0; z < ViewDistance; z++)
                {
                    chunks[x][y].Add(null);
                }
            }
        }
    }
    
    public void Awake()
    {
        conn = GetComponent<Connection>();

        blockTypes = ParseBlockTypes(blockTypesObject);
        
        InitChunks();
        UpdateDistanceDelta();
    }

    public void Start()
    {
        StartCoroutine(ScheduleChunkGeneration());
    }

    public void DestroyAllChunks()
    {
        for (int i = 0; i < ViewDistance; i++)
        {
            for (int i2 = 0; i2 < HeightDistance; i2++)
            {
                for (int i3 = 0; i3 < ViewDistance; i3++)
                {
                    if (i == ViewDelta && i3 == ViewDelta) continue;
                    Destroy(chunks[i][i2][i3]);
                }
            }
        }
    }
    
    public void MoveAndUpdate(string x, string y, string z, string xi = "0")
    {
        
        chunkQueue.Add(new Vector3Int(-1,-1,-1));
        conn.Move(x, z, y, xi);
        InvalidateChunkQueue();
        
        if (removeOldChunksOnMove) DestroyAllChunks();

        for (int yy = -HeightDelta; yy <= HeightDelta; yy++)
        {
            UpdateSingleChunk(0, yy, 0);
        }

        _updateChunksDebounce.Run(AddAllChunksToQueue, 0.5f, this);

        //AddAllChunksToQueue();
    }
    
    public void UpdateSingleChunk(int x, int y, int z)
    {
        chunkQueue.Add(new Vector3Int(x + ViewDelta, y + HeightDelta, z + ViewDelta));
        conn.Move((15 * x).ToString(), (15 * z).ToString(), y.ToString());
    }

    private void InvalidateChunkQueue()
    {
        int chunkCount = chunkQueue.Count;
        chunkQueue = new List<Vector3Int>();
        
        int preChunkCount = preChunkQueue.Count;
        preChunkQueue = new List<Vector3Int>();

        for (int i = 0; i < chunkCount; i++)
        {
            chunkQueue.Add(new Vector3Int(-1,-1,-1));
        }
        
        for (int i = 0; i < preChunkCount; i++)
        {
            preChunkQueue.Add(new Vector3Int(-999,-999,-999));
        }
    }

    public List<BlockTypes.BlockType> ParseBlockTypes(BlockTypes types)
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


            // block material logic
            if (entry.modelOverride == null)
            {
                if (entry.material == null)
                {
                    bool materialSet = false;

                    Material material;
                    if (mods.xray.TryGetValue(entry.name, out var value)) // use transparent material if in xray list
                    {
                        if (value == 0f)
                        {
                            print($"gg i am {entry.name}");
                            material = null;
                        }
                        else
                        {
                            material = new Material(blockTransparentMaterial);
                            material.SetFloat("_opacity", value);
                            materialSet = true;
                        }
                    }
                    else
                    {
                        material = new Material(blockMaterial);
                        materialSet = true;
                    }

                    if (materialSet)
                    {
                        material.SetFloat("_index", temp.textureIndex);
                    }
                    temp.material = material;
                }
                else
                {
                    var material = new Material(entry.material);
                    material.SetFloat("_index", temp.textureIndex);
                    temp.material = material;
                }
            }

            res.Add(temp);
        }

        return res;
    }

    public void CreateChunk(Vector3Int position, IDictionary data)
    {
        var map = ConvertObject<Dictionary<string, string>[,]>(data["map"]);
        var entities = ConvertObject<Dictionary<string,object>[]>(data["entities"]);

        var currentChunk = chunks[position.x][position.y][position.z];

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
        
        chunks[position.x][position.y][position.z] = chunk;
    }
    
    
    IEnumerator ScheduleChunkGeneration()
    {
        while (true)
        {
            int chunksPerFrame = 0;
            while (chunksPerFrame < 3)
            {
                if (chunkQueue.Count > 0 && chunkDataQueue.Count > 0)
                {
                    var chunkPos = chunkQueue[0];
                    var chunkData = chunkDataQueue[0];
                    chunkQueue.RemoveAt(0);
                    chunkDataQueue.RemoveAt(0);
                    
                    if (chunkPos.x != -1)
                    {
                        CreateChunk(new Vector3Int(chunkPos.x, chunkPos.y, chunkPos.z), chunkData);
                        chunksPerFrame++;
                    };
                }
                else
                {
                    break;
                }
            }
            
            int chunksToSendPerFrame = 0;
            while (chunksToSendPerFrame < HeightDistance)
            {
                if (preChunkQueue.Count > 0)
                {
                    var chunkPos = preChunkQueue[0];
                    preChunkQueue.RemoveAt(0);

                    if (chunkPos.x != -999)
                    {
                        chunkQueue.Add(new Vector3Int(chunkPos.x + ViewDelta, chunkPos.y + HeightDelta,
                            chunkPos.z + ViewDelta));
                        conn.Move((15 * chunkPos.x).ToString(), (15 * chunkPos.z).ToString(), chunkPos.y.ToString());
                        chunksToSendPerFrame++;
                    }
                }
                else
                {
                    break;
                }
            }
            
            yield return null; // Yield to the next frame
        }
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
                
                //if (y == 0 && z == 0 && x == 0) continue;
                
                preChunkQueue.Add(new Vector3Int(x, y, z));
                //chunkQueue.Add(new Vector3Int(x + ViewDelta, y + HeightDelta, z + ViewDelta));
                //conn.Move((15 * x).ToString(), (15 * z).ToString(), y.ToString());
            }
        }
    }

    public void HandleMove(IDictionary data)
    {
        chunkDataQueue.Add(data);
    }
    
    private static TValue ConvertObject<TValue>(object obj)
    {       
        var json = JsonConvert.SerializeObject(obj);
        var res = JsonConvert.DeserializeObject<TValue>(json);   
        return res;
    }
}
