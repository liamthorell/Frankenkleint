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
using Microsoft.Z3;
using Unity.Collections;
using Unity.Jobs;

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
    public bool invertDimensions = false;
    
    public int sendX = 0;
    public int sendY = 0;
    public int sendZ = 0;
    public int send4th = 0;

    public int mineX = 1;
    public int mineY = -1;
    public int mineZ = 0;
    public int mine4th = 0;
    
    public bool sendRepeat = false;
    public string packetType = "Move";
    
    public bool hasBeenNewTickAutoPickup = false;
    
    /*
     * Temporary place for dis
     * Workflow after updating this setting:
     * - rerun ParseBlockTypes()
     * - rerender all chunks
     * Limitations atm:
     * - current implementation stops unknown blocks from being rendered [x]
     * - cant make air transparent [x]
     */
    public static Dictionary<string, float> xray = new()
    {
        {"air", 0f}
    };

    private Dictionary<string, float> defaultXray = new();
    
    private VisualElement root;
    private UIController uiController;
    public Connection conn;
    public FreeCam freecam;
    public InputManager inputManager;
    public PlayerController playerController;
    public MazeSolver mazeSolver;
    
    public BlockTypes blockTypesObject; 
    
    private Debounce xrayDebounce = new Debounce();

    private VisualElement detailPanel;

    public Dictionary<string, List<string>> calculatedKills = new();
    public List<JobHandle> calcJobHandles = new();
    public List<NativeArray<int>> calcJobInputData = new();
    public List<NativeArray<int>> calcJobOutputData = new();
    public List<string> calcIds = new();

    private void Start()
    {
        defaultXray = xray.ToDictionary(entry => entry.Key, entry => entry.Value);
        uiController = GetComponent<UIController>();
        root = uiController.doc.rootVisualElement;


        root.Q<Toggle>("kill-aura").RegisterValueChangedCallback(KillAuraEvent);
        root.Q<Toggle>("kill-aura").value = killAura;
        
        root.Q<Toggle>("self-kill").RegisterValueChangedCallback(SelfKillEvent);
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

        root.Q<IntegerField>("mine-x").RegisterValueChangedCallback(MineXEvent);
        root.Q<IntegerField>("mine-y").RegisterValueChangedCallback(MineYEvent);
        root.Q<IntegerField>("mine-z").RegisterValueChangedCallback(MineZEvent);

        root.Q<Toggle>("send-repeat").RegisterValueChangedCallback(SendRepeatEvent);
        root.Q<RadioButtonGroup>("packet-type").RegisterValueChangedCallback(PacketTypeEvent);
        root.Q<Button>("send-packet").RegisterCallback<ClickEvent>(SendPacketEvent);
        
        root.Q<Toggle>("invert-dimensions").RegisterValueChangedCallback(InvertDimensionsEvent);
        
        // Quick send packet
        InitQuickSend("send-up", 0, 1, 0, 0);
        InitQuickSend("send-down", 0, -1, 0, 0);
        InitQuickSend("send-4-up", 0, 0, 0, 1);
        InitQuickSend("send-4-down", 0, 0, 0, -1);
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

        root.Q<Button>("save-maze").RegisterCallback<ClickEvent>(SaveMazeEvent);
        root.Q<Button>("solve-maze").RegisterCallback<ClickEvent>(SolveMazeEvent);
        root.Q<Button>("solve-maze-reverse").RegisterCallback<ClickEvent>(SolveMazeReverseEvent);
        root.Q<Button>("abort-solve-maze").RegisterCallback<ClickEvent>(AbortMazeSolveEvent);
        
        root.Q<Toggle>("auto-mine").RegisterValueChangedCallback(AutoMineEvent);
        root.Q<Toggle>("inverse-auto-mine").RegisterValueChangedCallback(InverseAutoMineEvent);
        
        InvokeRepeating(nameof(CheckToSeeIfMobsNeedsCalculate), 0f, 0.5f);
        
        InitXray();
        
        InvokeRepeating(nameof(Execute), 1.0f, 0.08f);
        //InvokeRepeating(nameof(KillAuraExecute), 2.0f, 0.05f);
        InvokeRepeating(nameof(KillAuraExecute), 2.0f, 0.5f);
        InvokeRepeating(nameof(AutoMineExecute), 2.0f, 0.5f);

        detailPanel = root.Q<VisualElement>("detail-panel");
        detailPanel.visible = false;
    }

    public void Execute()
    {
        SelfKillExecute();
        AutoPickupExecute();
        SendPacketExecute();
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            detailPanel.visible = false;
            root.Q<Label>("detail-inventory-title").visible = false;
            root.Q<Label>("detail-inventory").visible = false;
        }

        for (int i = 0; i < calcJobHandles.Count; i++)
        {
            var job = calcJobHandles[i];
            if (job.IsCompleted)
            {
                var outputData = calcJobOutputData[i];
                var inputData = calcJobInputData[i];
                var id = calcIds[i];
                calcJobInputData.RemoveAt(i);
                calcJobOutputData.RemoveAt(i);
                calcJobHandles.RemoveAt(i);
                calcIds.RemoveAt(i);

                var swordsToUse = new List<string>();
                
                job.Complete();

                for (int j = 0; j < outputData.Length / 2; j++)
                {
                    var sw1 = outputData[j * 2];
                    var sw2 = outputData[j * 2 + 1];
                    
                    if (sw1 == 0 && sw2 == 0) continue;
                    swordsToUse.Add(playerController.ConvertSlot(sw1, sw2));
                }
                calculatedKills[id] = swordsToUse;
                
                outputData.Dispose();
                inputData.Dispose();
            }
        }
    }

    public void CheckToSeeIfMobsNeedsCalculate()
    {
        var chunk = chunkManager.chunks[chunkManager.ViewDelta][chunkManager.HeightDelta][chunkManager.ViewDelta];
        if (chunk == null) return;
        
        var controller = chunk.GetComponent<ChunkController>();
        
        var swords = playerController.inventory.Where(item => item.Value["type"] == "sword").ToList();
        
        foreach (var entity in controller.entities)
        {
            if ((string)entity["type"] == "monster")
            {
                var id = GetIdOfMob(entity);
                
                if (!calculatedKills.ContainsKey(id))
                {
                    var inputData = new NativeArray<int>(swords.Count * 2 + 6, Allocator.Persistent);
                    
                    inputData[0] = ParseStrength((string)entity["hp"]);
                    inputData[1] = ParseIStrength((string)entity["hp"]);



                    int shield = 0;
                    int sheld = 0;
                    
                    var inventory = ConvertObject<Dictionary<string,Dictionary<string, string>>>(entity["inventory"]);
                    foreach (var item in inventory)
                    {
                        if (item.Value["type"] == "shield")
                        {
                            shield += int.Parse(item.Value["count"]);
                        }
                        if (item.Value["type"] == "sheld")
                        {
                            sheld += int.Parse(item.Value["count"]);
                        }
                    }
                    
                    inputData[2] = shield;
                    inputData[3] = sheld;
                    
                    inputData[4] = 1;
                    inputData[5] = 0;
                    
                    for (int i = 0; i < swords.Count; i++)
                    {
                        inputData[i * 2 + 4] = ParseStrength(swords[i].Value["strength"]);
                        inputData[i * 2 + 4 + 1] = ParseIStrength(swords[i].Value["strength"]);
                    }
                    
                    var outputData = new NativeArray<int>(100, Allocator.Persistent);
                    
                    calculatedKills.Add(id, new List<string>());
                    var job = new Calculate() {
                        inputData = inputData,
                        outputData = outputData,
                    };
                    
                    calcJobHandles.Add(job.Schedule());
                    calcJobInputData.Add(inputData);
                    calcJobOutputData.Add(outputData);
                    calcIds.Add(id);
                }
            }
        }
    }

    private string GetIdOfMob(Dictionary<string, object> entity)
    {
        string id = "";
        id += entity["hp"];
        id += entity["max_hp"];
        var inventory = ConvertObject<Dictionary<string,Dictionary<string, string>>>(entity["inventory"]);
        foreach (var item in inventory.Take(10))
        {
            id += item.Value["type"];
            id += item.Value["count"];
        }

        return id;
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

    private void InvertDimensionsEvent(ChangeEvent<bool> evt)
    {
        invertDimensions = evt.newValue;
    }

    private void InitXray()
    {
        var xrayContainer = root.Q<VisualElement>("xray-foldout");
        foreach (var entry in blockTypesObject.blocks)
        {
            if (entry.modelOverride)
                continue;
            
            var slider = new Slider();
            slider.name = "xray-" + entry.name;
            slider.label = entry.name;
            slider.lowValue = 0;
            slider.highValue = 1;
            slider.value = 1;
            slider.style.marginRight = 10;
            slider.userData = true;
            if (xray.TryGetValue(entry.name, out var value))
            {
                slider.value = value;
            }

            slider.RegisterValueChangedCallback(evt => {
                var sli = root.Q<Slider>("xray-" + entry.name);
                if ((bool)sli.userData)
                {
                    if (Math.Abs(evt.newValue - 1f) < 0.05f)
                    {
                        xray.Remove(entry.name);
                    }
                    else
                    {
                        xray[entry.name] = evt.newValue;
                    }
                    xrayDebounce.Run(XrayUpdated, 0.5f, this);
                }
                else
                {
                    sli.userData = true;
                }
            });

            xrayContainer.Add(slider);
        }
    }

    public void SaveMazeEvent(ClickEvent evt)
    {
        mazeSolver.SaveMaze();
    }
    public void SolveMazeEvent(ClickEvent evt)
    {
        mazeSolver.SolveMaze();
    }
    public void SolveMazeReverseEvent(ClickEvent evt)
    {
        mazeSolver.SolveMazeReverse();
    }
    
    public void AbortMazeSolveEvent(ClickEvent evt)
    {
        mazeSolver.AbortMazeSolve();
    }
    struct Calculate : IJob
    {
        public NativeArray<int> inputData;
        public NativeArray<int> outputData;
        public void Execute()
        {
            //Tuple<int, int> enemyHp = new Tuple<int, int>(11, 5);

            Tuple<int, int> enemyHp = new Tuple<int, int>(inputData[0], inputData[1]);


            //Tuple<int, int>[] swords = { new Tuple<int, int>(7, -4), new Tuple<int, int>(4, -3), new Tuple<int, int>(-5, -5), new Tuple<int, int>(7, -9) };
            List<Tuple<int, int>> swords = new List<Tuple<int, int>>();

            for (int i = 0; i < ((inputData.Length - 4) / 2); i++)
            {
                swords.Add(new Tuple<int, int>(inputData[i*2 + 4], inputData[i*2 + 4 + 1]));
            }
            
            using (Context ctx = new Context())
            {
                var shield = ctx.MkInt(inputData[2]);
                var sheld = ctx.MkInt(inputData[3]);
                for (int i = 1; i < 50; i++)
                {
                    Tuple<ArithExpr, ArithExpr>[] convertedSwords = swords.Select(x => new Tuple<ArithExpr, ArithExpr>(ctx.MkInt(x.Item1), ctx.MkInt(x.Item2))).ToArray();

                    Tuple<ArithExpr, ArithExpr>[] solutions = Enumerable.Range(0, i).Select(j => new Tuple<ArithExpr, ArithExpr>(ctx.MkIntConst(j.ToString()), ctx.MkIntConst("i" + j.ToString()))).ToArray();

                    Solver solver = ctx.MkSolver();
                    
                    foreach (var sol in solutions)
                    {
                        solver.Assert(ctx.MkOr(convertedSwords.Select(x => ctx.MkAnd(ctx.MkEq(sol.Item1, x.Item1), ctx.MkEq(sol.Item2, x.Item2)))));
                    }

                    ArithExpr total = ctx.MkInt(0);
                    ArithExpr totalI = ctx.MkInt(0);
                    foreach (var sol in solutions)
                    {
                        total = ctx.MkAdd(total, sol.Item1 - shield);
                        totalI = ctx.MkAdd(totalI, sol.Item2 - sheld);
                    }

                    solver.Assert(ctx.MkEq(total, ctx.MkInt(enemyHp.Item1)));
                    solver.Assert(ctx.MkEq(totalI, ctx.MkInt(enemyHp.Item2)));

                    if (solver.Check() == Status.SATISFIABLE)
                    {
                        Model model = solver.Model;

                        int[][] swordsToUse = new int[i][];
                        for (int j = 0; j < i; j++)
                        {
                            swordsToUse[j] = new int[2];
                        }
                        
                        foreach (FuncDecl constDecl in model.ConstDecls)
                        {
                            Expr value = model.Evaluate(constDecl.Apply());
                            if (constDecl.Name.ToString().Contains("i"))
                            {
                                swordsToUse[int.Parse(constDecl.Name.ToString().Substring(1))][1] = int.Parse(value.ToString());
                            }
                            else
                            {
                                swordsToUse[int.Parse(constDecl.Name.ToString())][0] = int.Parse(value.ToString());
                            }
                        }

                        for (int j = 0; j < (swordsToUse.Length); j++)
                        {
                            var sw = swordsToUse[j];
                            outputData[j * 2] = sw[0];
                            outputData[j * 2 + 1] = sw[1];
                        }
                        /*foreach (var sw in swordsToUse)   
                        {
                            print("Swords: " + sw[0] + ", " + sw[1]);
                        }*/
                        
                        break;
                    }
                    //Debug.Log("Model not good");
                }
            }
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
    
    private void MineXEvent(ChangeEvent<int> evt)
    {
        mineX = evt.newValue;
    }
    private void MineYEvent(ChangeEvent<int> evt)
    {
        mineY = evt.newValue;
    }
    private void MineZEvent(ChangeEvent<int> evt)
    {
        mineZ = evt.newValue;
    }

    public void AutoMineExecute()
    {
        if (!autoMine) return;
        
        if (chunkManager.chunks.Count == 0) return;

        var newY = inverseAutoMine ? -mineY : mineY;
        
        var block = chunkManager.GetBlockAtPosition(new Vector3Int(mineX, newY, mineZ));
        
        print("Next block type is: " + block["type"]);

        if ((string)block["type"] == "air")
        {
            print("Moving to next");
            chunkManager.MoveAndUpdate(mineX.ToString(), newY.ToString(), mineZ.ToString(), "0");
            return;
        };
        
        if ((string)block["type"] != "rock") return;
        
        var strength = (string)block["strength"];

        if (strength.Contains("i"))
        {
            var Istrength = ParseIStrength(strength);
            var inventory = playerController.inventory;
            bool success = false;

            if (Istrength > 0)
            {
                foreach (var item in inventory)
                {
                    if (item.Value["type"] == "pickaxe" && item.Value.TryGetValue("strength", out var value))
                    {
                        var itemIStrength = ParseIStrength(value);
                        if (itemIStrength == 1)
                        {
                            print("Found pickaxe with strength 1i, breaking " + Istrength + "i rock");
                            for (int i = 0; i < Istrength; i++)
                            {
                                conn.Interact(item.Key, mineX.ToString(), mineZ.ToString(), newY.ToString());
                            }

                            success = true;
                            break;
                        }
                        
                    }
                }
                
                if (!success)
                {
                    foreach (var item1 in inventory)
                    {
                        foreach (var item2 in inventory)
                        {
                            if (item1.Value["type"] == "pickaxe" && item1.Value.TryGetValue("strength", out var value1) && item2.Value["type"] == "pickaxe" && item2.Value.TryGetValue("strength", out var value2))
                            {
                                var itemIStrength1 = ParseIStrength(value1);
                                var itemIStrength2 = ParseIStrength(value2);

                                if (itemIStrength1 + itemIStrength2 == 1)
                                {
                                    print("Found two pickaxes with combined strength 1i, breaking " + Istrength + "i rock");
                                    for (int i = 0; i < Istrength; i++)
                                    {
                                        conn.Interact(item1.Key, mineX.ToString(), mineZ.ToString(), newY.ToString());
                                        conn.Interact(item2.Key, mineX.ToString(), mineZ.ToString(), newY.ToString());
                                    }

                                    success = true;
                                    break;
                                }

                            }
                        }

                        if (success) break;
                    }
                }

                if (!success)
                {
                    Debug.LogWarning("Could not find pickaxe with strength 1i");
                    return;
                }
            }
        }

        var normalStrength = ParseStrength(strength);
        print("Breaking " + normalStrength + " rock");
        if (normalStrength > 0)
        {
            for (int i = 0; i < normalStrength; i++)
            {
                for (int j = 0; j < 1000; j++)
                {
                    var slot = playerController.ConvertSlot(j, -1);
                    if (!playerController.inventory.ContainsKey(slot))
                    {
                        conn.Interact(slot, mineX.ToString(), mineZ.ToString(), newY.ToString());
                        break;
                    }
                }
            }
        }
        chunkManager.MoveAndUpdate(mineX.ToString(), newY.ToString(), mineZ.ToString(), "0");
    }

    private int ParseIStrength(string strength)
    {
        if (strength.Contains("+")) {
            strength = strength.Split('+')[1];
        }
        if (strength.Contains("-"))
        {
            int minusCount = strength.Count(x => x == '-');
            strength = strength.Split('-')[minusCount];
            
            if (minusCount == 2 && strength == "i") strength = "-1i";
        }

        if (strength == "i") strength = "1i";

        if (int.TryParse(strength.Remove(strength.Length - 1, 1), out var result))
        {
            return result;
        }
        return 0;
    }
    private int ParseStrength(string strength)
    {
        if (strength.Contains("+")) {
           return int.Parse(strength.Split('+')[0]);
        }
        if (strength.Contains("i") || strength == "")
        {
            return 0;
        }
        
        if (int.TryParse(strength, out var result))
        {
            return result;
        }
        return 0;
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
        xray["veilstone"] = 0f;
        xray["hypercube"] = 0f;
        xray["air"] = 0.3f;
        
        UpdateXraySliders();
        
        chunkManager.blockTypes = chunkManager.ParseBlockTypes(blockTypesObject);
        chunkManager.ResetChunks();
    }

    private void UpdateXraySliders()
    {
        var xrayContainer = root.Q<VisualElement>("xray-foldout");
        foreach (var entry in blockTypesObject.blocks)
        {
            if (entry.modelOverride)
                continue;
            
            var slider = xrayContainer.Q<Slider>("xray-" + entry.name);
            slider.userData = false;

            if (xray.TryGetValue(entry.name, out var value))
            {
                slider.value = value;
            }
            else
            {
                slider.value = 1f;
            }
        }
    }
    
    private void ResetTransparencyEvent(ClickEvent evt)
    {
        xray = defaultXray.ToDictionary(entry => entry.Key, entry => entry.Value);

        UpdateXraySliders();
        
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

            string id = GetIdOfMob(entity);

            if (calculatedKills.TryGetValue(id, out var killItem))
            {
                calculatedKills.Remove(id);
                if (killItem.Count > 0)
                {
                    foreach (var lol in killItem)
                    {
                        if (lol == "1")
                        {
                            print("Killing mob with hand");
                            conn.Interact("-10", (string)entity["x"], (string)entity["y"]);
                        }
                        foreach (var invItem in playerController.inventory)
                        {
                            if (invItem.Value["type"] == "sword" && invItem.Value["strength"] == lol)
                            {   
                                print("Killing mob with " + lol + " sword");
                                conn.Interact(invItem.Key, (string)entity["x"], (string)entity["y"]);
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                print("Could not find weapon");
                conn.Interact(playerController.GetCurrentSlot(), (string)entity["x"], (string)entity["y"]);
            }
            //conn.Interact(playerController.GetCurrentSlot(), (string)entity["x"], (string)entity["y"]);
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
        if (!autoPickup) return;
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
            new (1,1),
            new (1,-1),
            new (-1,1),
            new (-1,-1),
            new (0,1),
            new (0,-1),
            new (1,0),
            new (-1,0),
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

    public void ShowDetailPane(Dictionary<string, object> data)
    {
        detailPanel.visible = true;
        
        // title
        string type = (string)data["type"];
        string title = char.ToUpper(type[0]) + type[1..];
        if(data.TryGetValue("name", out var name))
        {
            title += " - " + (string)name;
        }
        root.Q<Label>("detail-type").text = title;

        // entity stats
        string entityStats = "";

        if (data.TryGetValue("x", out var x) && data.TryGetValue("y", out var y))
        {
            entityStats += $"Position: ({(string)x}, {(string)y})\n";
        }

        if (data.TryGetValue("hp", out var hp) && data.TryGetValue("max_hp", out var maxHp))
        {
            entityStats += $"HP: {(string)hp} / {(string)maxHp}\n";
        }

        if (data.TryGetValue("level", out var level))
        {
            entityStats += $"Level: {(string)level}\n";
        }

        if (data.TryGetValue("xp", out var xp))
        {
            entityStats += $"XP: {(string)xp}\n";
        }

        foreach (var attr in data)
        {
            if (!new[] { "type", "name", "x", "y", "hp", "max_hp", "level", "xp", "inventory" }.Contains(attr.Key))
            {
                entityStats += char.ToUpper(attr.Key[0]) + attr.Key[1..] + ": " + attr.Value + "\n";
            }
        }

        root.Q<Label>("detail-entity-stats").text = entityStats;

        bool foundInventory = false;
        var inventoryContent = root.Q<Label>("detail-inventory");
        inventoryContent.text = "";
        if (data.TryGetValue("inventory", out var inventory))
        {
            var inventoryDict = ConvertObject<Dictionary<string,Dictionary<string, string>>>(inventory);
            foreach (var item in inventoryDict)
            {
                inventoryContent.text += item.Key + ": " + char.ToUpper(item.Value["type"][0]) + item.Value["type"][1..] + " (" + item.Value["count"] + ")\n";
                foreach (var attr in item.Value)
                {
                    if (attr.Key != "type" && attr.Key != "count")
                    {
                        inventoryContent.text += "- " + char.ToUpper(attr.Key[0]) + attr.Key[1..] + ": " + attr.Value + "\n";
                    }
                }
                inventoryContent.text += "\n";
            }
            foundInventory = true;
        }
        root.Q<Label>("detail-inventory-title").visible = foundInventory;
        inventoryContent.visible = foundInventory;
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