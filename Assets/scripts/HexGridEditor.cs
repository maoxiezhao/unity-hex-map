using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HexGridEditor : MonoBehaviour {

    public Color[] colors;
    public HexGrid hexGrid;

    private bool applyColor = true;
    private Color currentColor;

    private bool applyElevation = true;
    private int currentElevation;

    private int brushSize = 0;

    private void Awake()
    {
        SelectColor(0);
        currentElevation = 0;
    }

    void Update ()
    {
        if (Input.GetMouseButton(0) &&
            !EventSystem.current.IsPointerOverGameObject()){
            HandleInput();
        }
	}

    void HandleInput()
    {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit)) {
            HexCell cell = hexGrid.GetCell(hit.point);
            EditCells(cell);
        }
    }

    public void SelectColor(int index)
    {
        applyColor = index >= 0;

        if (applyColor) {
            currentColor = colors[index];
        }
    }

    public void SetElevation(float elevation)
    {
        currentElevation = (int)elevation;
    }

    public void SetElevationEnable(bool isEnable)
    {
        applyElevation = isEnable;
    }

    public void SetBrushSize(float size)
    {
        brushSize = (int)size;
    }

    private void EditCells(HexCell centerCell)
    {
        int centerX = centerCell.coordinates.X;
        int centerZ = centerCell.coordinates.Z;

        // 最底部左侧的单元格x坐标为0，每往中间移动，最左侧的x的偏移+1
        for (int r = 0, z = centerZ - brushSize; z <= centerZ; z++, r++)
        {
            for (int x = centerX - r; x <= centerX + brushSize; x++) {
                HexCell cell = hexGrid.GetCell(new HexCoordinates(x, z));
                EditCell(cell);
            }
        }

        for (int r = 0, z = centerZ + brushSize; z > centerZ; z--, r++)
        {
            for (int x = centerX - brushSize; x <= centerX + r; x++){
                HexCell cell = hexGrid.GetCell(new HexCoordinates(x, z));
                EditCell(cell);
            }
        }
    }

    private void EditCell(HexCell cell)
    {
        if (cell == null) {
            return;
        }

        if (applyColor) {
            cell.Color = currentColor;
        }

        if (applyElevation) {
            cell.SetElevation(currentElevation);
        }
    }

    public void ShowUILable(bool visible)
    {
        hexGrid.ShowUILable(visible);
    }
}
