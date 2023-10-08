using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using Newtonsoft.Json;

public class Connection : MonoBehaviour
{
    private WebSocket ws;
    private ChunkManager chunkManager;

    private void Awake()
    {
        chunkManager = GetComponent<ChunkManager>();
    }

    private void Start()
    {
        print("Starting");
        ws =  new WebSocket("wss://daydun.com:666");
        ws.OnMessage += WebSocketHandler;
        
        ws.Connect();
        
        var data =  
            new Dictionary<string, string>(){
                {"type", "connect"},
                {"name", "lol"},
            };
        
        string json = JsonConvert.SerializeObject(data);
        ws.Send(json);
    }

    private void WebSocketHandler(object sender, MessageEventArgs e)
    {
        var data = JsonConvert.DeserializeObject<IDictionary>(e.Data);
        print($"Packet type: {data["type"]}");

        switch (data["type"])
        {
            case "tick":
                chunkManager.HandleTick(data);
                break;
        }
    }

    public void Interact(string x, string y, string slot)
    {
        var data =  
            new Dictionary<string, string>(){
                {"type", "interact"},
                {"x", x},
                {"y", y},
                {"slot", slot}
            };

        string json = JsonConvert.SerializeObject(data);
        ws.Send(json);
    }
    
    public void Move(string x, string y)
    {
        var data =  
            new Dictionary<string, string>(){
                {"type", "move"},
                {"x", x},
                {"y", y}
            };

        string json = JsonConvert.SerializeObject(data);
        ws.Send(json);
    }
}
