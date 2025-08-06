using System.Collections.Generic;
using UnityEngine;

public class CellularAutomataRules
{
    public void ApplyRules(Voxel[,,] grid)
    {
        // First reset all movement flags
        ResetMovementFlags(grid);

        // Process bottom-up for proper falling
        for (int y = 1; y < grid.GetLength(1); y++)
        {
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int z = 0; z < grid.GetLength(2); z++)
                {
                    if (grid[x, y, z].material == MaterialType.Sand && !grid[x, y, z].hasMoved)
                    {
                        SandDynamics(grid, x, y, z);
                    }
                    if (grid[x, y, z].material == MaterialType.Water && !grid[x, y, z].hasMoved)
                    {
                        WaterDynamics(grid, x, y, z);
                    }
                }
            }
        }
    }

    private void ResetMovementFlags(Voxel[,,] grid)
    {
        for (int y = 0; y < grid.GetLength(1); y++)
        {
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int z = 0; z < grid.GetLength(2); z++)
                {
                    grid[x, y, z].hasMoved = false;
                }
            }
        }
    }

    private void WaterDynamics(Voxel[,,] grid, int x, int y, int z)
    {
        // Try moving straight down into air first
        if (IsValidPosition(grid, x, y - 1, z) &&
            grid[x, y - 1, z].material == MaterialType.Air)
        {
            SwapVoxels(grid, x, y, z, x, y - 1, z);
            grid[x, y - 1, z].hasMoved = true;
            return;
        }

        // If blocked by water below, push it (with recursion limit)
        if (IsValidPosition(grid, x, y - 1, z) &&
            grid[x, y - 1, z].material == MaterialType.Water && grid[x, y - 1, z].hasMoved == true)
        {
            PushWater(grid, x, y - 1, z, new Vector3Int(0, -1, 0), 0);
        }
    }

    private void PushWater(Voxel[,,] grid, int x, int y, int z, Vector3Int pushDirection, int recursionDepth)
    {
        const int maxRecursion = 3;

        if (recursionDepth > maxRecursion || grid[x, y, z].hasMoved)
            return;

        Vector3Int moveDir = pushDirection;
        int nx = x + moveDir.x;
        int ny = y + moveDir.y;
        int nz = z + moveDir.z;

        if (IsValidPosition(grid, nx, ny, nz) && grid[nx, ny, nz].material == MaterialType.Air)
        {
            SwapVoxels(grid, x, y, z, nx, ny, nz);
            grid[nx, ny, nz].hasMoved = true;
            return;
        }

        // Get perpendicular directions based on push direction
        List<Vector3Int> perpendicularDirections = GetPerpendicularDirections(pushDirection);

        // Try all perpendicular directions
        foreach (Vector3Int dir in perpendicularDirections)
        {
            nx = x + dir.x;
            ny = y + dir.y;
            nz = z + dir.z;

            if (IsValidPosition(grid, nx, ny, nz) && grid[nx, ny, nz].material == MaterialType.Air)
            {
                SwapVoxels(grid, x, y, z, nx, ny, nz);
                grid[nx, ny, nz].hasMoved = true;
                return;
            }
        }

        // If completely blocked, push all neighboring water in perpendicular directions
        foreach (Vector3Int dir in perpendicularDirections)
        {
            nx = x + dir.x;
            ny = y + dir.y;
            nz = z + dir.z;

            if (IsValidPosition(grid, nx, ny, nz) &&
                grid[nx, ny, nz].material == MaterialType.Water)
            {
                PushWater(grid, nx, ny, nz, dir, recursionDepth + 1);
            }
        }
    }

    private List<Vector3Int> GetPerpendicularDirections(Vector3Int direction)
    {
        List<Vector3Int> perpendicularDirs = new List<Vector3Int>();

        // Handle primary axes
        if (direction.x != 0) // Pushing along x-axis
        {
            perpendicularDirs.Add(new Vector3Int(0, 0, 1));  // Forward
            perpendicularDirs.Add(new Vector3Int(0, 0, -1)); // Back
            perpendicularDirs.Add(new Vector3Int(0, 1, 0));  // Up
            perpendicularDirs.Add(new Vector3Int(0, -1, 0)); // Down
        }
        else if (direction.y != 0) // Pushing along y-axis
        {
            perpendicularDirs.Add(new Vector3Int(1, 0, 0));  // Right
            perpendicularDirs.Add(new Vector3Int(-1, 0, 0)); // Left
            perpendicularDirs.Add(new Vector3Int(0, 0, 1));  // Forward
            perpendicularDirs.Add(new Vector3Int(0, 0, -1)); // Back
        }
        else if (direction.z != 0) // Pushing along z-axis
        {
            perpendicularDirs.Add(new Vector3Int(1, 0, 0));  // Right
            perpendicularDirs.Add(new Vector3Int(-1, 0, 0)); // Left
            perpendicularDirs.Add(new Vector3Int(0, 1, 0));  // Up
            perpendicularDirs.Add(new Vector3Int(0, -1, 0)); // Down
        }

        return perpendicularDirs;
    }

    private void SandDynamics(Voxel[,,] grid, int x, int y, int z)
    {
        // First try moving straight down
        if (IsValidPosition(grid, x, y - 1, z) &&
            grid[x, y - 1, z].material == MaterialType.Air)
        {
            SwapVoxels(grid, x, y, z, x, y - 1, z);
            grid[x, y - 1, z].hasMoved = true;
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
                grid[tx, ty, tz].hasMoved = true;
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