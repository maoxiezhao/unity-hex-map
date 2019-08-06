using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexCellMesh : MonoBehaviour
{

    Mesh hexMesh;
    List<Vector3> vertices;     // 顶点信息
    List<int> triangels;        // indices信息
    List<Color> colors;
    MeshCollider meshCollider;

    private void Awake()
    {
        GetComponent<MeshFilter>().mesh = hexMesh = new Mesh();
        meshCollider = gameObject.AddComponent<MeshCollider>();

        hexMesh.name = "HexMesh";

        vertices = new List<Vector3>();
        triangels = new List<int>();
        colors = new List<Color>();
    }

    public void Triangulate(HexCell[] cells)
    {
        hexMesh.Clear();
        vertices.Clear();
        triangels.Clear();
        colors.Clear();

        foreach (HexCell cell in cells)
        {
            Triangulate(cell);
        }

        hexMesh.vertices = vertices.ToArray();
        hexMesh.triangles = triangels.ToArray();
        hexMesh.RecalculateNormals();
        hexMesh.colors = colors.ToArray();

        meshCollider.sharedMesh = hexMesh;
    }

    private void Triangulate(HexCell cell)
    {
        for (HexDirection dir = HexDirection.HexDirection_NE; dir <= HexDirection.HexDirection_NW; dir++)
        {
            Triangulate(dir, cell);
        }
    }
    
    // 为了做颜色混和，将创建cell之间相邻的形状
    // 两个Cell之间为一个矩形
    // 三个cell之间为一个三角形
    //      /\
    //     /  \
    //    v3---v4
    //  / |    | \
    //  \ |    | /
    //   v1 -- v2
    //     \  /
    //      \/
    private void Triangulate(HexDirection dir, HexCell cell)
    {
        Vector3 center = cell.transform.localPosition;
        Vector3 v1 = center + HexMetrics.GetFirstSolidCorner(dir);
        Vector3 v2 = center + HexMetrics.GetSecondSolidCorner(dir);

        // 三角形部分為實體顔色
        AddTriangle(center, v1, v2);
        AddTriangleColor(cell.color);

        if (dir <= HexDirection.HexDirection_SE) {
            if (cell.GetNeighbor(dir) != null)
                TriangulateConnection(dir, cell, v1, v2);
        }
    }

    private void TriangulateConnection(HexDirection dir, HexCell cell, Vector3 v1, Vector3 v2)
    {
        // connection bridge
        HexCell neighbor = cell.GetNeighbor(dir) ?? cell;

        Vector3 bridge = HexMetrics.GetBridge(dir);
        Vector3 v3 = v1 + bridge;
        Vector3 v4 = v2 + bridge;

        // process elevation
        v3.y = v4.y = neighbor.elevation * HexMetrics.elevationStep;

        if (cell.GetHexEdgeType(dir) == HexEdgeType.HexEdgeType_Slope)
        {
            TriangulateEdgeTerraces(v1, v2, v3, v4, cell, neighbor);
        }
        else
        {
            AddQuad(v1, v2, v3, v4);
            AddQuadColor(cell.color, neighbor.color);
        }

        // connection triangle
        HexCell nextNeighbor = cell.GetNeighbor(dir.Next());
        if (nextNeighbor != null && dir <= HexDirection.HexDirection_E)
        {
            Vector3 v5 = v2 + HexMetrics.GetBridge(dir.Next());
            v5.y = nextNeighbor.elevation * HexMetrics.elevationStep;

            if (cell.elevation <= neighbor.elevation)
            {
                if (cell.elevation <= nextNeighbor.elevation) {
                    TriangulateCorner(v2, cell, v4, neighbor, v5, nextNeighbor);
                }
                else {
                    TriangulateCorner(v5, nextNeighbor, v2, cell, v4, neighbor);
                }
            }
            else if (neighbor.elevation <= nextNeighbor.elevation){
                TriangulateCorner(v4, neighbor, v5, nextNeighbor, v2, cell);
            }
            else {
                TriangulateCorner(v5, nextNeighbor, v2, cell, v4, neighbor);
            }
        }
    }

    // process三角连接处的阶梯化,其中B为最低的cell
    //      L___R
    //       \ /
    //        B
    private void TriangulateCorner(
        Vector3 bottom, HexCell bottomCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell)
    {
        HexEdgeType leftEdgeType = bottomCell.GetHexEdgeType(leftCell);
        HexEdgeType rightEdgeType = bottomCell.GetHexEdgeType(rightCell);

        if (leftEdgeType == HexEdgeType.HexEdgeType_Slope)
        {
            if (rightEdgeType == HexEdgeType.HexEdgeType_Slope)
            {
                TriangulateCornerTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
            }
            else if (rightEdgeType == HexEdgeType.HexEdgeType_Flat)
            {
                TriangulateCornerTerraces(left, leftCell, right, rightCell, bottom, bottomCell);
            }
            else
            {
                TriangulateCornerTerracesCliff(bottom, bottomCell, left, leftCell, right, rightCell);
            }
        }
        else if (rightEdgeType == HexEdgeType.HexEdgeType_Slope)
        {
            if (leftEdgeType == HexEdgeType.HexEdgeType_Flat)
            {
                TriangulateCornerTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
            }
            else
            {
                TriangulateCornerTerracesCliffFlip(bottom, bottomCell, left, leftCell, right, rightCell);
            }
        }
        else if (leftCell.GetHexEdgeType(rightCell) == HexEdgeType.HexEdgeType_Slope)
        {
            if (leftCell.elevation < rightCell.elevation)
            {
                TriangulateCornerTerracesCliffFlip(right, rightCell, bottom, bottomCell, left, leftCell);
            }
            else
            {
                TriangulateCornerTerracesCliff(left, leftCell, right, rightCell, bottom, bottomCell);
            }
        }
        else
        {
            AddTriangle(bottom, left, right);
            AddTriangleColor(
                bottomCell.color,
                leftCell.color,
                rightCell.color);
        }
    }

    // 处理bottom与left和right都是slope的情况
    private void TriangulateCornerTerraces(
        Vector3 bottom, HexCell bottomCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell)
    {
        // process begin
        Vector3 v3 = HexMetrics.TerraceLerp(bottom, left, 1);
        Vector3 v4 = HexMetrics.TerraceLerp(bottom, right, 1);
        Color c3 = HexMetrics.TerraceLerp(bottomCell.color, leftCell.color, 1);
        Color c4 = HexMetrics.TerraceLerp(bottomCell.color, rightCell.color, 1);
        AddTriangle(bottom, v3, v4);
        AddTriangleColor(bottomCell.color, c3, c4);

        for (int step = 2; step < HexMetrics.terraceSteps; step++)
        {
            Vector3 v1 = v3;
            Vector3 v2 = v4;
            Color c1 = c3;
            Color c2 = c4;

            v3 = HexMetrics.TerraceLerp(bottom, left, step);
            v4 = HexMetrics.TerraceLerp(bottom, right, step);
            c3 = HexMetrics.TerraceLerp(bottomCell.color, leftCell.color, step);
            c4 = HexMetrics.TerraceLerp(bottomCell.color, rightCell.color, step);

            AddQuad(v1, v2, v3, v4);
            AddQuadColor(c1, c2, c3, c4);
        }

        // process end
        AddQuad(v3, v4, left, right);
        AddQuadColor(c3, c4, leftCell.color, rightCell.color);
    }

    // 处理Cliff的阶梯化(bottom与left是slop）
    private void TriangulateCornerTerracesCliff(
        Vector3 bottom, HexCell bottomCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell)
    {
        // find boundary point
        float b = 1.0f / Mathf.Abs(rightCell.elevation - bottomCell.elevation);
        Vector3 boundary = Vector3.Lerp(bottom, right, b);
        Color boundaryColor = Color.Lerp(bottomCell.color, rightCell.color, b);

        // process bottom boundary triangle
        TriangulateBoundaryTriangle(bottom, bottomCell, left, leftCell, boundary, boundaryColor);

        if (leftCell.GetHexEdgeType(rightCell) == HexEdgeType.HexEdgeType_Slope)
        {
            // process top boundary triangle
            TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
        }
        else
        {
            // process normal triangle
            AddTriangle(left, right, boundary);
            AddTriangleColor(leftCell.color, rightCell.color, boundaryColor);
        }
    }
    // 处理Cliff的阶梯化(bottom与right是slop）
    private void TriangulateCornerTerracesCliffFlip(
        Vector3 bottom, HexCell bottomCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell)
    {
        // find boundary point
        float b = 1.0f / Mathf.Abs(leftCell.elevation - bottomCell.elevation);
        Vector3 boundary = Vector3.Lerp(bottom, left, b);
        Color boundaryColor = Color.Lerp(bottomCell.color, leftCell.color, b);

        // process bottom boundary triangle
        TriangulateBoundaryTriangle(right, rightCell, bottom, bottomCell, boundary, boundaryColor);

        if (leftCell.GetHexEdgeType(rightCell) == HexEdgeType.HexEdgeType_Slope)
        {
            // process top boundary triangle
            TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
        }
        else
        {
            // process normal triangle
            AddTriangle(left, right, boundary);
            AddTriangleColor(leftCell.color, rightCell.color, boundaryColor);
        }
    }

    // 处理Cliff阶梯化时，处理边界三角形
    private void TriangulateBoundaryTriangle(
        Vector3 bottom, HexCell bottomCell,
        Vector3 left, HexCell leftCell,
        Vector3 boundary, Color boundaryColor)
    {
        // process begin
        Vector3 v2 = HexMetrics.TerraceLerp(bottom, left, 1);
        Color c2 = HexMetrics.TerraceLerp(bottomCell.color, leftCell.color, 1);
        AddTriangle(bottom, v2, boundary);
        AddTriangleColor(bottomCell.color, c2, boundaryColor);

        for (int step = 2; step < HexMetrics.terraceSteps; step++)
        {
            Vector3 v1 = v2;
            Color c1 = c2;

            v2 = HexMetrics.TerraceLerp(bottom, left, step);
            c2 = HexMetrics.TerraceLerp(bottomCell.color, leftCell.color, step);

            AddTriangle(v1, v2, boundary);
            AddTriangleColor(c1, c2, boundaryColor);
        }

        // process end
        AddTriangle(v2, left, boundary);
        AddTriangleColor(c2, leftCell.color, boundaryColor);
    }

    // 阶梯化边链接处
    private void TriangulateEdgeTerraces(Vector3 beginLeft, Vector3 beginRight, 
        Vector3 endLeft, Vector3 endRight, HexCell currentCell, HexCell neighborCell)
    {
        // process begin
        Vector3 v3 = HexMetrics.TerraceLerp(beginLeft, endLeft, 1);
        Vector3 v4 = HexMetrics.TerraceLerp(beginRight, endRight, 1);
        Color c2 = HexMetrics.TerraceLerp(currentCell.color, neighborCell.color, 1);

        AddQuad(beginLeft, beginRight, v3, v4);
        AddQuadColor(currentCell.color, c2);

        for (int step = 2; step < HexMetrics.terraceSteps; step++)
        {
            Vector3 v1 = v3;
            Vector3 v2 = v4;
            Color c1 = c2;

            v3 = HexMetrics.TerraceLerp(beginLeft, endLeft, step);
            v4 = HexMetrics.TerraceLerp(beginRight, endRight, step);
            c2 = HexMetrics.TerraceLerp(currentCell.color, neighborCell.color, step);

            AddQuad(v1, v2, v3, v4);
            AddQuadColor(c1, c2);
        }

        // process end
        AddQuad(v3, v4, endLeft, endRight);
        AddQuadColor(c2, neighborCell.color);
    }

    private void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);

        triangels.Add(vertexIndex);
        triangels.Add(vertexIndex + 1);
        triangels.Add(vertexIndex + 2);
    }

    private void AddTriangleColor(Color c1)
    {
        colors.Add(c1);
        colors.Add(c1);
        colors.Add(c1);
    }

    private void AddTriangleColor(Color c1, Color c2, Color c3)
    {
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c3);
    }

    // 填補六邊形之間的空隙
    private void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);
        vertices.Add(v4);

        triangels.Add(vertexIndex);
        triangels.Add(vertexIndex + 2);
        triangels.Add(vertexIndex + 1);
        triangels.Add(vertexIndex + 1);
        triangels.Add(vertexIndex + 2);
        triangels.Add(vertexIndex + 3);
    }

    private void AddQuadColor(Color c1, Color c2)
    {
        colors.Add(c1);
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c2);
    }

    private void AddQuadColor(Color c1, Color c2, Color c3, Color c4)
    {
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c3);
        colors.Add(c4);
    }
}