using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexCell : MonoBehaviour
{
    public HexCoordinates coordinates;
    public Color color;
    public int elevation;
    public RectTransform uiRect;

    [SerializeField]
    HexCell[] neighbors;

    public HexCell GetNeighbor(HexDirection dir) {
        return neighbors[(int)dir];
    }

    public void SetNeighbor(HexDirection dir, HexCell cell)
    {
        neighbors[(int)dir] = cell;
        cell.neighbors[(int)HexDirectionExtensions.Opposite(dir)] = this;
    }

    public void SetElevation(int elevation)
    {
        this.elevation = elevation;

        Vector3 pos = transform.localPosition;
        pos.y = elevation * HexMetrics.elevationStep;
        transform.position = pos;

        Vector3 uiPos = uiRect.localPosition;
        uiPos.z = -elevation * HexMetrics.elevationStep;
        uiRect.localPosition = uiPos;
    }

    public HexEdgeType GetHexEdgeType(HexDirection dir)
    {
        return HexMetrics.GetEdgeTypeByElevation(
            elevation, GetNeighbor(dir).elevation
        );
    }

    public HexEdgeType GetHexEdgeType(HexCell other)
    {
        return HexMetrics.GetEdgeTypeByElevation(
            elevation, other.elevation
        );
    }
}
