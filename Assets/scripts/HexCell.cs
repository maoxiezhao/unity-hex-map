using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexCell : MonoBehaviour
{
    public HexCoordinates coordinates;
    public int elevation = int.MinValue;

    public Transform uiTransform;
    public RectTransform uiRect;

    public HexGridChunk chunk;

    public Vector3 Position {
        get {
            return transform.localPosition;
        }
    }

    public Color Color {
        get {
            return color;
        }
        set {
            if (color == value) {
                return;
            }

            color = value;
            Refresh();
        }
    }
    public Color color;

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
        if (elevation == this.elevation) {
            return;
        }

        this.elevation = elevation;

        Vector3 pos = transform.localPosition;
        pos.y = elevation * HexMetrics.elevationStep;

        // perturb
        pos.y += (HexMetrics.SampleNoise(pos).y * 2f - 1f) * HexMetrics.elevationPerturbStrength;
        transform.localPosition = pos;

        Vector3 uiPos = uiRect.localPosition;
        uiPos.z = -pos.y;
        uiRect.localPosition = uiPos;

        Refresh();
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

    void Refresh()
    {
        if (chunk != null) {
            chunk.Refresh();

            for (int i = 0; i < neighbors.Length; i++)
            {
                HexCell neighbor = neighbors[i];
                if (neighbor != null && neighbor.chunk != chunk) {
                    neighbor.chunk.Refresh();
                }
            }
        }
    }
}
