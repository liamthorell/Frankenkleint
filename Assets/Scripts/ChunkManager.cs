using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    static public int ViewDistance = 3;
    // Start is called before the first frame update
    private GameObject[,,] chunks = new GameObject[15,15,15];

    public GameObject chunkPrefab;
    public GameObject chuncksGameObject;

    // Update is called once per frame
    void Update()
    {
        
    }

    public void HandleTick(IDictionary data)
    {
        print("yay before bad");
        Instantiate(chunkPrefab, chuncksGameObject.transform);
        print("yay after goo");
    }
}
