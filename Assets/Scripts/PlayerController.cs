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

    public Mods mods;

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

    public string GetPickUpSlot(string itemType)
    {
        if (itemType == "monster")
        {
            // use sword slot
        } else if (itemType == "rock")
        {
            // use pickaxe slot
        } else if (itemType == "soul")
        {
            // use a empty slot on a specific row
        }
        else if (inventory.ContainsKey(GetCurrentSlot()) && inventory[GetCurrentSlot()]["type"] == itemType)
        {
            // use the current slot because its the same type
            return GetCurrentSlot();
        }
        else if (!inventory.ContainsKey(GetCurrentSlot()))
        {
            // use the current slot becuase its empty
            return GetCurrentSlot();
        }
        else
        {
            // find the first slot thats empty
            return FirstEmptySlot();
        }
        return GetCurrentSlot();
    }

    public string FirstEmptySlot()
    {
        for (int i = 0; i < mods.inventorySizeI; i++)
        {
            for (int j = 0; j < mods.inventorySize; j++)
            {
                if (!inventory.ContainsKey(ConvertSlot(j, i)))
                {
                    return ConvertSlot(j, i);
                }
            }
        }

        return GetCurrentSlot();
    }
    
    private static TValue ConvertObject<TValue>(object obj)
    {       
        var json = JsonConvert.SerializeObject(obj);
        var res = JsonConvert.DeserializeObject<TValue>(json);   
        return res;
    }
}
