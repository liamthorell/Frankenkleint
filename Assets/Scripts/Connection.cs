using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using NativeWebSocket;
using Newtonsoft.Json;
using System.Text;
public class Connection : MonoBehaviour
{
    private WebSocket ws;
    private ChunkManager chunkManager;

    private void Awake()
    {
        chunkManager = GetComponent<ChunkManager>();
    }

    private async void Start()
    {
        print("Starting");
        ws =  new WebSocket("wss://daydun.com:666");
        
        ws.OnMessage += WebSocketHandler;
        
        Invoke(nameof(Connect), 0.5f);
        
        await ws.Connect();
    }
    
    void Update()
    {
        ws.DispatchMessageQueue();
    }

    async void Connect()
    {
        var data =  
            new Dictionary<string, string>(){
                {"type", "connect"},
                {"name", "lol"},
            };
        
        string json = JsonConvert.SerializeObject(data);
        await ws.SendText(json);
    }

    private void WebSocketHandler(byte[] bytes)
    {
        var message = Encoding.UTF8.GetString(bytes);
        var data = JsonConvert.DeserializeObject<IDictionary>(message);
        print($"Packet type: {data["type"]}");

        switch (data["type"])
        {
            case "tick":
                chunkManager.HandleTick(data);
                break;
        }
    }

    public async void Interact(string x, string y, string slot)
    {
        var data =  
            new Dictionary<string, string>(){
                {"type", "interact"},
                {"x", x},
                {"y", y},
                {"slot", slot}
            };

        string json = JsonConvert.SerializeObject(data);
        await ws.SendText(json);
    }
    
    public async void Move(string x, string y)
    {
        var data =  
            new Dictionary<string, string>(){
                {"type", "move"},
                {"x", x},
                {"y", y}
            };

        string json = JsonConvert.SerializeObject(data);
        await ws.SendText(json);
    }
}
