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
    public HexCell[] neighbors;


    bool hasIncomingRiver, hasOutgoingRiver;
    HexDirection incomingRiver, outgoingRiver;

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

        // check neighbor elevation to ensure the viability or river
        if (hasIncomingRiver && elevation > GetNeighbor(incomingRiver).elevation) {
            RemoveIncomingRiver();
        }
        if (hasOutgoingRiver && elevation < GetNeighbor(outgoingRiver).elevation) {
            RemoveOutgoingRiver();
        }

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

    void RefreshSelfOnly()
    {
        if (chunk != null) {
            chunk.Refresh();
        }
    }

    //------------------ River ---------------------
    // 定义一个cell有且仅有一个incomingRiver和outgoingRiver
    public bool HasIncomingRiver
    {
        get { return hasIncomingRiver; }
    }
    public bool HasOutgoingRiver
    {
        get { return hasOutgoingRiver; }
    }

    public HexDirection IncomingRiver
    {
        get { return incomingRiver; }
    }
    public HexDirection OutgoingRiver
    {
        get { return outgoingRiver; }
    }

    public bool HasRiver
    {
        get { return HasIncomingRiver || HasOutgoingRiver; }
    }

    public bool HasRiverThroughEdge(HexDirection dir)
    {
        return
            (HasIncomingRiver && incomingRiver == dir) ||
            (HasOutgoingRiver && outgoingRiver == dir);

    }

    public bool HasRiverBeginOrEnd()
    {
        return
            (hasIncomingRiver && !hasOutgoingRiver) ||
            (!hasIncomingRiver && hasOutgoingRiver);
    }

    public void RemoveOutgoingRiver()
    {
        if (!hasOutgoingRiver) {
            return;
        }

        hasOutgoingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(outgoingRiver);
        neighbor.hasIncomingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    public void RemoveIncomingRiver()
    {
        if (!hasIncomingRiver){
            return;
        }

        hasIncomingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(incomingRiver);
        neighbor.hasOutgoingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    public void RemoveRiver()
    {
        RemoveIncomingRiver();
        RemoveOutgoingRiver();
    }

    public void SetOutgoingRiver(HexDirection dir)
    {
        if (hasOutgoingRiver && outgoingRiver == dir) {
            return;
        }

        HexCell neighbor = GetNeighbor(dir);
        if (neighbor == null || elevation < neighbor.elevation) {
            return;
        }

        RemoveOutgoingRiver();
        if (hasIncomingRiver && incomingRiver == dir) {
            RemoveIncomingRiver();
        }

        hasOutgoingRiver = true;
        outgoingRiver = dir;
        RefreshSelfOnly();

        neighbor.RemoveIncomingRiver();
        neighbor.hasIncomingRiver = true;
        neighbor.incomingRiver = dir.Opposite();
        neighbor.RefreshSelfOnly();
    }

    public float GetStreamBedY()
    {
        float offset = HexMetrics.streamBedElevationOffset * HexMetrics.elevationStep;
        return elevation + offset;
    }
    //------------------------------------------------
}
