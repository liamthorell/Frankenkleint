using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class ChunkController : MonoBehaviour
{
    public Dictionary<string, string>[,] map;
    public Dictionary<string, object>[] entities;
    public Vector3Int chunkPosition;

    public GameObject floatingText;
    
    public List<BlockTypes.BlockType> types; // also temporary as heck

    public void GenerateBlocks()
    {
        print("Starting generation");
        for (int y = 7; y >= -7; y--)
        {
            for (int x = -7; x <= 7; x++)
            {
                var block = map[y + 7, x + 7];

                if (block["type"] == "air") continue;
                
                var type = block["type"]; // todo error handling if doesnt exist
                CreateBlock(type, x, y);
                
                /*switch (block["type"])
                {
                    case "tombstone":
                        CreateBlock(PrimitiveType.Cylinder, "tombstone", x, y, 0.6f);
                        break;
                    default:
                        var type = block.ContainsKey(block["type"]) ? block["type"] : "none";
                        CreateBlock(PrimitiveType.Cube, type, x, y);
                        break;
                }*/
            }
        }
    }

    public void GenerateEntities()
    {
        print("Generating entities");
        foreach (var entity in entities)
        {
            int x = int.Parse((string)entity["x"]);
            int y = int.Parse((string)entity["y"]);

            switch ((string)entity["type"])
            {
                case "player":
                    if (chunkPosition.y != 0 && chunkPosition.x == 0 && chunkPosition.z == 0) break;
                    CreateBlock("none", x, y,1f, entity["name"] + " " + entity["hp"] + "/" + entity["max_hp"]);
                    break;
                case "monster":
                    CreateBlock("none", x, y,1f, "Monster" + " " + entity["hp"] + "/" + entity["max_hp"]);
                    break;
                case "ghost":
                    CreateBlock("none", x, y,1f, "Ghost");
                    break;
                default:
                    CreateBlock("none", x, y);
                    break;
            }
        }
    }

    private void CreateBlock(string textureName, int x, int y, float scale = 1, string text = null)
    { 
        BlockTypes.BlockType type = types.Find(item => item.name == textureName);
        if (type.material == null && type.modelOverride == null) // todo clean this ugly ass shit up
        {
            type = types.Find(item => item.name == "none");
        }

        GameObject block;
        if (type.modelOverride == null)
        {

            block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.GetComponent<Renderer>().sharedMaterial = type.material;
            block.name = "Block";
        }
        else
        {
            block = Instantiate(type.modelOverride);
        }
        
        block.transform.position =
            new Vector3(x + (15 * chunkPosition.x), chunkPosition.y, y + (15 * chunkPosition.z));
        block.name = "Block";
        block.transform.localScale = new Vector3(1f, scale, 1f);
        block.transform.parent = transform;
    
        // init floating text
        if (text != null)
        {
            var textObject = Instantiate(floatingText, new Vector3(block.transform.position.x, block.transform.position.y + 0.75f, block.transform.position.z), Quaternion.identity, block.transform);
            var textMeshPro = textObject.GetComponent<TextMeshPro>();
            var floatingController = textObject.GetComponent<FloatingTextController>();
            floatingController.mainCamera = Camera.main;
            textMeshPro.text = text;
        }
    }
}
