using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using Newtonsoft.Json;

public class Connection : MonoBehaviour
{
    private WebSocket ws;
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

    private void OnDestroy()
    {
        ws.Close();
    }

    private void WebSocketHandler(object sender, MessageEventArgs e)
    {
        print($"Packets {e.Data}");
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
