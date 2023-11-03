using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using HGS.CallLimiter;

public class Mods : MonoBehaviour
{
    public ChunkManager chunkManager;
    
    public bool killAura = false;
    public bool selfKill = false;
    public int viewDistance;
    public int heightDistance;
    public int inventorySize = 10;
    public int inventorySizeI = 10;
    public int inventorySlider = 0;
    
    public int sendX = 0;
    public int sendY = 0;
    public int sendZ = 0;
    public int send4th = 0;
    
    public bool sendRepeat = false;
    public string packetType = "Move";
    
    /*
     * Temporary place for dis
     * Workflow after updating this setting:
     * - rerun ParseBlockTypes()
     * - rerender all chunks
     * Limitations atm:
     * - current implementation stops unknown blocks from being rendered
     * - cant make air transparent
     */
    public Dictionary<string, float> xray = new()
    {
        //{ "dirt", .2f },
        //{ "rock", .2f },
    };
    
    private VisualElement root;
    private UIController uiController;
    public Connection conn;
    public FreeCam freecam;
    public InputManager inputManager;
    public PlayerController playerController;
    
    public BlockTypes blockTypesObject; 
    
    private Debounce xrayDebounce = new Debounce();

    private void Start()
    {
        uiController = GetComponent<UIController>();
        root = uiController.doc.rootVisualElement;


        root.Q<Toggle>("kill-aura").RegisterValueChangedCallback(KillAuraEvent);
        root.Q<Toggle>("self-kill").RegisterValueChangedCallback(SelfKillEvent);
        root.Q<Toggle>("freecam").RegisterValueChangedCallback(FreecamEvent);
        root.Q<Button>("reset-camera-position").RegisterCallback<ClickEvent>(ResetCameraPositionEvent);

        root.Q<Toggle>("remove-old-chunks-on-move").RegisterValueChangedCallback(RemoveOldChunksOnMoveEvent);
        root.Q<Toggle>("remove-old-chunks-on-move").value = chunkManager.removeOldChunksOnMove;

        root.Q<IntegerField>("inventory-size").RegisterValueChangedCallback(InventorySizeEvent);
        root.Q<IntegerField>("inventory-size-i").RegisterValueChangedCallback(InventorySizeIEvent);
        root.Q<SliderInt>("inventory-slider").RegisterValueChangedCallback(InventorySliderEvent);

        
        root.Q<IntegerField>("view-distance").RegisterValueChangedCallback(ViewDistanceEvent);
        root.Q<IntegerField>("height-distance").RegisterValueChangedCallback(HeightDistanceEvent);

        root.Q<Button>("apply-render-distance").RegisterCallback<ClickEvent>(RenderDistanceButtonEvent);
        
        root.Q<IntegerField>("send-x").RegisterValueChangedCallback(SendXEvent);
        root.Q<IntegerField>("send-y").RegisterValueChangedCallback(SendYEvent);
        root.Q<IntegerField>("send-z").RegisterValueChangedCallback(SendZEvent);
        root.Q<IntegerField>("send-4th").RegisterValueChangedCallback(Send4thEvent);
        
        root.Q<Toggle>("send-repeat").RegisterValueChangedCallback(SendRepeatEvent);
        root.Q<RadioButtonGroup>("packet-type").RegisterValueChangedCallback(PacketTypeEvent);
        root.Q<Button>("send-packet").RegisterCallback<ClickEvent>(SendPacketEvent);
        
        // Quick send packet
        root.Q<Button>("send-up").RegisterCallback<ClickEvent>(SendUpPacketEvent);
        root.Q<Button>("send-down").RegisterCallback<ClickEvent>(SendDownPacketEvent);
        root.Q<Button>("send-4-up").RegisterCallback<ClickEvent>(Send4UpPacketEvent);
        root.Q<Button>("send-4-down").RegisterCallback<ClickEvent>(Send4DownPacketEvent);
        
        // Init render distance values
        viewDistance = chunkManager.ViewDistance;
        heightDistance = chunkManager.HeightDistance;
        root.Q<IntegerField>("view-distance").value = viewDistance;
        root.Q<IntegerField>("height-distance").value = heightDistance;
        
        // Init inventory size values
        root.Q<SliderInt>("inventory-slider").highValue = inventorySizeI;
        root.Q<IntegerField>("inventory-size").value = inventorySize;
        root.Q<IntegerField>("inventory-size-i").value = inventorySizeI;


        InitXray();

        InvokeRepeating(nameof(Execute), 2.0f, 0.08f);
    }

    public void Execute()
    {
        KillAuraExecute();
        SelfKillExecute();
        
        SendPacketExecute();
    }

    private void InitXray()
    {
        var xrayContainer = root.Q<VisualElement>("xray-foldout");
        foreach (var entry in blockTypesObject.blocks)
        {
            var slider = new Slider();
            slider.name = "xray-" + entry.name;
            slider.label = entry.name;
            slider.lowValue = 0;
            slider.highValue = 1;
            slider.value = 1;
            slider.style.marginRight = 10;
            if (xray.TryGetValue(entry.name, out var value))
            {
                slider.value = value;
            }

            slider.RegisterValueChangedCallback(evt =>
            {
                xray[entry.name] = evt.newValue;
                xrayDebounce.Run(XrayUpdated, 0.5f, this);
            });

            xrayContainer.Add(slider);
        }
    }

    private void XrayUpdated()
    {
        chunkManager.blockTypes = chunkManager.ParseBlockTypes(blockTypesObject);
        chunkManager.ResetChunks();
    }

    private void KillAuraEvent(ChangeEvent<bool> evt)
    {
        killAura = evt.newValue;
    }

