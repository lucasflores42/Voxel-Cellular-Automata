using Unity.Hierarchy;
using UnityEngine;

public class VoxelManager : MonoBehaviour
{
    public static VoxelManager Instance { get; private set; }

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
        CreateTerrain();
        caRules = new CellularAutomataRules();
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
        {
            for (int y = 0; y < gridSize; y++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    voxelGrid[x, y, z] = new Voxel()
                    {
                        position = new Vector3Int(x, y, z),
                        material = MaterialType.Air
                    };
                }
            }
        }
    }

    void CreateTerrain()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    int center = gridSize / 2;
                    float dist = Mathf.Sqrt(Mathf.Pow(x - center, 2) + Mathf.Pow(z - center, 2));

                    if (y == 0)
                    {
                        voxelGrid[x, y, z].material = MaterialType.Stone;
                    }
                    else if (y < 4 )
                    {
                        voxelGrid[x, y, z].material = MaterialType.Water;
                        voxelGrid[x, y, z].liquidAmount = 1.0f;
                    }
                    else if (y <= gridSize-1 && y >= gridSize/2  && x == gridSize/2 && z == gridSize/2)
                    {
                        voxelGrid[x, y, z].material = MaterialType.Water;
                        voxelGrid[x, y, z].liquidAmount = 1.0f;
                    }
                    if (y < gridSize / 2 && x == 0 || z == 0 || x == gridSize-1 || z == gridSize-1)
                    {
                        voxelGrid[x, y, z].material = MaterialType.Stone; 
                    }
                }
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