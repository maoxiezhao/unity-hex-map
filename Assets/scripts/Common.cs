﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HexDirection
{
    HexDirection_NE,
    HexDirection_E,
    HexDirection_SE,
    HexDirection_SW,
    HexDirection_W,
    HexDirection_NW,
}

public static class HexDirectionExtensions
{
    public static HexDirection Opposite(this HexDirection dir)
    {
        return (int)dir < 3 ? dir + 3 : dir - 3;
    }

    public static HexDirection Previous(this HexDirection dir)
    {
        return dir == HexDirection.HexDirection_NE ? HexDirection.HexDirection_NW : (dir - 1);
    }

    public static HexDirection Next(this HexDirection dir)
    {
        return dir == HexDirection.HexDirection_NW ? HexDirection.HexDirection_NE : (dir + 1);
    }

    public static HexDirection Previous2(this HexDirection dir)
    {
        dir -= 2;
        return dir >= HexDirection.HexDirection_NE ? dir : (dir + 6);
    }

    public static HexDirection Next2(this HexDirection dir)
    {
        dir += 2;
        return dir <= HexDirection.HexDirection_NW ? dir : (dir - 6);
    }
}

public enum HexEdgeType
{
    HexEdgeType_Flat,
    HexEdgeType_Slope,
    HexEdgeType_Cliff
}

public struct EdgeVertices
{
    public Vector3 v1, v2, v3, v4, v5;
    //public Vector3 v5;  // for river 

    public EdgeVertices(Vector3 corner1, Vector3 corner2, float outerStep = 0.25f)
    {
        v1 = corner1;
        v2 = Vector3.Lerp(corner1, corner2, outerStep);
        v3 = Vector3.Lerp(corner1, corner2, 0.5f);
        v4 = Vector3.Lerp(corner1, corner2, 1 - outerStep);
        v5 = corner2;
    }
}

public static class HexMetrics
{
    public const float outerToInner = 0.8660254f;
    public const float innerToOuter = 1.0f / outerToInner;

    public const float outerRadius = 10.0f;
    public const float innerRadius = outerRadius * outerToInner;
    public const float solidFactor = 0.8f;
    public const float blendFactor = 1f - solidFactor;

    public const float elevationStep = 3f;

    public const int terracesPerSlope = 2;  // 一个斜坡插入两个台阶
    public const int terraceSteps = 2 * terracesPerSlope + 1;   // 一个斜坡处理的步长
    public const float horizontalTerraceStepSize = 1.0f / terraceSteps;
    public const float verticalTerraceStepSize = 1.0f / (terracesPerSlope + 1);

    public static Texture2D noiseSource;
    public const float cellPerturbStrength = 4f;    // 顶点扰动强度
    public const float noiseScale = 0.003f;         // 噪声采样纹理缩放
    public const float elevationPerturbStrength = 1.5f; // 海拔扰动强度

    public const float streamBedElevationOffset = -1.5f; // 河道高度（edgeVertices)中间点偏移量
    public const float riverElevationOffset = -0.5f;   // 河面高度偏移值

    public const int chunkSizeX = 5, chunkSizeZ = 5;

    private static Vector3[] corners = {
        new Vector3(0.0f, 0.0f, outerRadius),
        new Vector3(innerRadius, 0.0f, 0.5f * outerRadius),
        new Vector3(innerRadius, 0.0f, -0.5f * outerRadius),
        new Vector3(0.0f, 0.0f, -outerRadius),
        new Vector3(-innerRadius, 0.0f, -0.5f * outerRadius),
        new Vector3(-innerRadius, 0.0f, 0.5f * outerRadius),
        new Vector3(0.0f, 0.0f, outerRadius),
    };

    public static Vector3 GetFirsetCorner(HexDirection dir)
    {
        return corners[(int)dir];
    }

    public static Vector3 GetSecondCorner(HexDirection dir)
    {
        return corners[(int)dir + 1];
    }

    public static Vector3 GetFirstSolidCorner(HexDirection dir)
    {
        return corners[(int)dir] * solidFactor;
    }

    public static Vector3 GetSecondSolidCorner(HexDirection dir)
    {
        return corners[(int)dir + 1] * solidFactor;
    }

    public static Vector3 GetBridge(HexDirection dir)
    {
        return (corners[(int)dir] + corners[(int)dir + 1]) * blendFactor;
    }

    public static Vector3 GetSolidEdgeMiddle(HexDirection dir)
    {
        return (corners[(int)dir] + corners[(int)dir + 1]) * 0.5f * solidFactor;
    }

    // 台阶插值---------------------------------------------------------
    // 根据step差值计算当前位置
    public static Vector3 TerraceLerp(Vector3 a, Vector3 b, int step)
    {
        Vector3 result = a;
        float h = horizontalTerraceStepSize * step;
        result.x += (b.x - a.x) * h;
        result.z += (b.z - a.z) * h;

        float v = ((step + 1) / 2) * verticalTerraceStepSize;
        result.y += (b.y - a.y) * v;

        return result;
    }

    public static Color TerraceLerp(Color a, Color b, int step)
    {
        return Color.Lerp(a, b, step * HexMetrics.horizontalTerraceStepSize);
    }

    public static HexEdgeType GetEdgeTypeByElevation(int elevation1, int elevation2)
    {
        int delta = Mathf.Abs(elevation2 - elevation1);
        if (delta < 1) return HexEdgeType.HexEdgeType_Flat;
        else if (delta < 2) return HexEdgeType.HexEdgeType_Slope;
        else return HexEdgeType.HexEdgeType_Cliff;
    }

    public static EdgeVertices TerraceLerp(EdgeVertices a, EdgeVertices b, int step)
    {
        EdgeVertices result;
        result.v1 = TerraceLerp(a.v1, b.v1, step);
        result.v2 = TerraceLerp(a.v2, b.v2, step);
        result.v3 = TerraceLerp(a.v3, b.v3, step);
        result.v4 = TerraceLerp(a.v4, b.v4, step);
        result.v5 = TerraceLerp(a.v5, b.v5, step);
        return result;
    }

    //--------------------------------------------------------------------------

    public static Vector4 SampleNoise(Vector3 position)
    {
        return noiseSource.GetPixelBilinear(
            position.x * noiseScale, position.z * noiseScale
        );
    }

    public static Vector3 Perturb(Vector3 pos)
    {
        Vector3 sample = SampleNoise(pos);
        pos.x += (sample.x * 2.0f - 1.0f) * cellPerturbStrength;
        pos.z += (sample.z * 2.0f - 1.0f) * cellPerturbStrength;
        return pos;
    }
}
