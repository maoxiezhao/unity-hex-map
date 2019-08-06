using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour {

    public int width = 6;
    public int height = 6;
    public HexCell cellPrefab;
    public Text cellLabelPrefab;

    public Color defaultColor = Color.white;
    public Color touchedColor = Color.magenta;

    private HexCell[] cells;
    private Canvas gridCanvas;
    private HexCellMesh hexMesh;

    public Texture2D noiseSource;

    private void Awake()
    {
        HexMetrics.noiseSource = noiseSource;

        cells = new HexCell[height * width];
        gridCanvas = GetComponentInChildren<Canvas>();
        hexMesh = GetComponentInChildren<HexCellMesh>();

        for (int z = 0, i = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
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
        cell.transform.SetParent(transform, false);
        cell.transform.localPosition = pos;
        cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
        cell.color = defaultColor;

        // set cell neighbors
        if (x > 0) { cell.SetNeighbor(HexDirection.HexDirection_W, cells[i - 1]); }
        if (z > 0)
        {
            if ((z & 1) == 0)
            {
                cell.SetNeighbor(HexDirection.HexDirection_SE, cells[i - width]);
                if (x > 0)
                {
                    cell.SetNeighbor(HexDirection.HexDirection_SW, cells[i - width - 1]);
                }
            }
            else
            {
                cell.SetNeighbor(HexDirection.HexDirection_SW, cells[i - width]);
                if (x < width - 1)
                {
                    cell.SetNeighbor(HexDirection.HexDirection_SE, cells[i - width + 1]);
                }
            }
        }

        // coords text label
        Text label = Instantiate<Text>(cellLabelPrefab);
        label.transform.SetParent(gridCanvas.transform, false);
        label.transform.localPosition = new Vector2(pos.x, pos.z);
        label.text = cell.coordinates.ToStringOnSeparateLines();

        cell.uiRect = label.rectTransform;
        cell.SetElevation(0);
    }

    private void Start()
    {
        hexMesh.Triangulate(cells);
    }

    public void TouchCell(Vector3 hitPos, Color color)
    {
        Vector3 pos = transform.InverseTransformPoint(hitPos);
        HexCoordinates coordinates = HexCoordinates.FromPosition(pos);

        int cellIndex = coordinates.Z * width + coordinates.Z / 2 + coordinates.X;
        HexCell cell = cells[cellIndex];
        cell.color = color;

        hexMesh.Triangulate(cells);
    }

    public HexCell GetCell(Vector3 targetPos)
    {
        Vector3 pos = transform.InverseTransformPoint(targetPos);
        HexCoordinates coordinates = HexCoordinates.FromPosition(pos);

        int cellIndex = coordinates.Z * width + coordinates.Z / 2 + coordinates.X;
        return cells[cellIndex];
    }

    public void Refresh()
    {
        hexMesh.Triangulate(cells);
    }

    public void OnEnable()
    {
        HexMetrics.noiseSource = noiseSource;
    }
}
