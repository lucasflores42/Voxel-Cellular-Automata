using UnityEngine;

public class VoxelVisualizer : MonoBehaviour
{
    public GameObject voxelPrefab;
    private GameObject[,,] visualGrid;

    void Start()
    {
        if (VoxelManager.Instance == null || voxelPrefab == null)
        {
            Debug.LogError("Missing references!");
            enabled = false;
            return;
        }

        InitializeGrid();
        CreateAllVoxels();
    }

    void InitializeGrid()
    {
        visualGrid = new GameObject[
            VoxelManager.Instance.gridSize,
            VoxelManager.Instance.gridSize,
            VoxelManager.Instance.gridSize];
    }

    public void CreateAllVoxels()
    {
        if (VoxelManager.Instance?.voxelGrid == null) return;

        for (int x = 0; x < VoxelManager.Instance.gridSize; x++)
            for (int y = 0; y < VoxelManager.Instance.gridSize; y++)
                for (int z = 0; z < VoxelManager.Instance.gridSize; z++)
                    UpdateVoxelVisual(x, y, z);
    }

    void UpdateVoxelVisual(int x, int y, int z)
    {
        // Clean up old visual
        if (visualGrid[x, y, z] != null)
            Destroy(visualGrid[x, y, z]);

        // Create new visual if not air
        if (VoxelManager.Instance.voxelGrid[x, y, z].material != MaterialType.Air)
        {
            float size = VoxelManager.Instance.voxelSize;
            Vector3 position = new Vector3(x * size, y * size, z * size);

            visualGrid[x, y, z] = Instantiate(voxelPrefab, position, Quaternion.identity, transform);
            visualGrid[x, y, z].transform.localScale = Vector3.one * size;
            visualGrid[x, y, z].name = $"Voxel_{x}_{y}_{z}";

            // Set material
            Material mat = VoxelManager.Instance.GetMaterial(
                VoxelManager.Instance.voxelGrid[x, y, z].material
            );
            if (mat != null)
                visualGrid[x, y, z].GetComponent<Renderer>().material = mat;
        }
    }
}