using System;
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
    private static Vector3[] baseVerts =
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
    
    private static Vector2[] baseUvs =
    {
        new (0, 1),
        new (1, 1),
        new (0, 0),
        new (1, 0),
    };
    
    public void GenerateBlocks()
    {
        bool[,] drawn = new bool[15, 15];

        for (int y = 0; y < 15; y++)
        {
            for (int x = 0; x < 15; x++)
            {
                if (drawn[y, x]) continue;
                
                var block = map[y, x];
                
                if (block["type"] == "air") continue;

                // front back left right up down
                bool[] sides = { true, true, true, true, true, true };
                
                string[] transparent = { "air", "tombstone", "leaves", "spawner"};
                
                var type = block["type"];

                if (type == "tombstone") // rendering for special blocks
                {
                    //no fucking tombstones i am alladeen madafaka
                    CreateBlockWithModel("tombstone", x-7, y-7);
                }
                else // greedy meshing algorithm
                {
                    int x_width = 0;
                    
                    // determine x width
                    for (int xx = 0; xx < (15 - x); xx++)
                    {
                        if (map[y, x + xx]["type"] != block["type"] || drawn[y, x+xx])
                        {
                            break;
                        }
                        x_width += 1;
                    }
                    
                    int y_width = 0;

                    for (int yy = 0; yy < (15 - y); yy++)
                    {
                        bool failed = false;
                        for (int xx = 0; xx < x_width; xx++)
                        {
                            if (map[y + yy, x + xx]["type"] != block["type"] || drawn[y+yy, x+xx])
                            {
                                failed = true;
                                break;
                            }
                        }

                        if (failed)
                            break;

                        for (int xx = 0; xx < x_width; xx++)
                        {
                            drawn[y + yy, x + xx] = true;
                        }
                        
                        y_width += 1;
                    }
                    
                    // block face culling
                    if (y > 0)
                    {
                        sides[0] = false;
                        for (int xx = 0; xx < x_width; xx++)
                        {
                            if (transparent.Contains(map[y - 1, x + xx]["type"]))
                            {
                                sides[0] = true;
                                break;
                            }
                        }
                    }

                    if (y + y_width < 15)
                    {
                        sides[1] = false;
                        for (int xx = 0; xx < x_width; xx++)
                        {
                            if (transparent.Contains(map[y + y_width, x + xx]["type"]))
                            {
                                sides[1] = true;
                                break;
                            }
                        }
                    }

                    if (x > 0)
                    {
                        sides[2] = false;
                        for (int yy = 0; yy < y_width; yy++)
                        {
                            if (transparent.Contains(map[y + yy, x - 1]["type"]))
                            {
                                sides[2] = true;
                                break;
                            }
                        }
                    }

                    if (x + x_width < 15)
                    {
                        sides[3] = false;
                        for (int yy = 0; yy < y_width; yy++)
                        {
                            if (transparent.Contains(map[y + yy, x + x_width]["type"]))
                            {
                                sides[3] = true;
                                break;
                            }
                        }
                    }

                    CreateBlock(type, x-7, y-7, sides, x_width, y_width);
                }
            }
        }
    }

    public void GenerateEntities()
    {
        foreach (var entity in entities)
        {
            int x = int.Parse((string)entity["x"]);
            int y = int.Parse((string)entity["y"]);

            switch ((string)entity["type"])
            {
                case "player":
                    if (chunkPosition.y != 0 && chunkPosition.x == 0 && chunkPosition.z == 0 && x == 0 && y == 0) break;
                    CreateBlockWithModel((string)entity["type"], x, y, 1f, entity["name"] + " " + entity["hp"] + "/" + entity["max_hp"]);
                    break;
                case "monster":
                    CreateBlockWithModel((string)entity["type"], x, y, 1f, "Monster" + " " + entity["hp"] + "/" + entity["max_hp"]);
                    break;
                case "ghost":
                    CreateBlockWithModel((string)entity["type"], x, y, 1f, "Ghost");
                    break;
                default:
                    CreateBlockWithModel("none", x, y);
                    break;
            }
        }
    }

    private void CreateBlockWithModel(string textureName, int x, int y, float scale = 1, string text = null, float alpha = 1f)
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
            
            if (alpha != 1f)
            {
                block.GetComponent<Renderer>().sharedMaterial.SetFloat("_opacity", alpha);
                print(block.GetComponent<Renderer>().sharedMaterial.GetFloat("_opacity"));
            }
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

    private void CreateBlock(string textureName, int x, int y, bool[] sides, int x_width, int y_width)
    {
        // this is most likely slow as fuck due to using linq pls fix it
        BlockTypes.BlockType type = types.Find(item => item.name == textureName);
        if (type.material == null && type.modelOverride == null)
        {
            return; // TODO issue with this: stops rendering of blocks without texture
            //type = types.Find(item => item.name == "none");
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
        var collider = blockObject.transform.AddComponent<BoxCollider>();
        collider.size = new Vector3(x_width, 1f, y_width);
        collider.center = new Vector3(x_width * 0.5f, 0.5f, y_width * 0.5f);

        // custom mesh logic
        List<Vector3> verts = new();
        List<int> tris = new();
        List<Vector2> uvs = new();

        // generate correct faces
        int faceId = 0;
        for (int i = 0; i < 6; i++)
        {
            if (!sides[i])
                continue;
            
            float x_multiplier = 1f, z_multiplier = 1f;
            float x_offset = 0f, z_offset = 0f;

            switch (i)
            {
                case 0:
                    x_multiplier = x_width;
                    break;
                case 1:
                    z_offset = y_width - 1;
                    x_multiplier = x_width;
                    break;
                case 2:
                    z_multiplier = y_width;
                    break;
                case 3:
                    x_offset = x_width - 1;
                    z_multiplier = y_width;
                    break;
                case 4:
                    x_multiplier = x_width;
                    z_multiplier = y_width;
                    break;
                case 5:
                    x_multiplier = x_width;
                    z_multiplier = y_width;
                    break;
            }

            verts.Add(new Vector3(baseVerts[i * 4 + 0].x * x_multiplier + x_offset, baseVerts[i * 4 + 0].y, baseVerts[i * 4 + 0].z * z_multiplier + z_offset));
            verts.Add(new Vector3(baseVerts[i * 4 + 1].x * x_multiplier + x_offset, baseVerts[i * 4 + 1].y, baseVerts[i * 4 + 1].z * z_multiplier + z_offset));
            verts.Add(new Vector3(baseVerts[i * 4 + 2].x * x_multiplier + x_offset, baseVerts[i * 4 + 2].y, baseVerts[i * 4 + 2].z * z_multiplier + z_offset));
            verts.Add(new Vector3(baseVerts[i * 4 + 3].x * x_multiplier + x_offset, baseVerts[i * 4 + 3].y, baseVerts[i * 4 + 3].z * z_multiplier + z_offset));
            
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
        
        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.uv = uvs.ToArray();

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }
}
