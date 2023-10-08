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

    async private void Start()
    {
        print("Starting");
        ws =  new WebSocket("wss://daydun.com:666");
        //ws.OnMessage += WebSocketHandler;
        
        ws.OnOpen += () =>
        {
            Debug.Log("Connection open!");
        };

        ws.OnError += (e) =>
        {
            Debug.Log("Error! " + e);
        };

        ws.OnClose += (e) =>
        {
            Debug.Log("Connection closed!");
        };
        
        ws.OnMessage += (bytes) =>
        {
            var message = Encoding.UTF8.GetString(bytes);
            Debug.Log("OnMessage! " + message);
        };
        
        await ws.Connect();
        
        var data =  
            new Dictionary<string, string>(){
                {"type", "connect"},
                {"name", "lol"},
            };
        
        string json = JsonConvert.SerializeObject(data);
        await ws.SendText(json);
    }
    void Update()
    {
        ws.DispatchMessageQueue();
    }

    /*private void WebSocketHandler(object sender, MessageEventArgs e)
    {
        var data = JsonConvert.DeserializeObject<IDictionary>(e.Data);
        print($"Packet type: {data["type"]}");

        switch (data["type"])
        {
            case "tick":
                chunkManager.HandleTick(data);
                break;
        }
    }*/

    /*public void Interact(string x, string y, string slot)
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
    }*/
    
    /*public void Move(string x, string y)
    {
        var data =  
            new Dictionary<string, string>(){
                {"type", "move"},
                {"x", x},
                {"y", y}
            };

        string json = JsonConvert.SerializeObject(data);
        ws.Send(json);
    }*/
}
