using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkController : MonoBehaviour
{
    public Dictionary<string, string>[,] map;
    public Dictionary<string, object>[] entities;
    
    public Material blockMaterial;
    private Dictionary<string, Material> materials;
    
    public void Init()
    {
        materials = new Dictionary<string, Material>();
        
        string[] blockTypes = { "dirt", "concrete", "wood", "leaves", "tombstone", "none" };

        for (int i = 0; i < blockTypes.Length; i++)
        {        
            Material material = new Material(blockMaterial);
            material.SetFloat("_index", i);
            materials.Add(blockTypes[i], material);
        }
        print(materials.Keys);
    }
    
    public void GenerateBlocks()
    {
        print("Starting generation");
        for (int y = 7; y >= -7; y--)
        {
            for (int x = -7; x <= 7; x++)
            {
                var block = map[y + 7, x + 7];

                if (block["type"] == "air") continue;

                switch (block["type"])
                {
                    case "dirt":
                        CreateBlock(PrimitiveType.Cube, "dirt", x, y);
                        break;
                    case "concrete":
                        CreateBlock(PrimitiveType.Cube, "concrete", x, y);
                        break;
                    case "tombstone":
                        CreateBlock(PrimitiveType.Cylinder, "tombstone", x, y);
                        break;
                    case "wood":
                        CreateBlock(PrimitiveType.Cube, "wood", x, y);
                        break;
                    case "leaves":
                        CreateBlock(PrimitiveType.Cube, "leaves", x, y);
                        break;
                    default:
                        CreateBlock(PrimitiveType.Cube, "none", x, y);
                        break;
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

            switch ((string)entity["type"])
            {
                case "player":
                    CreateBlock(PrimitiveType.Capsule, "none", x, y);
                    break;
                case "monster":
                    CreateBlock(PrimitiveType.Capsule, "none", x, y);
                    break;
                case "ghost":
                    CreateBlock(PrimitiveType.Capsule, "none", x, y);
                    break;
                default:
                    CreateBlock(PrimitiveType.Capsule, "none", x, y);
                    break;
            }
        }
    }

    private void CreateBlock(PrimitiveType type, string textureName, int x, int y)
    {
        var block = GameObject.CreatePrimitive(type);
        block.transform.position = new Vector3(x, 0,y);
        block.name = "Block";
        block.transform.parent = transform;

        block.GetComponent<Renderer>().sharedMaterial = materials[textureName];
    }
}
