using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkController : MonoBehaviour
{
    // Start is called before the first frame update
    public Dictionary<string, string>[,] map;
    public Dictionary<string, object>[] entities;

    public void GenerateBlocks()
    {
        print("Starting generation");
        for (int y = 7; y >= -7; y--)
        {
            for (int x = -7; x <= 7; x++)
            {
                var block = map[y + 7, x + 7];

                if (block["type"] == "air") continue;
                
                if (block["type"] == "dirt")
                {
                    CreateBlock(PrimitiveType.Cube, new Color32(87, 73, 36,255), x, y);
                } 
                else if (block["type"] == "concrete")
                {
                    CreateBlock(PrimitiveType.Cube, new Color32(150, 150, 150,255), x, y);
                }
                else if (block["type"] == "tombstone")
                {
                    CreateBlock(PrimitiveType.Cylinder, new Color32(79, 79, 79,255), x, y);
                }
                else if (block["type"] == "wood")
                {
                    CreateBlock(PrimitiveType.Cube, new Color32(214, 154, 75,255), x, y);
                }
                else if (block["type"] == "leaves")
                {
                    CreateBlock(PrimitiveType.Cube, new Color32(94, 204, 78,255), x, y);
                }
                else
                {
                    CreateBlock(PrimitiveType.Cube, new Color32(173, 49, 49,255), x, y);
                }
            }
        }
    }

    public void GenerateEntities()
    {
        print("Generating entities");
        foreach (Dictionary<string, object> entity in entities)
        {
            int x = int.Parse((string)entity["x"]);
            int y = int.Parse((string)entity["y"]);
            
            if ((string)entity["type"] == "player")
            {
                CreateBlock(PrimitiveType.Capsule, new Color32(240, 200, 0,255), x, y);
            }
            if ((string)entity["type"] == "monster")
            {
                CreateBlock(PrimitiveType.Capsule, new Color32(240, 0, 0,255), x, y);
            }
            if ((string)entity["type"] == "monster")
            {
                CreateBlock(PrimitiveType.Capsule, new Color32(255, 255, 255,255), x, y);
            }
            else
            {
                CreateBlock(PrimitiveType.Capsule, new Color32(173, 49, 49,255), x, y);
            }
            
        }
    }

    private void CreateBlock(PrimitiveType type, Color color, int x, int y)
    {
        var block = GameObject.CreatePrimitive(type);
        block.transform.position = new Vector3(x, 0,y);
        block.name = "Block";
        block.transform.parent = transform;
        block.GetComponent<Renderer>().material.color = color;
    }
}
