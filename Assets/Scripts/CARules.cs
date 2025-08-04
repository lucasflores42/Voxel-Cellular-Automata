using System.Collections.Generic;
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
        Vector3 toCenter = shellCenter - new Vector3(x, y, z);

        // Get all possible movement directions that point toward center
        List<Vector3Int> validDirections = new List<Vector3Int>();

        // Generate all 26 possible neighbor directions (3D Moore neighborhood)
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dy == 0 && dz == 0) continue; // Skip self

                    // Only consider directions that move toward center
                    if (dx * toCenter.x + dy * toCenter.y + dz * toCenter.z > 0)
                    {
                        validDirections.Add(new Vector3Int(dx, dy, dz));
                    }
                }
            }
        }

        // Shuffle directions to remove any processing order bias
        for (int i = 0; i < validDirections.Count; i++)
        {
            int randomIndex = Random.Range(i, validDirections.Count);
            (validDirections[i], validDirections[randomIndex]) = (validDirections[randomIndex], validDirections[i]);
        }

        // Try all valid directions until we find an empty spot
        foreach (Vector3Int dir in validDirections)
        {
            int tx = x + dir.x;
            int ty = y + dir.y;
            int tz = z + dir.z;

            if (IsValidPosition(grid, tx, ty, tz) &&
                grid[tx, ty, tz].material == MaterialType.Air)
            {
                SwapVoxels(grid, x, y, z, tx, ty, tz);
                return;
            }
        }
    }

    private bool IsValidPosition(Voxel[,,] grid, int x, int y, int z)
    {
        // Only check bounds now (material checks are done explicitly in TryMoveSand)
        return x >= 0 && x < grid.GetLength(0) &&
               y >= 0 && y < grid.GetLength(1) &&
               z >= 0 && z < grid.GetLength(2);
    }

    private void SwapVoxels(Voxel[,,] grid, int x1, int y1, int z1, int x2, int y2, int z2)
    {
        Voxel temp = grid[x1, y1, z1];
        grid[x1, y1, z1] = grid[x2, y2, z2];
        grid[x2, y2, z2] = temp;
    }
}