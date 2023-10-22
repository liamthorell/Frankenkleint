using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
public class PlayerController : MonoBehaviour
{
    // Start is called before the first frame update
    public Dictionary<string,Dictionary<string, string>> inventory;
    public string hp;
    public string maxHp;
    public string xp;
    public string level;
    public string playerName;
    public string currentSlot = "0";
    public UIController uiController;

    public void HandleTick(IDictionary data)
    {
        var entities = ConvertObject<Dictionary<string, object>[]>(data["entities"]);

        foreach (var entity in entities)
        {
            if ((string)entity["type"] == "player" && (string)entity["x"] == "0" && (string)entity["y"] == "0")
            {
                hp = entity["hp"] as string;
                maxHp = entity["max_hp"] as string;
                xp = entity["xp"] as string;
                level = entity["level"] as string;
                playerName = entity["name"] as string;
                inventory = ConvertObject<Dictionary<string,Dictionary<string, string>>>(entity["inventory"]);
            }
        }
        
        uiController.UpdateInventory();
        uiController.UpdateStats();
    }
    
    private static TValue ConvertObject<TValue>(object obj)
    {       
        var json = JsonConvert.SerializeObject(obj);
        var res = JsonConvert.DeserializeObject<TValue>(json);   
        return res;
    }
}
