using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;

public class MazeSolver : MonoBehaviour
{
    
    public List<Vector4> chunkQueue = new();
    // Start is called before the first frame update
    void Start()
    {
        // Write file using StreamWriter
        using (StreamWriter writer = new StreamWriter("maze.txt"))
        {
            writer.WriteLine("test123");
            writer.WriteLine("test123");
            writer.WriteLine("test123");
        }
    }

    public void HandleMove(IDictionary data)
    {
        var map = ConvertObject<Dictionary<string, string>[,]>(data["map"]);
        return;
    }
    
    private static TValue ConvertObject<TValue>(object obj)
    {       
        var json = JsonConvert.SerializeObject(obj);
        var res = JsonConvert.DeserializeObject<TValue>(json);   
        return res;
    }
}
