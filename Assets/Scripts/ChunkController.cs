using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class ChunkController : MonoBehaviour
{
    public Dictionary<string, string>[,] map;
    public Dictionary<string, object>[] entities;
    public Vector3Int chunkPosition;

    public GameObject floatingText;
    
    public List<BlockTypes.BlockType> types; // also temporary as heck
    
    // temp
    private Vector3[] baseVerts =
    {
        new(0, 1, 0),
        new(1, 1, 0),
        new(0, 0, 0),
        new(1, 0, 0),
        new (1, 1, 1),
        new (0, 1, 1),
        new (1, 0, 1),
        new (0, 0, 1),
        new (0, 1, 1),
        new (0, 1, 0),
        new (0, 0, 1),
        new (0, 0, 0),
        new (1, 1, 0),
        new (1, 1, 1),
        new (1, 0, 0),
        new (1, 0, 1),
        new (0, 1, 1),
        new (1, 1, 1),
        new (0, 1, 0),
        new (1, 1, 0),
        new (0, 0, 0),
        new (1, 0, 0),
        new (0, 0, 1),
        new (1, 0, 1)
    };
    
    private Vector2[] baseUvs =
    {
        new (0, 1),
        new (1, 1),
        new (0, 0),
        new (1, 0),
    };

    public void GenerateBlocks()
    {
        print("Starting generation");
        for (int y = 7; y >= -7; y--)
        {
            for (int x = -7; x <= 7; x++)
            {
                var block = map[y + 7, x + 7];
                
                if (block["type"] == "air") continue;
                
                // front back left right up down
                bool[] sides = { true, true, true, true, true, true };

                string[] transparent = { "air", "tombstone", "leaves", "spawner" };
                
                // block face culling start
                if (y != -7 && !transparent.Contains(map[y + 7 - 1, x + 7]["type"]))
                    sides[0] = false;
                if (y != 7 && !transparent.Contains(map[y + 7 + 1, x + 7]["type"]))
                    sides[1] = false;

                if (x != -7 && !transparent.Contains(map[y + 7, x + 7 - 1]["type"]))
                    sides[2] = false;
                if (x != 7 && !transparent.Contains(map[y + 7, x + 7 + 1]["type"]))
                    sides[3] = false;

                var type = block["type"];

                if (type == "tombstone")
                {
                    CreateBlockWithModel("tombstone", x, y);
                }
                else
                {
                    CreateBlock(type, x, y, sides);
                }

                /*switch (block["type"])
                {
                    case "tombstone":
                        CreateBlockWithModel("tombstone", x, y);
                        break;
                    default:
                        var type = block.ContainsKey(block["type"]) ? block["type"] : "none";
                        CreateBlock(type, x, y, sides);
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
                    if (chunkPosition.y != 0 && chunkPosition.x == 0 && chunkPosition.z == 0 && x == 0 && y == 0) break;
                    CreateBlockWithModel("none", x, y,1f, entity["name"] + " " + entity["hp"] + "/" + entity["max_hp"]);
                    break;
                case "monster":
                    CreateBlockWithModel("none", x, y,1f, "Monster" + " " + entity["hp"] + "/" + entity["max_hp"]);
                    break;
                case "ghost":
                    CreateBlockWithModel("none", x, y,1f, "Ghost");
                    break;
                default:
                    CreateBlockWithModel("none", x, y);
                    break;
            }
        }
    }

    private void CreateBlockWithModel(string textureName, int x, int y, float scale = 1, string text = null)
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
            new Vector3(x + (15 * chunkPosition.x) + 0.5f, chunkPosition.y + 0.5f, y + (15 * chunkPosition.z) + 0.5f);
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

    private void CreateBlock(string textureName, int x, int y, bool[] sides)
    {
        BlockTypes.BlockType type = types.Find(item => item.name == textureName);
        if (type.material == null && type.modelOverride == null) // todo clean this ugly ass shit up
        {
            type = types.Find(item => item.name == "none");
        }
        
        var blockObject = new GameObject
        {
            name = "block",
            transform =
            {
                parent = transform,
                position = new Vector3(x + (15 * chunkPosition.x), chunkPosition.y, y + (15 * chunkPosition.z))
            }
        };

        Mesh mesh = new Mesh();
        var renderer = blockObject.transform.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = type.material;
        var filter = blockObject.transform.AddComponent<MeshFilter>();
        filter.mesh = mesh;
        
        // custom mesh logic
        List<Vector3> verts = new();
        List<int> tris = new();
        List<Vector2> uvs = new();

        int faceId = 0;
        for (int i = 0; i < 6; i++)
        {
            if (sides[i])
            {
                verts.Add(baseVerts[i * 4 + 0]);
                verts.Add(baseVerts[i * 4 + 1]);
                verts.Add(baseVerts[i * 4 + 2]);
                verts.Add(baseVerts[i * 4 + 3]);
                
                tris.Add(0 + faceId * 4);
                tris.Add(1 + faceId * 4);
                tris.Add(2 + faceId * 4);
                tris.Add(1 + faceId * 4);
                tris.Add(3 + faceId * 4);
                tris.Add(2 + faceId * 4);
                
                uvs.Add(baseUvs[0]);
                uvs.Add(baseUvs[1]);
                uvs.Add(baseUvs[2]);
                uvs.Add(baseUvs[3]);

                faceId ++;
            }
        }
        
        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.uv = uvs.ToArray();
          
        mesh.RecalculateNormals();
    }
}
