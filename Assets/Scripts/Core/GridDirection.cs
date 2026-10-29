using UnityEngine;

namespace RollingBall
{
    /// <summary>字节顺序: N=1, E=2, S=4, W=8.</summary>
    public static class GridDirection
    {
        // 四个方向
        public static readonly Vector2Int N = new Vector2Int(0, 1);
        public static readonly Vector2Int E = new Vector2Int(1, 0);
        public static readonly Vector2Int S = new Vector2Int(0, -1);
        public static readonly Vector2Int W = new Vector2Int(-1, 0);

        public static readonly Vector2Int[] All = { N, E, S, W };

        public static int ToBit(Vector2Int d)
        {
            if (d == N)
                return 1;
            if (d == E)
                return 2;
            if (d == S)
                return 4;
            if (d == W)
                return 8;
            return 0;
        }

        public static Vector2Int FromBit(int bit)
        {
            switch (bit)
            {
                case 1:
                    return N;
                case 2:
                    return E;
                case 4:
                    return S;
                case 8:
                    return W;
                default:
                    return Vector2Int.zero;
            }
        }

        public static Vector2Int Opposite(Vector2Int d) => new Vector2Int(-d.x, -d.y);

        /// <summary> 通过N步将四位掩码顺时针旋转4步 </summary>
        public static int RotateCW(int mask, int steps)
        {
            steps = ((steps % 4) + 4) % 4;
            for (int i = 0; i < steps; i++)
                mask = ((mask << 1) | (mask >> 3)) & 0xF;
            return mask;
        }

        /// <summary> Z轴方向角 </summary>
        public static float ZAngle(Vector2Int d)
        {
            if (d == E)
                return 0f;
            if (d == N)
                return 90f;
            if (d == W)
                return 180f;
            if (d == S)
                return -90f;
            return 0f;
        }
    }
}
