using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexCellMesh : MonoBehaviour
{

    Mesh hexMesh;
    static List<Vector3> vertices = new List<Vector3>();     // 顶点信息
    static List<int> triangels = new List<int>();        // indices信息
    static List<Color> colors = new List<Color>();
    MeshCollider meshCollider;

    private void Awake()
    {
        GetComponent<MeshFilter>().mesh = hexMesh = new Mesh();
        meshCollider = gameObject.AddComponent<MeshCollider>();

        hexMesh.name = "HexMesh";
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
    //    center
    private void Triangulate(HexDirection dir, HexCell cell)
    {
        if (cell == null) {
            return;
        }

        Vector3 center = cell.Position;
        Vector3 v1 = center + HexMetrics.GetFirstSolidCorner(dir);
        Vector3 v2 = center + HexMetrics.GetSecondSolidCorner(dir);
        EdgeVertices edge = new EdgeVertices(v1, v2);

        TriangulateEdgeFan(center, edge, cell.color);

        if (dir <= HexDirection.HexDirection_SE) {
            if (cell.GetNeighbor(dir) != null)
                TriangulateConnection(dir, cell, edge);
        }
    }

    private void TriangulateConnection(HexDirection dir, HexCell cell, EdgeVertices e1)
    {
        HexCell neighbor = cell.GetNeighbor(dir) ?? cell;

        // connection bridge
        Vector3 bridge = HexMetrics.GetBridge(dir);
        bridge.y = neighbor.Position.y - cell.Position.y;

        EdgeVertices e2 = new EdgeVertices(
            e1.v1 + bridge, e1.v4 + bridge);

        if (cell.GetHexEdgeType(dir) == HexEdgeType.HexEdgeType_Slope)
        {
            TriangulateEdgeTerraces(e1, e2, cell, neighbor);
        }
        else
        {
            TriangulateEdgeStrip(e1, cell.color, e2, neighbor.color);
        }

        // connection triangle
        HexCell nextNeighbor = cell.GetNeighbor(dir.Next());
        if (nextNeighbor != null && dir <= HexDirection.HexDirection_E)
        {
            Vector3 v5 = e1.v4 + HexMetrics.GetBridge(dir.Next());
            v5.y = nextNeighbor.Position.y;

            if (cell.elevation <= neighbor.elevation)
            {
                if (cell.elevation <= nextNeighbor.elevation) {
                    TriangulateCorner(e1.v4, cell, e2.v4, neighbor, v5, nextNeighbor);
                }
                else {
                    TriangulateCorner(v5, nextNeighbor, e1.v4, cell, e2.v4, neighbor);
                }
            }
            else if (neighbor.elevation <= nextNeighbor.elevation){
                TriangulateCorner(e2.v4, neighbor, v5, nextNeighbor, e1.v4, cell);
            }
            else {
                TriangulateCorner(v5, nextNeighbor, e1.v4, cell, e2.v4, neighbor);
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
        Vector3 boundary = Vector3.Lerp(Perturb(bottom), Perturb(right), b);
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
            // 分界点不扰动
            AddUnPerturbedTriangle(Perturb(left), Perturb(right), boundary);
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
        Vector3 boundary = Vector3.Lerp(Perturb(bottom), Perturb(left), b);
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
            // 分界点不扰动
            AddUnPerturbedTriangle(Perturb(left), Perturb(right), boundary);
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
        Vector3 v2 = Perturb(HexMetrics.TerraceLerp(bottom, left, 1));
        Color c2 = HexMetrics.TerraceLerp(bottomCell.color, leftCell.color, 1);

        // 分界点不进行扰动
        AddUnPerturbedTriangle(Perturb(bottom), v2, boundary);
        AddTriangleColor(bottomCell.color, c2, boundaryColor);

        for (int step = 2; step < HexMetrics.terraceSteps; step++)
        {
            Vector3 v1 = v2;
            Color c1 = c2;

            v2 = Perturb(HexMetrics.TerraceLerp(bottom, left, step));
            c2 = HexMetrics.TerraceLerp(bottomCell.color, leftCell.color, step);

            AddUnPerturbedTriangle(v1, v2, boundary);
            AddTriangleColor(c1, c2, boundaryColor);
        }

        // process end
        AddUnPerturbedTriangle(v2, Perturb(left), boundary);
        AddTriangleColor(c2, leftCell.color, boundaryColor);
    }

    // 阶梯化边链接处
    private void TriangulateEdgeTerraces(EdgeVertices begin, EdgeVertices end, HexCell currentCell, HexCell neighborCell)
    {
        // process begin
        EdgeVertices e2 = HexMetrics.TerraceLerp(begin, end, 1);
        Color c2 = HexMetrics.TerraceLerp(currentCell.color, neighborCell.color, 1);

        TriangulateEdgeStrip(begin, currentCell.color, e2, c2);

        for (int step = 2; step < HexMetrics.terraceSteps; step++)
        {
            EdgeVertices e1 = e2;
            Color c1 = c2;
            e2 = HexMetrics.TerraceLerp(begin, end, step);
            c2 = HexMetrics.TerraceLerp(currentCell.color, neighborCell.color, 1);

            TriangulateEdgeStrip(e1, c1, e2, c2);
        }

        // process end
        TriangulateEdgeStrip(e2, c2, end, neighborCell.color);
    }

    private void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(Perturb(v1));
        vertices.Add(Perturb(v2));
        vertices.Add(Perturb(v3));

        triangels.Add(vertexIndex);
        triangels.Add(vertexIndex + 1);
        triangels.Add(vertexIndex + 2);
    }

    private void AddUnPerturbedTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);

        triangels.Add(vertexIndex);
        triangels.Add(vertexIndex + 1);
        triangels.Add(vertexIndex + 2);
    }

    // 填補六邊形之間的空隙
    private void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(Perturb(v1));
        vertices.Add(Perturb(v2));
        vertices.Add(Perturb(v3));
        vertices.Add(Perturb(v4));

        triangels.Add(vertexIndex);
        triangels.Add(vertexIndex + 2);
        triangels.Add(vertexIndex + 1);
        triangels.Add(vertexIndex + 1);
        triangels.Add(vertexIndex + 2);
        triangels.Add(vertexIndex + 3);
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

    private Vector3 Perturb(Vector3 pos)
    {
        Vector3 sample = HexMetrics.SampleNoise(pos);
        pos.x += (sample.x * 2.0f - 1.0f) * HexMetrics.cellPerturbStrength;
        pos.z += (sample.z * 2.0f - 1.0f) * HexMetrics.cellPerturbStrength;
        return pos;
    }

    // 创建从hex center到边缘的3个三个三角形扇面
    private void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, Color color)
    {
        AddTriangle(center, edge.v1, edge.v2);
        AddTriangleColor(color);
        AddTriangle(center, edge.v2, edge.v3);
        AddTriangleColor(color);
        AddTriangle(center, edge.v3, edge.v4);
        AddTriangleColor(color);
    }

    // 创建两个edge之间的3个四边形
    private void TriangulateEdgeStrip(EdgeVertices e1, Color c1, EdgeVertices e2, Color c2)
    {
        AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
        AddQuadColor(c1, c2);
        AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
        AddQuadColor(c1, c2);
        AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
        AddQuadColor(c1, c2);
    }
}