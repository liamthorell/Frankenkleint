using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "Custom", menuName = "ScriptableObjects/BlockTypes", order = 1)]
public class BlockTypes : ScriptableObject
{
    [System.Serializable]
    public struct BlockType
    {
        public string name;
        public int textureIndex;
        public GameObject modelOverride;
        public Material material; // not serialized somehow?
    }

    public BlockType[] blocks;
}
