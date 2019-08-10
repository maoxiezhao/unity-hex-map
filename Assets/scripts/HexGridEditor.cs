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

    private bool isDrag = false;
    HexDirection dragDirection;
    HexCell previouseCell;

    enum OptionalToggle {
        Ignore, Yes, No
    }
    OptionalToggle riverMode;

    private void Awake()
    {
        SelectColor(0);
        currentElevation = 0;
    }

    void Update ()
    {
        if (Input.GetMouseButton(0) &&
            !EventSystem.current.IsPointerOverGameObject()) {
            HandleInput();
        }
        else {
            previouseCell = null;
        }
	}

    void HandleInput()
    {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit)) {
            HexCell cell = hexGrid.GetCell(hit.point);
            if (previouseCell && previouseCell != cell){
                ValidateDrag(cell);
            }
            else {
                isDrag = false;
            }

            EditCells(cell);
            previouseCell = cell;
        }
        else {
            previouseCell = null;
        }
    }

    private void ValidateDrag(HexCell currentCell)
    {
        for (dragDirection = HexDirection.HexDirection_NE; dragDirection <= HexDirection.HexDirection_NW; dragDirection++)
        {
            if (previouseCell.GetNeighbor(dragDirection) == currentCell)
            {
                isDrag = true;
                return;
            }
        }

        isDrag = false;
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

        if (riverMode == OptionalToggle.No) {
            cell.RemoveRiver();
        }
        else if (isDrag && riverMode == OptionalToggle.Yes)
        {
            HexCell otherCell = cell.GetNeighbor(dragDirection.Opposite());
            if (otherCell != null) {
                otherCell.SetOutgoingRiver(dragDirection);
            }
        }
    }

    public void ShowUILable(bool visible)
    {
        hexGrid.ShowUILable(visible);
    }

    public void SetRiverMode(int mode)
    {
        riverMode = (OptionalToggle)(mode);
    }
}
