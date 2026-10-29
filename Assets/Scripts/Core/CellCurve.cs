using System.Collections.Generic;
using UnityEngine;

namespace RollingBall
{
    /// <summary>
    /// 计算球在“一个格子内部”要走的曲线。纯静态几何，不依赖任何组件，
    /// 所以关卡求解器（编辑器）和运行时球都能复用同一套算法。
    ///
    /// 约定：所有曲线都从一个边的中点进、到另一个边的中点出，
    /// 这样相邻格子的端点天然重合，球跨格时位置连续、不会跳变。
    /// </summary>
    public static class CellCurve
    {
        /// <summary>每段曲线采样多少小段。越大越圆滑，16 段对 1 格足够。</summary>
        private const int Samples = 16;

        /// <summary>
        /// 普通格子：从入口边中点到出口边中点。
        /// - 直行：一条直线，长度 = 1 格；
        /// - 转弯：以格子某个角为圆心、半径半格的 1/4 圆弧，长度 ≈ π/4。
        /// </summary>
        public static List<Vector2> Build(
            Vector2 center,
            Vector2Int inDir,
            Vector2Int outDir,
            float cellSize
        )
        {
            float half = cellSize * 0.5f;

            // 入口在中心的反方向半格处，出口在中心的 outDir 方向半格处
            Vector2 entry = center - (Vector2)inDir * half;
            Vector2 exit = center + (Vector2)outDir * half;

            var pts = new List<Vector2>(Samples + 1);

            if (inDir == outDir)
            {
                // 直行：入口 -> 出口 一条直线
                for (int i = 0; i <= Samples; i++)
                    pts.Add(Vector2.Lerp(entry, exit, (float)i / Samples));
                return pts;
            }

            if (inDir == -outDir)
            {
                // 掉头：现有形状用不到，保险起见退化成一个折线
                pts.Add(entry);
                pts.Add(center);
                pts.Add(exit);
                return pts;
            }

            // 转弯：圆弧圆心正好是“入口方向的反向”和“出口方向”围成的那个格子角
            // 例：从西边进、向北出 -> 圆心在格子左上角，半径半格
            Vector2 arcCenter = center + (-(Vector2)inDir + (Vector2)outDir) * half;

            Vector2 from = entry - arcCenter;
            Vector2 to = exit - arcCenter;

            float a0 = Mathf.Atan2(from.y, from.x);
            float a1 = Mathf.Atan2(to.y, to.x);

            // DeltaAngle 会选最短的那段（正好 90°），避免反着绕 270°
            float delta = Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, a1 * Mathf.Rad2Deg) * Mathf.Deg2Rad;

            for (int i = 0; i <= Samples; i++)
            {
                float a = a0 + delta * ((float)i / Samples);
                pts.Add(arcCenter + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * half);
            }
            return pts;
        }

        /// <summary>起点格：从格子中心出发，走到它唯一开口的那条边中点。</summary>
        public static List<Vector2> BuildStart(Vector2 center, Vector2Int dir, float cellSize)
        {
            float half = cellSize * 0.5f;
            Vector2 exit = center + (Vector2)dir * half;
            var pts = new List<Vector2>(Samples + 1);

            for (int i = 0; i <= Samples; i++)
                pts.Add(Vector2.Lerp(center, exit, (float)i / Samples));
            return pts;
        }

        /// <summary>终点格：从入口边中点滚到格子中心（到中心即判定胜利）。</summary>
        public static List<Vector2> BuildGoal(Vector2 center, Vector2Int inDir, float cellSize)
        {
            float half = cellSize * 0.5f;
            Vector2 entry = center - (Vector2)inDir * half;
            var pts = new List<Vector2>(Samples + 1);

            for (int i = 0; i <= Samples; i++)
                pts.Add(Vector2.Lerp(entry, center, (float)i / Samples));
            return pts;
        }
    }
}
