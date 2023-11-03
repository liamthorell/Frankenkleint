using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using NativeWebSocket;
using Newtonsoft.Json;
using System.Text;
public class ConnectionNative : MonoBehaviour
{
    private WebSocket ws;
    private ChunkManager chunkManager;
    private PlayerController playerController;

    private void Awake()
    {
        chunkManager = GetComponent<ChunkManager>();
        playerController = GetComponent<PlayerController>();
    }

    private async void Start()
    {
        print("Starting");
        ws =  new WebSocket("wss://daydun.com:666");
        
        ws.OnMessage += WebSocketHandler;
        
        Invoke(nameof(Connect), 0.5f);
        
        await ws.Connect();
    }

    private async void OnDestroy()
    {
        await ws.Close();
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        ws.DispatchMessageQueue();
#endif
    }

    async void Connect()
    {
        var data =  
            new Dictionary<string, string>(){
                {"type", "connect"},
                {"name", MenuHandler.username},
            };
        
        string json = JsonConvert.SerializeObject(data);
        await ws.SendText(json);
    }

    private void WebSocketHandler(byte[] bytes)
    {
        var message = Encoding.UTF8.GetString(bytes);
        var data = JsonConvert.DeserializeObject<IDictionary>(message);
        
        //print($"Packet type: {data["type"]}");

        switch (data["type"])
        {
            case "tick":
                chunkManager.HandleTick(data);
                playerController.HandleTick(data);
                break;
            case "move":
                chunkManager.HandleMove(data);
                break;
        }
    }

    public async void Interact(string slot, string x, string z, string y = "0", string i = "0")
    {
        if (i[0] != '-') i = "+" + i;
        if (y[0] != '-') y = "+" + y;
        
        var data =  
            new Dictionary<string, string>{
                {"type", "interact"},
                {"x", x + i + "i"},
                {"y", z + y + "i"},
                {"slot", slot}
            };
        string json = JsonConvert.SerializeObject(data);
        await ws.SendText(json);
    }
    
    public async void Move(string x, string z, string y = "0", string i = "0")
    {
        if (i[0] != '-') i = "+" + i;
        if (y[0] != '-') y = "+" + y;
        
        var data =  
            new Dictionary<string, string>{
                {"type", "move"},
                {"x", x + i + "i"},
                {"y", z + y + "i"}
            };
        string json = JsonConvert.SerializeObject(data);
        await ws.SendText(json);
    }
}
