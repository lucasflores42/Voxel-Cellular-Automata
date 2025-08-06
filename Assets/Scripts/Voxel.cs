using UnityEngine;

[System.Serializable]
public class Voxel
{
    public Vector3Int position;
    public MaterialType material;
    public int health;
    public float temperature;
    public bool hasMoved;

}