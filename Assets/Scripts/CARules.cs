using UnityEngine;

public class CellularAutomataRules
{
    private Vector3 shellCenter;

    public CellularAutomataRules(Vector3 center)
    {
        shellCenter = center;
    }

    public void ApplyRules(Voxel[,,] grid)
    {
        // Process bottom-up for proper falling
        for (int y = 1; y < grid.GetLength(1); y++)
        {
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int z = 0; z < grid.GetLength(2); z++)
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
        // Calculate direction to center (no normalization needed)
        Vector3 toCenter = shellCenter - new Vector3(x, y, z);

        // Get integer movement directions (-1, 0, or 1 for each axis)
        int xStep = toCenter.x > 0 ? 1 : (toCenter.x < 0 ? -1 : 0);
        int yStep = toCenter.y > 0 ? 1 : (toCenter.y < 0 ? -1 : 0);
        int zStep = toCenter.z > 0 ? 1 : (toCenter.z < 0 ? -1 : 0);

        // Try moving directly toward center first
        if (IsValidPosition(grid, x + xStep, y + yStep, z + zStep))
        {
            SwapVoxels(grid, x, y, z, x + xStep, y + yStep, z + zStep);
            return;
        }

        // If blocked, try alternative 2D radial movements
        if (IsValidPosition(grid, x + xStep, y + yStep, z)) // X+Y
        {
            SwapVoxels(grid, x, y, z, x + xStep, y + yStep, z);
            return;
        }
        if (IsValidPosition(grid, x, y + yStep, z + zStep)) // Y+Z
        {
            SwapVoxels(grid, x, y, z, x, y + yStep, z + zStep);
            return;
        }
        if (IsValidPosition(grid, x + xStep, y, z + zStep)) // X+Z
        {
            SwapVoxels(grid, x, y, z, x + xStep, y, z + zStep);
            return;
        }

        // If still blocked, try pure axis movements
        if (IsValidPosition(grid, x + xStep, y, z)) // X only
        {
            SwapVoxels(grid, x, y, z, x + xStep, y, z);
            return;
        }
        if (IsValidPosition(grid, x, y + yStep, z)) // Y only
        {
            SwapVoxels(grid, x, y, z, x, y + yStep, z);
            return;
        }
        if (IsValidPosition(grid, x, y, z + zStep)) // Z only
        {
            SwapVoxels(grid, x, y, z, x, y, z + zStep);
            return;
        }
    }

    private bool IsValidPosition(Voxel[,,] grid, int x, int y, int z)
    {
        return x >= 0 && x < grid.GetLength(0) &&
               y >= 0 && y < grid.GetLength(1) &&
               z >= 0 && z < grid.GetLength(2) &&
               grid[x, y, z].material == MaterialType.Air;
    }

    private void SwapVoxels(Voxel[,,] grid, int x1, int y1, int z1, int x2, int y2, int z2)
    {
        Voxel temp = grid[x1, y1, z1];
        grid[x1, y1, z1] = grid[x2, y2, z2];
        grid[x2, y2, z2] = temp;
    }
}