﻿using System;
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
    
    private VisualElement root;
    private UIController uiController;
    public Connection conn;
    private void Start()
    {
        uiController = GetComponent<UIController>();
        root = uiController.doc.rootVisualElement;
        
        root.Q<Toggle>("kill-aura").RegisterValueChangedCallback(KillAuraEvent);
        root.Q<Toggle>("self-kill").RegisterValueChangedCallback(SelfKillEvent);
        root.Q<Vector2IntField>("render-distance").RegisterValueChangedCallback(RenderDistanceEvent);
        root.Q<Button>("apply-render-distance").RegisterCallback<ClickEvent>(RenderDistanceButtonEvent);
        
        // Init render distance values
        viewDistance = chunkManager.ViewDistance;
        heightDistance = chunkManager.HeightDistance;
        root.Q<Vector2IntField>("render-distance").value = new Vector2Int(viewDistance, heightDistance);
        
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
}