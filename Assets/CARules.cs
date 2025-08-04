using UnityEngine;

public class CellularAutomataRules
{
    public void ApplyRules(Voxel[,,] grid)
    {
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);
        int depth = grid.GetLength(2);

        // Process from BOTTOM to TOP for proper falling
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    if (grid[x, y, z].material == MaterialType.Sand)
                    {
                        TryMoveSand(grid, x, y, z);
                    }
                }
            }
        }
    }

    private void TryMoveSand(Voxel[,,] grid, int x, int y, int z)
    {
        // Can't move below bottom
        if (y <= 0) return;

        // Check if below is air
        if (grid[x, y - 1, z].material == MaterialType.Air)
        {
            SwapVoxels(grid, x, y, z, x, y - 1, z);
            return;
        }

        // Optional: Add diagonal falling for more natural behavior
        bool canMoveLeft = x > 0;
        bool canMoveRight = x < grid.GetLength(0) - 1;

        if (canMoveLeft && grid[x - 1, y - 1, z].material == MaterialType.Air)
        {
            SwapVoxels(grid, x, y, z, x - 1, y - 1, z);
            return;
        }

        if (canMoveRight && grid[x + 1, y - 1, z].material == MaterialType.Air)
        {
            SwapVoxels(grid, x, y, z, x + 1, y - 1, z);
            return;
        }

    }

    private void SwapVoxels(Voxel[,,] grid, int x1, int y1, int z1, int x2, int y2, int z2)
    {
        Voxel temp = grid[x1, y1, z1];
        grid[x1, y1, z1] = grid[x2, y2, z2];
        grid[x2, y2, z2] = temp;

        // Update positions
        grid[x1, y1, z1].position = new Vector3Int(x1, y1, z1);
        grid[x2, y2, z2].position = new Vector3Int(x2, y2, z2);
    }
}