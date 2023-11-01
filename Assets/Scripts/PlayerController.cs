using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
public class PlayerController : MonoBehaviour
{
    public Dictionary<string,Dictionary<string, string>> inventory;
    public string hp;
    public string maxHp;
    public string xp;
    public string level;
    public string playerName;
    public Vector2Int currentSlot = new Vector2Int(0,0);
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

    public string ConvertSlot(int x, int y)
    {
        string item = "";

        if (x != 0)
        {
            item += x.ToString();
        }
        if (y != 0) {
            if (item != "" && y > 0) {
                item += "+";
            }
            if (y == -1) {
                item += "-i";
            } else if (y == 1) {
                item += "i";
            } else {
                item += y.ToString() + "i";
            }
        }

        if (x == 0 && y == 0) {
            item = "0";
        }

        return item;
    }

    public string GetCurrentSlot()
    {
        return ConvertSlot(currentSlot.x, currentSlot.y);
    }
    
    private static TValue ConvertObject<TValue>(object obj)
    {       
        var json = JsonConvert.SerializeObject(obj);
        var res = JsonConvert.DeserializeObject<TValue>(json);   
        return res;
    }
}
