using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

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
    
    private VisualElement root;
    private UIController uiController;
    public Connection conn;
    public FreeCam freecam;
    public InputManager inputManager;

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

        
        root.Q<Vector2IntField>("render-distance").RegisterValueChangedCallback(RenderDistanceEvent);
        root.Q<Button>("apply-render-distance").RegisterCallback<ClickEvent>(RenderDistanceButtonEvent);
        
        // Init render distance values
        viewDistance = chunkManager.ViewDistance;
        heightDistance = chunkManager.HeightDistance;
        root.Q<Vector2IntField>("render-distance").value = new Vector2Int(viewDistance, heightDistance);
        
        // Init inventory size values
        root.Q<SliderInt>("inventory-slider").highValue = inventorySizeI;
        root.Q<IntegerField>("inventory-size").value = inventorySize;
        root.Q<IntegerField>("inventory-size-i").value = inventorySizeI;

        InvokeRepeating(nameof(Execute), 2.0f, 0.08f);
    }

    public void Execute()
    {
        KillAuraExecute();
        SelfKillExecute();
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
    
    private void RenderDistanceEvent(ChangeEvent<Vector2Int> evt)
    {
        viewDistance = evt.newValue.x;
        heightDistance = evt.newValue.y;
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
}