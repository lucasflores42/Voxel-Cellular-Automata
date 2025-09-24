using System.Collections.Generic;
using NUnit.Framework.Constraints;
using NUnit.Framework.Internal;
using UnityEngine;
using static Unity.VisualScripting.Member;

public class CellularAutomataRules
{
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
                        SandDynamics(grid, x, y, z);
                    }
                    if (grid[x, y, z].material == MaterialType.Water)
                    {
                        WaterDynamics(grid, x, y, z);
                    }
                }
            }
        }
    }

    /* ************************************************************************************
     *                                                                                    *
     *                                   Water Dynamics                                   * 
     *                                                                                    *
     ************************************************************************************ */
    private void WaterDynamics(Voxel[,,] grid, int x, int y, int z)
    {
        float currentLiquid = grid[x, y, z].liquidAmount;

        // Rule 1: Flow downward first (gravity)
        // air
        if (y > 0 && (grid[x, y - 1, z].material == MaterialType.Air))
        {
            SwapVoxels(grid, x, y, z, x, y - 1, z);
            return;
        }
        // water
        else if (grid[x, y - 1, z].material == MaterialType.Water &&
             grid[x, y, z].liquidAmount > grid[x, y - 1, z].liquidAmount)
        {
            // Equalize the water amounts between current and bottom voxel
            float flow = 1 - grid[x, y - 1, z].liquidAmount;

            grid[x, y, z].liquidAmount -= flow;
            grid[x, y - 1, z].liquidAmount += flow;

            if (grid[x, y, z].liquidAmount < 0.5)
            {
                grid[x, y, z].material = MaterialType.Air;
            }

            return;
        }

        // Rule 2: Flow sideways
        Vector3Int[] sideDirections = {
        new Vector3Int(-1, 0, 0),  // Left
        new Vector3Int(1, 0, 0),    // Right
        new Vector3Int(0, 0, -1),   // Back
        new Vector3Int(0, 0, 1)     // Forward
    };

        // First pass: calculate total difference and count neighbors
        int num = 0;
        float totalDifference = 0;

        foreach (var dir in sideDirections)
        {
            int nx = x + dir.x;
            int ny = y + dir.y;
            int nz = z + dir.z;

            if (IsValidPosition(grid, nx, ny, nz) && grid[x, y, z].liquidAmount > grid[nx, ny, nz].liquidAmount)
            {
                num++;
                totalDifference += grid[x, y, z].liquidAmount - grid[nx, ny, nz].liquidAmount;
            }
        }

        if (num > 0)
        {
            // Second pass: apply flow based on original values
            foreach (var dir in sideDirections)
            {
                int nx = x + dir.x;
                int ny = y + dir.y;
                int nz = z + dir.z;

                if (IsValidPosition(grid, nx, ny, nz) && currentLiquid > grid[nx, ny, nz].liquidAmount)
                {
                    float neighborDiff = currentLiquid - grid[nx, ny, nz].liquidAmount;
                    float flowAmount = (neighborDiff / totalDifference) * (currentLiquid - 0.5f) * 0.5f;

                    grid[x, y, z].liquidAmount -= flowAmount;
                    grid[nx, ny, nz].liquidAmount += flowAmount;

                    if (grid[nx, ny, nz].liquidAmount >= 0.5)
                    {
                        grid[nx, ny, nz].material = MaterialType.Water;
                    }
                }
            }

            // Check source emptiness after all flows
            if (grid[x, y, z].liquidAmount < 0.5)
            {
                grid[x, y, z].material = MaterialType.Air;
            }
        }
        else
        {
            // Rule 3: Flow upward if pressurized
            if (currentLiquid > 1 && y < grid.GetLength(1) - 1)
            {
                int nx = x;
                int ny = y + 1;
                int nz = z;

                if (IsValidPosition(grid, nx, ny, nz) && grid[nx, ny, nz].material != MaterialType.Stone)
                {
                    float flowAmount = (currentLiquid - 1);
                    grid[x, y, z].liquidAmount -= flowAmount;
                    grid[nx, ny, nz].liquidAmount += flowAmount;

                    if (grid[nx, ny, nz].liquidAmount >= 0.5)
                    {
                        grid[nx, ny, nz].material = MaterialType.Water;
                    }
                    if (grid[x, y, z].liquidAmount < 0.5)
                    {
                        grid[x, y, z].material = MaterialType.Air;
                    }
                }
            }
        }
    }

    private void ShuffleDirections(Vector3Int[] directions)
    {
        // Fisher-Yates shuffle
        for (int i = directions.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Vector3Int temp = directions[i];
            directions[i] = directions[j];
            directions[j] = temp;
        }
    }

    /* ************************************************************************************
     *                                                                                    *
     *                                   Sand Dynamics                                    * 
     *                                                                                    *
     ************************************************************************************ */
    private void SandDynamics(Voxel[,,] grid, int x, int y, int z)
    {
        // First try moving straight down
        if (IsValidPosition(grid, x, y - 1, z) &&
            grid[x, y - 1, z].material == MaterialType.Air)
        {
            SwapVoxels(grid, x, y, z, x, y - 1, z);
            return;
        }

        // If blocked below, try the 4 horizontal directions 
        Vector3Int[] directions = {
            new Vector3Int(-1, -1, 0),  // Down-left
            new Vector3Int(1, -1, 0),   // Down-right
            new Vector3Int(0, -1, -1),  // Down-back
            new Vector3Int(0, -1, 1),   // Down-forward
            new Vector3Int(-1, -1, -1), // Down-left-back
            new Vector3Int(-1, -1, 1), // Down-left-forward
            new Vector3Int(1, -1, -1),  // Down-right-back
            new Vector3Int(1, -1, 1)    // Down-right-forward
        };

        for (int i = 0; i < 4; i++)
        {
            int randomIndex = Random.Range(1, directions.Length);
            int tx = x + directions[randomIndex].x;
            int ty = y + directions[randomIndex].y;
            int tz = z + directions[randomIndex].z;

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