using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexCellMesh : MonoBehaviour
{
    [NonSerialized] List<Vector3> vertices ;     // 顶点信息
    [NonSerialized] List<int> triangels;        // indices信息
    [NonSerialized] List<Color> colors;
    [NonSerialized] List<Vector2> uvs;

    Mesh hexMesh;
    MeshCollider meshCollider;

    public bool useCollider;
    public bool useColors;
    public bool useUVs;

    private void Awake()
    {
        GetComponent<MeshFilter>().mesh = hexMesh = new Mesh();
        if (useCollider) { 
            meshCollider = gameObject.AddComponent<MeshCollider>();
        }

        hexMesh.name = "HexMesh";
    }

    public void Clear()
    {
        vertices = ListPool<Vector3>.Get();
        triangels = ListPool<int>.Get();
        if (useColors) {
            colors = ListPool<Color>.Get();
        }
        if (useUVs) {
            uvs = ListPool<Vector2>.Get();
        }

        hexMesh.Clear();
    }

    public void Apply()
    {
        hexMesh.SetVertices(vertices);
        hexMesh.SetTriangles(triangels, 0);

        ListPool<Vector3>.Add(vertices);
        ListPool<int>.Add(triangels);

        if (useColors)
        {
            hexMesh.SetColors(colors);
            ListPool<Color>.Add(colors);
        }

        if (useUVs)
        {
            hexMesh.SetUVs(0, uvs);
            ListPool<Vector2>.Add(uvs);
        }

        hexMesh.RecalculateNormals();

        if (useCollider){
            meshCollider.sharedMesh = hexMesh;
        }
    }

    public void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(HexMetrics.Perturb(v1));
        vertices.Add(HexMetrics.Perturb(v2));
        vertices.Add(HexMetrics.Perturb(v3));

        triangels.Add(vertexIndex);
        triangels.Add(vertexIndex + 1);
        triangels.Add(vertexIndex + 2);
    }

    // 添加非扰动三角形
    public void AddUnPerturbedTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
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
    public void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(HexMetrics.Perturb(v1));
        vertices.Add(HexMetrics.Perturb(v2));
        vertices.Add(HexMetrics.Perturb(v3));
        vertices.Add(HexMetrics.Perturb(v4));

        triangels.Add(vertexIndex);
        triangels.Add(vertexIndex + 2);
        triangels.Add(vertexIndex + 1);
        triangels.Add(vertexIndex + 1);
        triangels.Add(vertexIndex + 2);
        triangels.Add(vertexIndex + 3);
    }

    public void AddTriangleColor(Color c1)
    {
        colors.Add(c1);
        colors.Add(c1);
        colors.Add(c1);
    }

    public void AddTriangleColor(Color c1, Color c2, Color c3)
    {
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c3);
    }

    public void AddQuadColor(Color c1, Color c2)
    {
        colors.Add(c1);
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c2);
    }

    public void AddQuadColor(Color c1, Color c2, Color c3, Color c4)
    {
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c3);
        colors.Add(c4);
    }

    public void AddQuadColor(Color c)
    {
        colors.Add(c);
        colors.Add(c);
        colors.Add(c);
        colors.Add(c);
    }

    public void AddTriangleUV(Vector2 uv1, Vector2 uv2, Vector2 uv3)
    {
        uvs.Add(uv1);
        uvs.Add(uv2);
        uvs.Add(uv3);
    }

    public void AddQuadUV(Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4)
    {
        uvs.Add(uv1);
        uvs.Add(uv2);
        uvs.Add(uv3);
        uvs.Add(uv4);
    }

    public void AddQuadUV(float uMin, float uMax, float vMin, float vMax)
    {
        uvs.Add(new Vector2(uMin, vMin));
        uvs.Add(new Vector2(uMax, vMin));
        uvs.Add(new Vector2(uMin, vMax));
        uvs.Add(new Vector2(uMax, vMax));
    }
}