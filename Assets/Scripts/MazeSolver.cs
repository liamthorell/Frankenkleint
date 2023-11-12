using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

public class MazeSolver : MonoBehaviour
{
    public bool isSavingMaze = false;
    private List<Vector4> chunkQueue = new();
    private List<Vector4> preChunkQueue = new();
    private List<Dictionary<string, object>> maze = new();
    private List<Dictionary<string, int>> path = new();
    
    public Connection conn;
    public ChunkManager ChunkManager;
    
    private void Start()
    {
        StartCoroutine(SendChunks());
        InvokeRepeating(nameof(MoveOneStepFromPath), 0f, 0.26f);
    }

    private void Update()
    {
        if (!isSavingMaze) return;

        if (chunkQueue.Count == 0 && preChunkQueue.Count == 0)
        {
            isSavingMaze = false;
            var json = JsonConvert.SerializeObject(maze);
            File.WriteAllText("maze.json", json);
        }
    }


    public void MoveOneStepFromPath()
    {
        if (path.Count > 0)
        {
            print("Current path length: " + path.Count);
            
            var p = path[0];
            path.RemoveAt(0);

            print("Moving to: " + p["x"] + " " + p["y"] + " " + p["z"] + " " + p["xi"]);
            
            ChunkManager.MoveAndUpdate(p["x"].ToString(), p["y"].ToString(), p["z"].ToString(), p["xi"].ToString(), false);
        }
    }

    public void SaveMaze()
    {
        maze.Clear();
        chunkQueue.Clear();
        preChunkQueue.Clear();
        
        for (int xi = 0; xi < 30; xi++)
        {
            for (int y = 0; y < 30; y++)
            {
                for (int z = 0; z < 2; z++)
                {
                    for (int x = 0; x < 2; x++)
                    {
                        preChunkQueue.Add(new Vector4(x,y,z,xi));
                    }    
                }
            }
        }
        
        isSavingMaze = true;
    }

    public void SolveMaze()
    {
        var json = File.ReadAllText("path.json");
        path = JsonConvert.DeserializeObject<List<Dictionary<string, int>>>(json);
    }
    public void SolveMazeReverse()
    {
        var json = File.ReadAllText("path.json");
        var tempPath = JsonConvert.DeserializeObject<List<Dictionary<string, int>>>(json);
        var tempPathInverse = new List<Dictionary<string, int>>();
        foreach (var p in tempPath)  
        {
            tempPathInverse.Add(new Dictionary<string, int>()
            {
                {"x", -p["x"]},
                {"y", -p["y"]},
                {"z", -p["z"]},
                {"xi", -p["xi"]},
            });
        }
        
        tempPathInverse.Reverse();
        
        path = tempPathInverse;
    }
    
    IEnumerator SendChunks()
    {
        while (true)
        {
            int chunksToSendPerFrame = 0;
            while (chunksToSendPerFrame < 5)
            {
                if (preChunkQueue.Count > 0)
                {
                    var chunkPos = preChunkQueue[0];
                    preChunkQueue.RemoveAt(0);

                    chunkQueue.Add(chunkPos);
                    conn.Move((15 * chunkPos.x + 7).ToString(), (15 * chunkPos.z + 7).ToString(), chunkPos.y.ToString(), chunkPos.w.ToString());
                    chunksToSendPerFrame++;
                }
                else
                {
                    break;
                }
            }
            
            yield return null; // Yield to the next frame
        }
    }

    public void HandleMove(IDictionary data)
    {
        if (chunkQueue.Count == 0) return;
        
        var chunkPos = chunkQueue[0];
        chunkQueue.RemoveAt(0);
        maze.Add(new Dictionary<string, object>(){
            {"x", (int)chunkPos.x},
            {"y", (int)chunkPos.y},
            {"z", (int)chunkPos.z},
            {"xi", (int)chunkPos.w},
            {"map", data["map"]}
        });
    }
    
    private static TValue ConvertObject<TValue>(object obj)
    {       
        var json = JsonConvert.SerializeObject(obj);
        var res = JsonConvert.DeserializeObject<TValue>(json);   
        return res;
    }
}
