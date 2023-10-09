using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.WebSockets;
using UnityEngine;
using Websocket.Client;
using Newtonsoft.Json;
using System.Text;
public class Connection : MonoBehaviour
{
    private WebsocketClient ws;
    private ChunkManager chunkManager;
    private bool newPacket;
    private ResponseMessage resp;

    private void Awake()
    {
        newPacket = false;
        chunkManager = GetComponent<ChunkManager>();
    }

    private async void Start()
    {
        print("Starting");
        var url = new Uri("wss://daydun.com:666");
        ws = new WebsocketClient(url);
        
        ws.MessageReceived.Subscribe((msg) =>
        {
            resp = msg;
            newPacket = true;
        });
        await ws.Start();
        
        Connect();
    }

    private async void OnDestroy()
    {
        await ws.Stop(WebSocketCloseStatus.NormalClosure, "Closed");
    }

    void Connect()
    {
        var data =  
            new Dictionary<string, string>(){
                {"type", "connect"},
                {"name", "balls420"},
            };
        
        string json = JsonConvert.SerializeObject(data);
        ws.Send(json);
        print("Done connecting");
    }

    private void Update()
    {
        if (newPacket)
        {
            newPacket = false;
            WebSocketHandler();
        }
    }

    private void WebSocketHandler()
    {
        if (resp == null || resp.MessageType != WebSocketMessageType.Text || resp.Text == null) return;

        var data = JsonConvert.DeserializeObject<IDictionary>(resp.Text);
        
        print($"Packet type: {data["type"]}");

        switch (data["type"])
        {
            case "tick":
                chunkManager.HandleTick(data);
                break;
            case "move":
                chunkManager.HandleMove(data);
                break;
        }
    }

    public void Interact(string x, string y, string slot)
    {
        var data =  
            new Dictionary<string, string>{
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
            new Dictionary<string, string>{
                {"type", "move"},
                {"x", x},
                {"y", y}
            };

        string json = JsonConvert.SerializeObject(data);
        ws.Send(json);
        print("Done moving");
    }
}
