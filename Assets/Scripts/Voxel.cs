using UnityEngine;

[System.Serializable]
public class Voxel
{
    public Vector3Int position;
    public MaterialType material;
    public float liquidAmount; // 0-1 representing how full the cell is
}