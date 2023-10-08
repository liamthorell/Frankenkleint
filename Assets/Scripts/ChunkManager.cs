using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    static public int ViewDistance = 3;
    private GameObject[,,] chunks = new GameObject[3, 3, 6];

    public GameObject chunkObject;
    public GameObject chuncksGameObject;

    void Update()
    {
        
    }

    private void Start()
    {
        
    }

    public void HandleTick(IDictionary data)
    {
        if (chunks[1, 1, 2] != null)
        {
            Destroy(chunks[1,1,2]);
        }
        var chunk = Instantiate(chunkObject, chuncksGameObject.transform);
        chunks[1, 1, 2] = chunk;
    }
}
