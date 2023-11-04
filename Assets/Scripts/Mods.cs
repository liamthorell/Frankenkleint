using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using HGS.CallLimiter;
using Newtonsoft.Json;

public class Mods : MonoBehaviour
{
    public ChunkManager chunkManager;
    
    public bool killAura = true;
    public bool selfKill = false;
    public bool autoPickup = true;
    public bool autoMine = false;
    public bool inverseAutoMine = false;
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
    
    public bool hasBeenNewTickAutoPickup = false;
    
    /*
     * Temporary place for dis
     * Workflow after updating this setting:
     * - rerun ParseBlockTypes()
     * - rerender all chunks
     * Limitations atm:
     * - current implementation stops unknown blocks from being rendered
     * - cant make air transparent
     */
    public static Dictionary<string, float> xray = new()
    {
        //{ "dirt", .2f },
        //{ "rock", .2f },
        {"air", 0f}
    };

    private Dictionary<string, float> defaultXray = new();
    
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
        defaultXray = xray.ToDictionary(entry => entry.Key, entry => entry.Value);
        uiController = GetComponent<UIController>();
        root = uiController.doc.rootVisualElement;


        root.Q<Toggle>("kill-aura").RegisterValueChangedCallback(KillAuraEvent);
        root.Q<Toggle>("kill-aura").value = killAura;
        
        root.Q<Toggle>("self-kill").RegisterValueChangedCallback(SelfKillEvent);
        root.Q<Toggle>("freecam").RegisterValueChangedCallback(FreecamEvent);
        root.Q<Button>("reset-camera-position").RegisterCallback<ClickEvent>(ResetCameraPositionEvent);

        root.Q<Toggle>("remove-old-chunks-on-move").RegisterValueChangedCallback(RemoveOldChunksOnMoveEvent);
        root.Q<Toggle>("remove-old-chunks-on-move").value = chunkManager.removeOldChunksOnMove;
        
        root.Q<Toggle>("auto-pickup").RegisterValueChangedCallback(AutoPickUpEvent);
        root.Q<Toggle>("auto-pickup").value = autoPickup;

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
        InitQuickSend("send-up", 0, 1, 0, 0);
        InitQuickSend("send-down", 0, -1, 0, 0);
        InitQuickSend("send-4-up", 0, 1, 0, 1);
        InitQuickSend("send-4-down", 0, 1, 0, -1);
        InitQuickSend("send-forward", 0, 0, 1, 0);
        InitQuickSend("send-backward", 0, 0, -1, 0);
        InitQuickSend("send-left", -1, 0, 0, 0);
        InitQuickSend("send-right", 1, 0, 0, 0);
        InitQuickSend("send-up-left", -1, 1, 0, 0);
        InitQuickSend("send-up-right", 1, 1, 0, 0);

        
        // Init render distance values
        viewDistance = chunkManager.ViewDistance;
        heightDistance = chunkManager.HeightDistance;
        root.Q<IntegerField>("view-distance").value = viewDistance;
        root.Q<IntegerField>("height-distance").value = heightDistance;
        
        // Init inventory size values
        root.Q<SliderInt>("inventory-slider").highValue = inventorySizeI;
        root.Q<IntegerField>("inventory-size").value = inventorySize;
        root.Q<IntegerField>("inventory-size-i").value = inventorySizeI;
        
        root.Q<Button>("dungeon-mode").RegisterCallback<ClickEvent>(DungeonModeEvent);
        root.Q<Button>("reset-transparency").RegisterCallback<ClickEvent>(ResetTransparencyEvent);
        
        root.Q<Button>("log-inventory").RegisterCallback<ClickEvent>(LogInventoryEvent);
        root.Q<Button>("log-entities").RegisterCallback<ClickEvent>(LogEntitiesEvent);
        
        root.Q<Toggle>("auto-mine").RegisterValueChangedCallback(AutoMineEvent);
        root.Q<Toggle>("inverse-auto-mine").RegisterValueChangedCallback(InverseAutoMineEvent);


        InitXray();

        InvokeRepeating(nameof(Execute), 2.0f, 0.08f);
        
