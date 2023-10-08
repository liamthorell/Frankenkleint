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
    private GameObject[,,] chunks = new GameObject[3, 3, 5];

    public GameObject chunkObject;
    public GameObject chunksGameObject;

    public Material blockMaterial;

    public void HandleTick(IDictionary data)
    {
        if (chunks[1, 1, 2] != null)
        {
            Destroy(chunks[1, 1, 2]);
        }

        var chunk = Instantiate(chunkObject, chunksGameObject.transform);
        var chunkController = chunk.GetComponent<ChunkController>();
        chunkController.blockMaterial = blockMaterial;
        chunkController.map = ConvertObject<Dictionary<string,string>[,]>(data["map"]);
        chunkController.entities = ConvertObject<Dictionary<string,object>[]>(data["entities"]);

        chunkController.Init();
        chunkController.GenerateBlocks();
        chunkController.GenerateEntities();
        
        chunks[1, 1, 2] = chunk;
    }
    
    private static TValue ConvertObject<TValue>(object obj)
    {       
        var json = JsonConvert.SerializeObject(obj);
        var res = JsonConvert.DeserializeObject<TValue>(json);   
        return res;
    }
}
