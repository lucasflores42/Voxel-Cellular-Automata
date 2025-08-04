using Unity.Hierarchy;
using UnityEngine;

public class VoxelManager : MonoBehaviour
{
    public static VoxelManager Instance { get; private set; }
    public Vector3 shellCenter { get; private set; }

    [Header("Settings")]
    public int gridSize = 32;
    public float stepInterval = 0.1f;
    [Range(0.1f, 1f)] public float voxelSize = 1f; // Size control here

    [Header("Materials")]
    public Material sandMaterial;
    public Material waterMaterial;
    public Material stoneMaterial;
    public Material lavaMaterial;

    [HideInInspector] public Voxel[,,] voxelGrid;
    private CellularAutomataRules caRules;
    private float timer;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        InitializeGrid();
        shellCenter = new Vector3(gridSize / 2f, gridSize / 2f, gridSize / 2f);
        CreateTerrain();
        caRules = new CellularAutomataRules(shellCenter);
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer > stepInterval)
        {
            caRules.ApplyRules(voxelGrid);
            UpdateAllVisuals();
            timer = 0;
        }
    }

    void InitializeGrid()
    {
        voxelGrid = new Voxel[gridSize, gridSize, gridSize];
        for (int x = 0; x < gridSize; x++)
            for (int y = 0; y < gridSize; y++)
                for (int z = 0; z < gridSize; z++)
                    voxelGrid[x, y, z] = new Voxel()
                    {
                        position = new Vector3Int(x, y, z),
                        material = MaterialType.Air
                    };
    }

    void CreateTerrain()
    {
        // Create stone sphere shell
        int radius = 10;
        float thickness = 3f; // Shell thickness

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    // Calculate distance from center
                    Vector3 voxelPos = new Vector3(x, y, z);
                    float distance = Vector3.Distance(voxelPos, shellCenter);

                    // Create hollow sphere shell
                    if (distance <= radius && distance >= radius - thickness)
                    {
                        voxelGrid[x, y, z].material = MaterialType.Stone;
                    }
                }
            }
        }

        // Create sand blocks above the sphere
        int sandHeight = gridSize-1;
        for (int x = gridSize / 4; x < gridSize * 3 / 4; x++)
        {
            for (int z = gridSize / 4; z < gridSize * 3 / 4; z++)
            {
                voxelGrid[x, z, sandHeight].material = MaterialType.Sand;
            }
        }
    }

    void UpdateAllVisuals()
    {
        VoxelVisualizer visualizer = FindAnyObjectByType<VoxelVisualizer>();
        if (visualizer != null)
        {
            visualizer.CreateAllVoxels(); // Just trigger visual update
        }
    }

    public Material GetMaterial(MaterialType type)
    {
        return type switch
        {
            MaterialType.Sand => sandMaterial,
            MaterialType.Water => waterMaterial,
            MaterialType.Stone => stoneMaterial,
            MaterialType.Lava => lavaMaterial,
            _ => null
        };
    }
}