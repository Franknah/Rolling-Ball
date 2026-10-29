using System.Collections.Generic;
using UnityEngine;

namespace RollingBall
{
    /// <summary>
    /// 关卡创建器
    /// 把关卡数据变成场景里的格子，并提供“坐标 <-> 世界位置”的查询。
    ///
    /// 两种工作方式：
    /// - 编辑器里场景已经摆好预制体实例（由 ProjectBootstrap 生成）：直接索引它们，不重新创建；
    /// - 场景里没有格子（例如运行时换了关卡）：按 ASCII 地图实例化预制体。
    /// 这样既能所见即所得，又能保证运行时所有方格都来自预制体。
    /// </summary>
    public class LevelBuilder : MonoBehaviour
    {
        public LevelDefinition level;

        /// <summary>所有格子实例的父节点。</summary>
        public Transform gridRoot;

        /// <summary>格子坐标 -> 格子组件。</summary>
        private readonly Dictionary<Vector2Int, PipeCell> cells =
            new Dictionary<Vector2Int, PipeCell>();

        public PipeCell StartCell { get; private set; }
        public PipeCell GoalCell { get; private set; }

        /// <summary>地图尺寸（格）。</summary>
        public Vector2Int Size { get; private set; }
        public IReadOnlyDictionary<Vector2Int, PipeCell> Cells => cells;
        public float CellSize => level != null ? level.cellSize : 1f;

        // 场景加载时建立索引，GameManager 的 Start 晚于它，可以直接使用
        private void Awake() => Build();

        /// <summary>格子坐标 -> 世界坐标（格子中心）。</summary>
        public Vector2 CellToWorld(Vector2Int p)
        {
            float s = CellSize;
            return new Vector2(p.x * s, p.y * s);
        }

        /// <summary>世界坐标 -> 格子坐标。用 Round 而不是 Floor，因为格子中心在整数坐标上。</summary>
        public Vector2Int WorldToCell(Vector3 world)
        {
            float s = CellSize;
            return new Vector2Int(Mathf.RoundToInt(world.x / s), Mathf.RoundToInt(world.y / s));
        }

        /// <summary>坐标是否在地图范围内。</summary>
        public bool IsInside(Vector2Int p) => p.x >= 0 && p.y >= 0 && p.x < Size.x && p.y < Size.y;

        public bool TryGetCell(Vector2Int p, out PipeCell cell) => cells.TryGetValue(p, out cell);

        /// <summary>
        /// 建立关卡。优先索引场景里已有的预制体；没有的话才按地图实例化。
        /// </summary>
        public void Build()
        {
            if (level == null)
            {
                Debug.LogError("[RollingBall] LevelBuilder: level is not assigned.");
                return;
            }
            if (gridRoot == null)
                gridRoot = transform;

            Size = new Vector2Int(level.Width, level.Height);

            if (TryIndexExisting())
                return;

            Clear();
            InstantiateFromLevel();
        }

        /// <summary>
        /// 尝试把 gridRoot 下已经摆好的预制体实例登记成格子。
        /// 返回 false 表示“看起来不是有效关卡”，调用方就退回按地图生成。
        /// </summary>
        private bool TryIndexExisting()
        {
            if (gridRoot == null || gridRoot.childCount == 0)
                return false;

            var found = new Dictionary<Vector2Int, PipeCell>();
            PipeCell start = null;
            PipeCell goal = null;

            for (int i = 0; i < gridRoot.childCount; i++)
            {
                PipeCell cell = gridRoot.GetChild(i).GetComponent<PipeCell>();
                if (cell == null)
                    return false; // 混进了非格子物体

                // 坐标由世界位置反推，避免依赖未序列化的字段
                Vector2Int pos = WorldToCell(cell.transform.position);
                if (!IsInside(pos) || found.ContainsKey(pos))
                    return false;

                cell.SetGridPos(pos);
                found[pos] = cell;

                if (cell.Shape == PipeShape.Start)
                    start = cell;
                if (cell.Shape == PipeShape.Goal)
                    goal = cell;
            }

            if (start == null || goal == null)
                return false;

            cells.Clear();
            foreach (KeyValuePair<Vector2Int, PipeCell> pair in found)
                cells[pair.Key] = pair.Value;
            StartCell = start;
            GoalCell = goal;
            return true;
        }

        /// <summary>按 ASCII 地图实例化预制体（运行时换关卡时用）。</summary>
        private void InstantiateFromLevel()
        {
            int height = level.Height;
            int width = level.Width;

            for (int row = 0; row < height; row++)
            {
                char[] symbols = level.SymbolsIn(row);

                // 文本第一行是地图最上面，所以网格 y 要反过来算
                int gridY = height - 1 - row;

                for (int x = 0; x < width && x < symbols.Length; x++)
                {
                    char symbol = symbols[x];
                    CellEntry entry = level.FindEntry(symbol);
                    if (entry == null || entry.prefab == null)
                    {
                        Debug.LogError(
                            $"[RollingBall] Unknown cell symbol '{symbol}' at ({x},{gridY})."
                        );
                        Clear();
                        return;
                    }

                    var pos = new Vector2Int(x, gridY);
                    PipeCell cell = Instantiate(
                        entry.prefab,
                        CellToWorld(pos),
                        Quaternion.identity,
                        gridRoot
                    );
                    cell.name = $"{symbol}_{x}_{gridY}";
                    cell.SetGridPos(pos);
                    cell.SetRotationImmediate(entry.initialRotation);
                    cells[pos] = cell;

                    if (cell.Shape == PipeShape.Start)
                        StartCell = cell;
                    if (cell.Shape == PipeShape.Goal)
                        GoalCell = cell;
                }
            }

            if (StartCell == null)
                Debug.LogError("[RollingBall] Level has no Start cell.");
            if (GoalCell == null)
                Debug.LogError("[RollingBall] Level has no Goal cell.");
        }

        /// <summary>销毁所有格子并清空索引。</summary>
        public void Clear()
        {
            cells.Clear();
            StartCell = null;
            GoalCell = null;
            if (gridRoot == null)
                return;

            for (int i = gridRoot.childCount - 1; i >= 0; i--)
            {
                GameObject child = gridRoot.GetChild(i).gameObject;
                // 编辑模式必须用 DestroyImmediate，运行时用 Destroy
                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }
        }

        /// <summary>把所有格子恢复到关卡设定的初始朝向（“重新开始”用）。</summary>
        public void ResetRotations()
        {
            if (level == null)
                return;

            int height = level.Height;
            for (int row = 0; row < height; row++)
            {
                char[] symbols = level.SymbolsIn(row);
                int gridY = height - 1 - row;

                for (int x = 0; x < symbols.Length; x++)
                {
                    CellEntry entry = level.FindEntry(symbols[x]);
                    if (
                        entry != null
                        && cells.TryGetValue(new Vector2Int(x, gridY), out PipeCell cell)
                    )
                        cell.SetRotationImmediate(entry.initialRotation);
                }
            }
        }
    }
}
