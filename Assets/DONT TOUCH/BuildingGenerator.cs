using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
[System.Serializable]
public class GridCell
{
    public Vector3 worldPos;
    public bool occupied;
}

[ExecuteAlways]
public class BuildingGenerator : MonoBehaviour
{
    public List<BuildingPart> parts = new List<BuildingPart>();

    public Vector2 tileSize = new Vector2(1f, 1f);
    public Vector3Int gridSize = new Vector3Int(3, 5, 3);

    public bool regenerate;

    public GridCell[,,] grid;
    public Transform generatedRoot;

    void OnValidate()
    {
        GenerateGrid();

        if (regenerate)
        {
            regenerate = false;
            RegenerateBuilding();
        }
    }

    void GenerateGrid()
    {
        if (gridSize.x <= 0 || gridSize.y <= 0 || gridSize.z <= 0)
            return;

        grid = new GridCell[gridSize.x, gridSize.y, gridSize.z];

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                for (int z = 0; z < gridSize.z; z++)
                {
                    Vector3 pos = transform.position +
                                  new Vector3(
                                      x * tileSize.x,
                                      y,
                                      z * tileSize.y
                                  );

                    grid[x, y, z] = new GridCell
                    {
                        worldPos = pos,
                        occupied = false
                    };
                }
            }
        }
    }

    void RegenerateBuilding()
    {
        ClearGenerated();
        GenerateBuilding();
    }

    void ClearGenerated()
    {
        if (generatedRoot == null)
        {
            GameObject root = new GameObject("Generated_Building");
            root.transform.SetParent(transform);
            root.transform.localPosition = Vector3.zero;
            generatedRoot = root.transform;
        }

        while (generatedRoot.childCount > 0)
        {
            DestroyImmediate(generatedRoot.GetChild(0).gameObject);
        }
    }

    void GenerateBuilding()
    {
        if (grid == null || parts == null || parts.Count == 0)
            return;

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                for (int z = 0; z < gridSize.z; z++)
                {
                    GridCell cell = grid[x, y, z];

                    if (y == 0)
                    {
                        Spawn(cell, BuildingPart.Type.Floor);
                    }
                    else if (IsEdge(x, z))
                    {
                        Spawn(cell, BuildingPart.Type.Wall);
                    }
                }
            }
        }
    }

    void Spawn(GridCell cell, BuildingPart.Type type)
    {
        BuildingPart prefab = parts.Find(p => p.type == type);
        if (prefab == null) return;

        GameObject go = Instantiate(
            prefab.gameObject,
            cell.worldPos,
            Quaternion.identity,
            generatedRoot
        );

        cell.occupied = true;
    }

    bool IsEdge(int x, int z)
    {
        return x == 0 || z == 0 ||
               x == gridSize.x - 1 ||
               z == gridSize.z - 1;
    }

    void OnDrawGizmos()
    {
        if (grid == null) return;

        Gizmos.color = Color.red;

        foreach (var cell in grid)
        {
            Gizmos.DrawWireCube(
                cell.worldPos + new Vector3(tileSize.x, 1f, tileSize.y) * 0.5f,
                new Vector3(tileSize.x, 1f, tileSize.y)
            );
        }
    }
}
