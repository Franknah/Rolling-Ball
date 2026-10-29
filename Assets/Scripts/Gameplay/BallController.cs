using System;
using System.Collections.Generic;
using UnityEngine;

namespace RollingBall
{
    /// <summary>
    /// 小球控制器：按固定速度沿管道中心线前进
    ///
    /// 运动流程：
    ///   起始格中心 -> 起始格出口边 -> 下一格入口边 -> 下一格曲线 -> ...
    /// 每到一格就向该格询问出口方向，出口无效（撞墙/出界）就停下并抛出 DeadEnd。
    /// </summary>
    public class BallController : MonoBehaviour
    {
        [SerializeField]
        private float defaultSpeed = 3f;

        /// <summary> 最大时间限制 </summary>
        [SerializeField]
        private float maxTravelTime = 30f;

        /// <summary>滚进死路时触发。】
        /// </summary>
        public event Action<BallController> DeadEnd;

        /// <summary>到达终点格中心时触发。</summary>
        public event Action<BallController> ReachedGoal;

        public float Speed { get; private set; }
        public bool IsMoving { get; private set; }

        /// <summary>球当前所在（或最后所在）的格子，旋转交互会用它判断“不能转球脚下的格子”。</summary>
        public PipeCell CurrentCell { get; private set; }

        private LevelBuilder level;

        private Vector2Int inDir; // 进入当前格时的运动方向
        private Vector2Int outDir; // 当前格的离开方向

        // 当前这一格的折线采样点，以及每个点的累计弧长
        private readonly List<Vector2> points = new List<Vector2>();
        private float[] cumulative = Array.Empty<float>();
        private float totalLength; // 当前格曲线总长
        private float traveled; // 已经走过多长
        private float elapsed; // 本次出发累计时间
        private bool moving;

        public void Bind(LevelBuilder builder)
        {
            level = builder;
            if (level != null && level.level != null)
                maxTravelTime = level.level.maxTravelTime;
        }

        /// <summary>从起点格中心出发，沿它唯一的开口方向开始滚动。</summary>
        public void Begin(PipeCell startCell, float speed)
        {
            if (startCell == null || level == null)
            {
                Stop();
                return;
            }

            CurrentCell = startCell;
            Speed = speed > 0f ? speed : defaultSpeed;
            elapsed = 0f;

            Vector2Int dir = SingleOpening(startCell);
            if (dir == Vector2Int.zero)
            {
                Stop();
                return;
            }

            inDir = dir;
            outDir = dir;
            transform.position = startCell.WorldCenter;

            // 起点格的特殊之处：球从中心出发，而不是从某条边进入
            SetPoints(CellCurve.BuildStart(startCell.WorldCenter, dir, level.CellSize));
            traveled = 0f;
            moving = true;
            IsMoving = true;
        }

        /// <summary>把球放到某格中心并停住，不开始移动。</summary>
        public void ParkAt(PipeCell cell)
        {
            Stop();
            CurrentCell = cell;
            if (cell != null) transform.position = cell.WorldCenter;
        }

        public void Stop()
        {
            moving = false;
            IsMoving = false;
        }

        /// <summary>取格子唯一开口的方向；多开口时取第一个（起点格正常只有一个）。</summary>
        private static Vector2Int SingleOpening(PipeCell cell)
        {
            int mask = cell.Connections;
            int count = 0;
            Vector2Int first = Vector2Int.zero;

            for (int i = 0; i < GridDirection.All.Length; i++)
            {
                if ((mask & GridDirection.ToBit(GridDirection.All[i])) == 0)
                    continue;
                count++;
                if (count == 1)
                    first = GridDirection.All[i];
            }
            return count == 1 ? first : (count > 1 ? first : Vector2Int.zero);
        }

        private void Update() => Tick(Time.deltaTime);

        /// <summary>
        /// 推进一帧。公开出来是为了让编辑器的自动校验能用固定步长驱动，得到可复现的结果。
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!moving)
                return;

            float dt = deltaTime;
            elapsed += dt;
            if (elapsed > maxTravelTime)
            {
                Fail();
                return;
            }

            // 这一帧要走的路程
            float step = Speed * dt;

            while (step > 0f && moving)
            {
                float remaining = totalLength - traveled;
                if (remaining <= 0f)
                {
                    AdvanceCell();
                    continue;
                }

                // 还未走完
                if (step < remaining)
                {
                    traveled += step;
                    step = 0f;
                }
                else
                {
                    // 先把这一格走完，位置对齐到曲线终点，再决定下一格
                    step -= remaining;
                    traveled = totalLength;
                    transform.position = Sample(traveled);
                    AdvanceCell();
                }
            }

            if (moving)
                transform.position = Sample(traveled);
        }

        /// <summary>当前格走完后：判断胜利 / 出界 / 进入下一格。</summary>
        private void AdvanceCell()
        {
            if (CurrentCell == null)
            {
                Fail();
                return;
            }

            // 终点格：曲线走完就算到达
            if (CurrentCell.Shape == PipeShape.Goal)
            {
                Stop();
                ReachedGoal?.Invoke(this);
                return;
            }

            Vector2Int next = CurrentCell.GridPos + outDir;

            // 出界、邻居是空地、或邻居没有朝向本格的开口 —— 都算死路
            if (
                !level.IsInside(next)
                || !level.TryGetCell(next, out PipeCell nextCell)
                || !nextCell.AcceptsEntry(outDir)
            )
            {
                Fail();
                return;
            }

            EnterCell(nextCell, outDir);
        }

        /// <summary>进入一个新格子，并向它询问出口、生成该格的曲线。</summary>
        private void EnterCell(PipeCell cell, Vector2Int moveDir)
        {
            CurrentCell = cell;
            inDir = moveDir;

            // 终点格只需要滚到中心
            if (cell.Shape == PipeShape.Goal)
            {
                outDir = moveDir;
                SetPoints(CellCurve.BuildGoal(cell.WorldCenter, moveDir, level.CellSize));
                traveled = 0f;
                return;
            }

            if (!cell.TryGetExit(moveDir, out Vector2Int exit))
            {
                Fail();
                return;
            }

            outDir = exit;
            SetPoints(CellCurve.Build(cell.WorldCenter, moveDir, exit, level.CellSize));
            traveled = 0f;
        }

        private void Fail()
        {
            Stop();
            DeadEnd?.Invoke(this);
        }

        /// <summary>把曲线采样点变成“可沿弧长查询”的结构。</summary>
        private void SetPoints(List<Vector2> pts)
        {
            points.Clear();
            points.AddRange(pts);

            totalLength = 0f;
            cumulative = new float[points.Count];

            for (int i = 1; i < points.Count; i++)
            {
                totalLength += Vector2.Distance(points[i - 1], points[i]);
                cumulative[i] = totalLength;
            }
        }

        /// <summary>按累计弧长取曲线上的一点（保证匀速，转弯不会变快）。</summary>
        private Vector2 Sample(float distance)
        {
            if (points.Count == 0)
                return transform.position;
            if (distance <= 0f)
                return points[0];
            if (distance >= totalLength)
                return points[points.Count - 1];

            for (int i = 1; i < points.Count; i++)
            {
                if (cumulative[i] < distance)
                    continue;

                float segment = cumulative[i] - cumulative[i - 1];
                float t = segment <= 1e-6f ? 0f : (distance - cumulative[i - 1]) / segment;
                return Vector2.Lerp(points[i - 1], points[i], t);
            }
            return points[points.Count - 1];
        }
    }
}