using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct HexCoordinates {

    [SerializeField]
    private int x, z;

    public int X { get { return x; } }
    public int Z { get { return z; } }
    public int Y { get { return -x - z; } }

    public HexCoordinates(int x, int z)
    {
        this.x = x;
        this.z = z;
    }

    public static HexCoordinates FromOffsetCoordinates(int x, int z)
    {
        return new HexCoordinates(x - z/2, z);
    }

    public static HexCoordinates FromPosition(Vector3 pos)
    {
        float x = pos.x / (HexMetrics.innerRadius * 2.0f);
        float y = -x;
        float offset = pos.z / (HexMetrics.outerRadius * 3.0f);

        x -= offset;
        y -= offset;

        int iX = Mathf.RoundToInt(x);
        int iY = Mathf.RoundToInt(y);
        int iZ = Mathf.RoundToInt(-x - y);

        // 当RoundToInt得出的值和不为0，因四舍五入丢弃的值过多
        // 则重新计算差值最大的
        if (iX + iY + iZ != 0)
        {
            float dx = Mathf.Abs(x - iX);
            float dy = Mathf.Abs(y - iY);
            float dz = Mathf.Abs(-x - y - iZ);

            if (dx > dy && dx > dz)
            {
                iX = -iY - iZ;
            }
            else if (dz > dy)
            {
                iZ = -iX - iY;
            }
        }

        return new HexCoordinates(iX, iZ);
    }

    public override string ToString()
    {
        return "(" + X.ToString() + ", " + Y.ToString() +", " + Z.ToString() + ")";
    }

    public string ToStringOnSeparateLines()
    {
        return X.ToString() + "\n" + Y.ToString() + "\n" + Z.ToString();
    }
}
