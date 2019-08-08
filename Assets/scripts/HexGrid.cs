
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour
{

    public int chunkCountX = 4;
    public int chunkCountZ = 3;
    private int cellCountX;
    private int cellCountZ;

    public HexGridChunk chunkPrefab;
    private HexGridChunk[] chunks;

    public HexCell cellPrefab;
    public Text cellLabelPrefab;

    public Color defaultColor = Color.white;
    public Color touchedColor = Color.magenta;

    private HexCell[] cells;
    public Texture2D noiseSource;

    private void Awake()
    {
        HexMetrics.noiseSource = noiseSource;

        cellCountX = chunkCountX * HexMetrics.chunkSizeX;
        cellCountZ = chunkCountZ * HexMetrics.chunkSizeZ;

        CreateChunks();
        CreateCells();
    }

    void CreateChunks()
    {
        chunks = new HexGridChunk[chunkCountX * chunkCountZ];
        for (int z = 0, i = 0; z < chunkCountZ; z++)
        {
            for (int x = 0; x < chunkCountX; x++)
            {
                HexGridChunk chunk = chunks[i++] = Instantiate<HexGridChunk>(chunkPrefab);
                chunk.transform.SetParent(transform);
            }
        }
    }

    void CreateCells()
    {
        cells = new HexCell[cellCountX * cellCountZ];

        for (int z = 0, i = 0; z < cellCountZ; z++)
        {
            for (int x = 0; x < cellCountX; x++)
            {
                CreateCell(x, z, i++);
            }
        }
    }

    void CreateCell(int x, int z, int i)
    {
        // grid position
        Vector3 pos;
        pos.x = (x + z * 0.5f - z / 2) * (HexMetrics.innerRadius * 2.0f);
        pos.y = 0.0f;
        pos.z = z * (HexMetrics.outerRadius * 1.5f);

        // create cell instance
        HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab);
        cell.transform.localPosition = pos;
        cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
        cell.color = defaultColor;

        // coords text label
        Text label = Instantiate<Text>(cellLabelPrefab);
        cell.uiTransform = label.transform;
        cell.uiRect = label.rectTransform;

        AddCellToChunk(x, z, cell);

        // TODO 
        label.transform.localPosition = new Vector2(pos.x, pos.z);
        label.text = cell.coordinates.ToStringOnSeparateLines();

        cell.SetElevation(0);

        // set cell neighbors
        if (x > 0) { cell.SetNeighbor(HexDirection.HexDirection_W, cells[i - 1]); }
        if (z > 0)
        {
            if ((z & 1) == 0)
            {
                cell.SetNeighbor(HexDirection.HexDirection_SE, cells[i - cellCountX]);
                if (x > 0)
                {
                    cell.SetNeighbor(HexDirection.HexDirection_SW, cells[i - cellCountX - 1]);
                }
            }
            else
            {
                cell.SetNeighbor(HexDirection.HexDirection_SW, cells[i - cellCountX]);
                if (x < cellCountX - 1)
                {
                    cell.SetNeighbor(HexDirection.HexDirection_SE, cells[i - cellCountX + 1]);
                }
            }
        }
    }

    private void AddCellToChunk(int x, int z, HexCell cell)
    {
        int chunkX = x / HexMetrics.chunkSizeX;
        int chunkZ = z / HexMetrics.chunkSizeZ;
        HexGridChunk chunk = chunks[chunkX + chunkZ * chunkCountX];

        int localX = x - chunkX * HexMetrics.chunkSizeX;
        int localZ = z - chunkZ * HexMetrics.chunkSizeZ;
        chunk.AddCell(localX + localZ * HexMetrics.chunkSizeX, cell);
    }

    public HexCell GetCell(Vector3 targetPos)
    {
        Vector3 pos = transform.InverseTransformPoint(targetPos);
        HexCoordinates coordinates = HexCoordinates.FromPosition(pos);

        int cellIndex = coordinates.Z * cellCountX + coordinates.Z / 2 + coordinates.X;
        return cells[cellIndex];
    }

    public HexCell GetCell(HexCoordinates coordinates)
    {
        int z = coordinates.Z;
        if (z < 0 || z >= cellCountZ) {
            return null;
        }
        int x = coordinates.X + z / 2;
        if (x < 0 || x >= cellCountX) {
            return null;
        }

        return cells[x + z * cellCountX];
    }

    public void OnEnable()
    {
        HexMetrics.noiseSource = noiseSource;
    }

    public void ShowUILable(bool visible)
    {
        for (int i = 0; i < chunks.Length; i++) {
            chunks[i].ShowUILable(visible);
        }
    }
}
