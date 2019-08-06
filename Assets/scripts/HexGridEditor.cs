using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HexGridEditor : MonoBehaviour {

    public Color[] colors;
    public HexGrid hexGrid;

    private Color currentColor;
    private int currentElevation;

    private void Awake()
    {
        SelectColor(0);
        currentElevation = 0;
    }

    void Update ()
    {
        if (Input.GetMouseButton(0)){
            HandleInput();
        }
	}

    void HandleInput()
    {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit)) {
            HexCell cell = hexGrid.GetCell(hit.point);
            EditCell(cell);
        }
    }

    public void SelectColor(int index)
    {
        currentColor = colors[index];
    }

    public void SetElevation(float elevation)
    {
        currentElevation = (int)elevation;
    }

    private void EditCell(HexCell cell)
    {
        cell.color = currentColor;
        cell.SetElevation(currentElevation);

        hexGrid.Refresh();
    }
}