    private void KillAuraExecute()
    {
        if (!killAura) return;
        
        if (chunkManager.chunks.Count == 0) return;
        
        var currentChunk = chunkManager.chunks[chunkManager.ViewDelta][chunkManager.HeightDelta][chunkManager.ViewDelta];
        
        var controller  = currentChunk.GetComponent<ChunkController>();

        foreach (var entity in controller.entities)
        {
            if ((string)entity["type"] != "monster") continue;
            
            int x = int.Parse((string)entity["x"]);
            int y = int.Parse((string)entity["y"]);
                
            if (x > 1 || x < -1 || y > 1 || y < -1) continue;
                
            conn.Interact("-1", (string)entity["x"], (string)entity["y"]);
        }
    }
    
    private void SelfKillEvent(ChangeEvent<bool> evt)
    {
        selfKill = evt.newValue;
    }
    
    private void SelfKillExecute()
    {
        if (!selfKill) return;
        
        conn.Interact("-1", "0", "0");
    }
    
    private void ViewDistanceEvent(ChangeEvent<int> evt)
    {
        viewDistance = evt.newValue;
    }
    
    private void HeightDistanceEvent(ChangeEvent<int> evt)
    {
        heightDistance = evt.newValue;
    }

    private void RenderDistanceButtonEvent(ClickEvent evt)
    {
        chunkManager.ViewDistance = viewDistance;
        chunkManager.HeightDistance = heightDistance;
        chunkManager.UpdateDistanceDelta();
        chunkManager.ResetChunks();
    }
    
    private void FreecamEvent(ChangeEvent<bool> evt)
    {
        var newValue = evt.newValue;

        freecam.active = newValue;
        inputManager.wasd = !newValue;
    }
    
    private void InventorySizeEvent(ChangeEvent<int> evt)
    {
        inventorySize = evt.newValue;
        
        uiController.UpdateInventory();
    }
    
    private void InventorySizeIEvent(ChangeEvent<int> evt)
    {
        inventorySizeI = evt.newValue;
        root.Q<SliderInt>("inventory-slider").highValue = inventorySizeI;
    }
    
    private void InventorySliderEvent(ChangeEvent<int> evt)
    {
        inventorySlider = evt.newValue;
        root.Q<SliderInt>("inventory-slider").label = "Inventory i: " + inventorySlider;
        
        uiController.UpdateInventory();
    }

    private void RemoveOldChunksOnMoveEvent(ChangeEvent<bool> evt)
    {
        chunkManager.removeOldChunksOnMove = evt.newValue;
    }
    
    private void ResetCameraPositionEvent(ClickEvent evt)
    {
        freecam.SetDefaultCameraPos();
    }
    
    private void SendXEvent(ChangeEvent<int> evt)
    {
        sendX = evt.newValue;
    }
    
    private void SendYEvent(ChangeEvent<int> evt)
    {
        sendY = evt.newValue;
    }
    
    private void SendZEvent(ChangeEvent<int> evt)
    {
        sendZ = evt.newValue;
    }
    
    private void Send4thEvent(ChangeEvent<int> evt)
    {
        send4th = evt.newValue;
    }
    
    private void SendRepeatEvent(ChangeEvent<bool> evt)
    {
        sendRepeat = evt.newValue;
    }
    
    private void PacketTypeEvent(ChangeEvent<int> evt)
    {
        if (evt.newValue == 0)
        {
            packetType = "Move";
        } else if (evt.newValue == 1)
        {
            packetType = "Interact";
        }
    }

    private void SendPacketExecute()
    {
        if (!sendRepeat) return;

        if (packetType == "Move")
        {
            chunkManager.MoveAndUpdate(sendX.ToString(), sendY.ToString(), sendZ.ToString(), send4th.ToString());
        }
        else if (packetType == "Interact")
        {
            conn.Interact(playerController.GetCurrentSlot(), sendX.ToString(), sendZ.ToString(), sendY.ToString(), send4th.ToString());
        }
    }

    private void SendPacketEvent(ClickEvent evt)
    {
        if (packetType == "Move")
        {
            chunkManager.MoveAndUpdate(sendX.ToString(), sendY.ToString(), sendZ.ToString(), send4th.ToString());
        }
        else if (packetType == "Interact")
        {
            conn.Interact(playerController.GetCurrentSlot(), sendX.ToString(), sendZ.ToString(), sendY.ToString(), send4th.ToString());
        }
    }
    
    private void SendUpPacketEvent(ClickEvent evt)
    {
        if (packetType == "Move")
        {
            chunkManager.MoveAndUpdate("0", "1", "0", "0");
        }
        else if (packetType == "Interact")
        {
            conn.Interact(playerController.GetCurrentSlot(), "0", "0", "1", "0");
        }
    }
    
    private void SendDownPacketEvent(ClickEvent evt)
    {
        if (packetType == "Move")
        {
            chunkManager.MoveAndUpdate("0", "-1", "0", "0");
        }
        else if (packetType == "Interact")
        {
            conn.Interact(playerController.GetCurrentSlot(), "0", "0", "-1", "0");
        }
    }
    
    private void Send4UpPacketEvent(ClickEvent evt)
    {
        if (packetType == "Move")
        {
            chunkManager.MoveAndUpdate("0", "0", "0", "1");
        }
        else if (packetType == "Interact")
        {
            conn.Interact(playerController.GetCurrentSlot(), "0", "0", "0", "1");
        }
    }
    
    private void Send4DownPacketEvent(ClickEvent evt)
    {
        if (packetType == "Move")
        {
            chunkManager.MoveAndUpdate("0", "0", "0", "-1");
        }
        else if (packetType == "Interact")
        {
            conn.Interact(playerController.GetCurrentSlot(), "0", "0", "0", "-1");
        }
    }
}