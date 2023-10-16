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
    
    private VisualElement root;
    private UIController uiController;
    private void Start()
    {
        uiController = GetComponent<UIController>();
        root = uiController.doc.rootVisualElement;
        
        root.Q<Toggle>("kill-aura").RegisterValueChangedCallback(killAuraEvent);
        root.Q<Toggle>("self-kill").RegisterValueChangedCallback(selfKillEvent);
        root.Q<Vector2IntField>("render-distance").RegisterValueChangedCallback(renderDistanceEvent);
        root.Q<Button>("apply-render-distance").RegisterCallback<ClickEvent>(renderDistanceButtonEvent);
        
        // Init render distance values
        viewDistance = chunkManager.ViewDistance;
        heightDistance = chunkManager.HeightDistance;
        root.Q<Vector2IntField>("render-distance").value = new Vector2Int(viewDistance, heightDistance);
    }

    private void killAuraEvent(ChangeEvent<bool> evt)
    {
        killAura = evt.newValue;
    }

    private void killAuraExecute()
    {
        if (!killAura) return;
    }
    
    private void selfKillEvent(ChangeEvent<bool> evt)
    {
        killAura = evt.newValue;
    }
    
    private void selfKIllExecute()
    {
        if (!killAura) return;
    }
    
    private void renderDistanceEvent(ChangeEvent<Vector2Int> evt)
    {
        viewDistance = evt.newValue.x;
        heightDistance = evt.newValue.y;
    }

    private void renderDistanceButtonEvent(ClickEvent evt)
    {
        chunkManager.ViewDistance = viewDistance;
        chunkManager.HeightDistance = heightDistance;
        chunkManager.UpdateDistanceDelta();
        chunkManager.ResetChunks();
    }
}