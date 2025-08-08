using System.Collections.Generic;
using NUnit.Framework.Constraints;
using UnityEngine;

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
        // Assuming each voxel has a liquidAmount field (0-1)
        float currentLiquid = grid[x, y, z].liquidAmount;
        if (currentLiquid <= 0) return;

        // Rule 1: Flow downward first
        if (y > 0 && TryFlow(grid, x, y, z, x, y - 1, z, currentLiquid))
        {
            return; // If we flowed down, wait for next frame to flow sideways
        }

        // Rule 2: Flow sideways (4 directions in 3D)
        Vector3Int[] sideDirections = {
        new Vector3Int(-1, 0, 0),  // Left
        new Vector3Int(1, 0, 0),    // Right
        new Vector3Int(0, 0, -1),   // Back
        new Vector3Int(0, 0, 1)     // Forward
    };

        // Shuffle directions for more natural flow
        ShuffleDirections(sideDirections);

        foreach (var dir in sideDirections)
        {
            int nx = x + dir.x;
            int ny = y + dir.y;
            int nz = z + dir.z;

            if (IsValidPosition(grid, nx, ny, nz) &&
                grid[nx, ny, nz].material != MaterialType.Stone)
            {
                if (TryFlow(grid, x, y, z, nx, ny, nz, currentLiquid))
                {
                    // Only flow to one side per frame for more natural behavior
                    break;
                }
            }
        }

        // Rule 3: Flow upward if pressurized (liquid > max capacity)
        const float maxLiquid = 1.0f;
        if (currentLiquid > maxLiquid && y < grid.GetLength(1) - 1)
        {
            int nx = x;
            int ny = y + 1; // Up
            int nz = z;

            if (IsValidPosition(grid, nx, ny, nz) &&
                grid[nx, ny, nz].material != MaterialType.Stone)
            {
                // Calculate overflow
                float overflow = currentLiquid - maxLiquid;
                float transferAmount = overflow * 0.5f; // Dampen the upward flow

                // Transfer liquid upward
                grid[x, y, z].liquidAmount -= transferAmount;
                grid[nx, ny, nz].liquidAmount += transferAmount;
            }
        }
    }

    private bool TryFlow(Voxel[,,] grid, int x1, int y1, int z1, int x2, int y2, int z2, float sourceLiquid)
    {
        Voxel source = grid[x1, y1, z1];
        Voxel dest = grid[x2, y2, z2];

        // Can't flow into solids
        if (dest.material == MaterialType.Stone) return false;

        // Special case: if destination is air, do a full swap
        if (dest.material == MaterialType.Air)
        {
            // Complete swap of the voxels
            SwapVoxels(grid, x1, y1, z1, x2, y2, z2);
            return true;
        }

        // For water-to-water flow, only flow if destination has less liquid
        if (dest.material == MaterialType.Water && dest.liquidAmount >= source.liquidAmount)
            return false;

        // Calculate flow amount (simplified)
        float flowAmount = (source.liquidAmount - dest.liquidAmount) * 0.5f;
        flowAmount = Mathf.Min(flowAmount, source.liquidAmount); // Don't over-transfer

        // Apply the flow
        source.liquidAmount -= flowAmount;
        dest.liquidAmount += flowAmount;

        // Ensure the destination becomes water if it receives any liquid
        if (dest.liquidAmount > 0)
        {
            dest.material = MaterialType.Water;
        }

        // If source is now empty, turn it back to air
        if (source.liquidAmount <= 0)
        {
            source.material = MaterialType.Air;
            source.liquidAmount = 0;
        }

        return true;
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