        InvokeRepeating(nameof(AutoMineExecute), 2.0f, 0.7f);
    }

    public void Execute()
    {
        KillAuraExecute();
        SelfKillExecute();
        AutoPickupExecute();
        
        SendPacketExecute();
    }

    private void InitQuickSend(string name, int x, int y, int z, int xi)
    {
        root.Q<Button>(name).RegisterCallback<ClickEvent>(evt =>
        {
            Vector3Int newPos = inputManager.TransformXZWithCamera(x, z);
            int newx = newPos.x;
            int newz = newPos.z;
            
            if (packetType == "Interact" || packetType == "InteractAndMove")
            {
                var itemType = chunkManager.GetBlockAtPosition(new Vector3Int(newx, y, newz));
                
                conn.Interact(playerController.GetPickUpSlot(itemType), newx.ToString(), newz.ToString(), y.ToString(), xi.ToString());
                
                if (packetType == "Interact") chunkManager.MoveAndUpdate("0", "0", "0", "0");
            }
            if (packetType == "Move" || packetType == "InteractAndMove")
            {
                chunkManager.MoveAndUpdate(newx.ToString(), y.ToString(), newz.ToString(), xi.ToString());
            }
            if (packetType == "Info")
            {
                var block = chunkManager.GetBlockAtPosition(new Vector3Int(newx, y, newz));

                if ((string)block["type"] == "rock")
                {
                    LogInfo("Block: " + block["type"] + " (" + block["strength"] + ")");
                }
                else
                {
                    LogInfo("Block: " + block["type"]);
                }
            
            }
        });
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

    private void LogInventoryEvent(ClickEvent evt)
    {
        var inventory = playerController.inventory;
        var inventoryString = "";
        foreach (var item in inventory)
        {
            inventoryString += item.Key + ": " + item.Value["type"] + " (" + item.Value["count"] + ")\n";
            foreach (var attr in item.Value)
            {
                if (attr.Key != "type" && attr.Key != "count")
                {
                    inventoryString += attr.Key + ": " + attr.Value + "\n";
                }
            }
            inventoryString += "\n";
        }
        LogInfo(inventoryString);
    }

    private void LogEntitiesEvent(ClickEvent evt)
    {
        if (chunkManager.chunks.Count == 0) return;
        
        var currentChunk = chunkManager.chunks[chunkManager.ViewDelta][chunkManager.HeightDelta][chunkManager.ViewDelta];
        
        if (currentChunk == null) return;
        
        var controller  = currentChunk.GetComponent<ChunkController>();

        string logText = "";

        foreach (var entity in controller.entities)
        {
            if ((string)entity["type"] != "monster") continue;
            
            logText += entity["type"] + " (" + entity["hp"] + ")\n";
            
            var inventory = ConvertObject<Dictionary<string,Dictionary<string, string>>>(entity["inventory"]);

            foreach (var item in inventory)
            {
                logText += item.Key + ": " + item.Value["type"] + " (" + item.Value["count"] + ")\n";
                foreach (var attr in item.Value)
                {
                    if (attr.Key != "type" && attr.Key != "count")
                    {
                        logText += attr.Key + ": " + attr.Value + "\n";
                    }
                }
                logText += "\n";
            }
            logText += "-----\n";
        }
        
        LogInfo(logText);
    }

    private void AutoMineEvent(ChangeEvent<bool> evt)
    {
        autoMine = evt.newValue;
    }
    
    private void InverseAutoMineEvent(ChangeEvent<bool> evt)
    {
        inverseAutoMine = evt.newValue;
    }

    public void AutoMineExecute()
    {
        if (!autoMine) return;
        
        if (chunkManager.chunks.Count == 0) return;

        int y = -1;
        if (inverseAutoMine) y = 1;
        
        var block = chunkManager.GetBlockAtPosition(new Vector3Int(1, y, 0));
        
        print("Next block type is: " + block["type"]);

        if ((string)block["type"] == "air")
        {
            print("Moving to next");
            chunkManager.MoveAndUpdate("1", y.ToString(), "0", "0");
            return;
        };
        
        if ((string)block["type"] != "rock") return;
        
        var strength = (string)block["strength"];

        var Istrength = ParseIStrength(strength);

        if (strength.Contains("i"))
        {
            var inventory = playerController.inventory;
            bool success = false;
            foreach (var item in inventory)
            {
                if ((string)item.Value["type"] == "pickaxe" && item.Value.TryGetValue("strength", out var value))
                {
                    var itemIStrength = ParseIStrength(value);
                    if (itemIStrength == "")
                    {
                        print("Found pickaxe with strength 1i, breaking " + Istrength + "i rock");
                        for (int i = 0; i < int.Parse(Istrength); i++)
                        {
                            conn.Interact(item.Key, "1", "0", y.ToString());
                        }

                        success = true;
                        break;
                    }
                }
            }

            if (!success)
            {
                Debug.LogWarning("Could not find pickaxe with strength 1i");
                return;
            }
        }

        var normalStrength = ParseStrength(strength);
        print("Breaking " + normalStrength + " rock");
        for (int i = 0; i < int.Parse(normalStrength); i++)
        {
            conn.Interact("-1", "1", "0", y.ToString());
        }
        chunkManager.MoveAndUpdate("1", y.ToString(), "0", "0");
    }

    private string ParseIStrength(string strength)
    {
        if (strength.Contains("+")) {
            strength = strength.Split('+')[1];
        }
        return strength.Remove(strength.Length - 1, 1);
    }
    private string ParseStrength(string strength)
    {
        if (strength.Contains("+")) {
           return strength.Split('+')[0];
        }

        return strength;
    }

    private void XrayUpdated()
    {
        chunkManager.blockTypes = chunkManager.ParseBlockTypes(blockTypesObject);
        chunkManager.ResetChunks();
    }

    private void DungeonModeEvent(ClickEvent evt)
    {
        xray["dirt"] = 0f;
        xray["rock"] = 0f;
        xray["concrete"] = 0f;
        xray["wood"] = 0f;

        xray["air"] = 0.3f;
        
        chunkManager.blockTypes = chunkManager.ParseBlockTypes(blockTypesObject);
        chunkManager.ResetChunks();
    }
    
    private void ResetTransparencyEvent(ClickEvent evt)
    {
        xray = defaultXray.ToDictionary(entry => entry.Key, entry => entry.Value);
        
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
        
        if (currentChunk == null) return;
        
        var controller  = currentChunk.GetComponent<ChunkController>();

        foreach (var entity in controller.entities)
        {
            if ((string)entity["type"] != "monster") continue;
            
            int x = int.Parse((string)entity["x"]);
            int y = int.Parse((string)entity["y"]);
                
            if (x > 1 || x < -1 || y > 1 || y < -1) continue;
                
            conn.Interact(playerController.GetCurrentSlot(), (string)entity["x"], (string)entity["y"]);
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

    private void AutoPickUpEvent(ChangeEvent<bool> evt)
    {
        autoPickup = evt.newValue;
    }

    private void AutoPickupExecute()
    {
        if (!hasBeenNewTickAutoPickup) return;
        hasBeenNewTickAutoPickup = false;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D))
        {
            return;
        }
        
        if (chunkManager.chunks.Count == 0) return;
        
        var currentChunk = chunkManager.chunks[chunkManager.ViewDelta][chunkManager.HeightDelta][chunkManager.ViewDelta];
        
        if (currentChunk == null) return;
        
        var controller  = currentChunk.GetComponent<ChunkController>();
        
        var positions = new List<Vector2Int>()
        {
            new Vector2Int(1,1),
            new Vector2Int(1,-1),
            new Vector2Int(-1,1),
            new Vector2Int(-1,-1),
            new Vector2Int(0,1),
            new Vector2Int(0,-1),
            new Vector2Int(1,0),
            new Vector2Int(-1,0),
        };

        var goodItems = new List<string>() {"sword", "pickaxe", "compass", "soul", "ventricle", "artery", "bone_marrow", "shield", "health_potion"};
        
        foreach (var pos in positions)
        {
            var mapItem = controller.map[7 + pos.y, 7 + pos.x];

            if (goodItems.Contains(mapItem["type"]))
            {
                conn.Interact(playerController.GetPickUpSlot(ConvertObject<Dictionary<string, object>>(mapItem)), pos.x.ToString(), pos.y.ToString(), "0", "0");
            }
        }
        
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
        } else if (evt.newValue == 2)
        {
            packetType = "InteractAndMove";
        } else if (evt.newValue == 3)
        {
            packetType = "Info";
        }
    }

    private void SendPacketExecute()
    {
        if (!sendRepeat) return;
        
        if (packetType == "Interact" || packetType == "InteractAndMove")
        {
            var itemType = chunkManager.GetBlockAtPosition(new Vector3Int(sendX, sendY, sendZ));
            
            conn.Interact(playerController.GetPickUpSlot(itemType), sendX.ToString(), sendZ.ToString(), sendY.ToString(), send4th.ToString());
            
            if (packetType == "Interact") chunkManager.MoveAndUpdate("0", "0", "0", "0");
        } 
        if (packetType == "Move" || packetType == "InteractAndMove")
        {
            chunkManager.MoveAndUpdate(sendX.ToString(), sendY.ToString(), sendZ.ToString(), send4th.ToString());
        }
        if (packetType == "Info")
        {
            var block = chunkManager.GetBlockAtPosition(new Vector3Int(sendX, sendY, sendZ));

            if ((string)block["type"] == "rock")
            {
                LogInfo("Block: " + block["type"] + " (" + block["strength"] + ")");
            }
            else
            {
                LogInfo("Block: " + block["type"]);
            }
            
        }
    }

    public void LogInfo(string info)
    {
        root.Q<Label>("info").text = info;
    }

    private void SendPacketEvent(ClickEvent evt)
    {
        if (packetType == "Interact" || packetType == "InteractAndMove")
        {
            var itemType = chunkManager.GetBlockAtPosition(new Vector3Int(sendX, sendY, sendZ));
            
            conn.Interact(playerController.GetPickUpSlot(itemType), sendX.ToString(), sendZ.ToString(), sendY.ToString(), send4th.ToString());
            
            if (packetType == "Interact") chunkManager.MoveAndUpdate("0", "0", "0", "0");
        }
        if (packetType == "Move" || packetType == "InteractAndMove")
        {
            chunkManager.MoveAndUpdate(sendX.ToString(), sendY.ToString(), sendZ.ToString(), send4th.ToString());
        }
        if (packetType == "Info")
        {
            var block = chunkManager.GetBlockAtPosition(new Vector3Int(sendX, sendY, sendZ));

            if ((string)block["type"] == "rock")
            {
                LogInfo("Block: " + block["type"] + " (" + block["strength"] + ")");
            }
            else
            {
                LogInfo("Block: " + block["type"]);
            }
            
        }
    }
    
    private static TValue ConvertObject<TValue>(object obj)
    {       
        var json = JsonConvert.SerializeObject(obj);
        var res = JsonConvert.DeserializeObject<TValue>(json);   
        return res;
    }
}