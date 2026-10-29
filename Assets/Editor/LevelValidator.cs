using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RollingBall.EditorTools
{
    /// <summary>Breadth-first search over every rotatable cell; proves a level is solvable.</summary>
    public static class LevelValidator
    {
        private struct CellModel
        {
            public PipeShape shape;
            public bool rotatable;
            public int initial;
        }

        [MenuItem("Rolling Ball/Validate Levels")]
        public static void ValidateMenu() => ValidateAll();

        public static void ValidateAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:LevelDefinition");
            if (guids.Length == 0)
            {
                Debug.LogWarning("[RollingBall] No LevelDefinition assets found.");
                return;
            }

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
                if (level == null) continue;

                int? clicks = Solve(level);
                if (clicks == null) Debug.LogError($"[RollingBall] {level.displayName}: UNSOLVABLE ({path})");
                else Debug.Log($"[RollingBall] {level.displayName}: solvable, min rotations = {clicks} ({path})");
            }
        }

        public static int? Solve(LevelDefinition level)
        {
            int width = level.Width;
            int height = level.Height;
            if (width == 0 || height == 0) return null;

            var grid = new CellModel[width, height];
            var rotIndex = new int[width, height];
            var rotatablePositions = new List<Vector2Int>();
            var start = new Vector2Int(-1, -1);

            for (int y = 0; y < height; y++)
            {
                int row = height - 1 - y;
                char[] symbols = level.SymbolsIn(row);

                for (int x = 0; x < width && x < symbols.Length; x++)
                {
                    CellEntry entry = level.FindEntry(symbols[x]);
                    if (entry == null || entry.prefab == null) return null;

                    grid[x, y] = new CellModel
                    {
                        shape = entry.prefab.Shape,
                        rotatable = entry.prefab.Rotatable,
                        initial = entry.initialRotation,
                    };
                    rotIndex[x, y] = -1;

                    if (grid[x, y].rotatable)
                    {
                        rotIndex[x, y] = rotatablePositions.Count;
                        rotatablePositions.Add(new Vector2Int(x, y));
                    }
                    if (grid[x, y].shape == PipeShape.Start) start = new Vector2Int(x, y);
                }
            }

            if (start.x < 0) return null;

            int count = rotatablePositions.Count;
            if (count > 10) return null; // 4^10 = 1M states, still tractable

            var initialState = new int[count];
            for (int i = 0; i < count; i++)
                initialState[i] = grid[rotatablePositions[i].x, rotatablePositions[i].y].initial;

            var queue = new Queue<(int[] state, int depth)>();
            var seen = new HashSet<int>();

            queue.Enqueue((initialState, 0));
            seen.Add(Encode(initialState));

            while (queue.Count > 0)
            {
                var (state, depth) = queue.Dequeue();

                if (Trace(grid, rotIndex, state, start, width, height)) return depth;

                for (int i = 0; i < count; i++)
                {
                    var next = (int[])state.Clone();
                    next[i] = (next[i] + 1) % 4;

                    if (seen.Add(Encode(next))) queue.Enqueue((next, depth + 1));
                }
            }
            return null;
        }

        /// <summary>Base-4 encoding of the rotation state, used as a compact set key.</summary>
        private static int Encode(int[] state)
        {
            int key = 0;
            for (int i = 0; i < state.Length; i++) key = key * 4 + state[i];
            return key;
        }

        private static bool Trace(CellModel[,] grid, int[,] rotIndex, int[] state, Vector2Int start, int width, int height)
        {
            int Connections(Vector2Int p)
            {
                CellModel m = grid[p.x, p.y];
                int index = rotIndex[p.x, p.y];
                int steps = index >= 0 ? state[index] : m.initial;
                return GridDirection.RotateCW(m.shape.BaseMask(), steps);
            }

            Vector2Int dir = SingleOpening(Connections(start));
            if (dir == Vector2Int.zero) return false;

            Vector2Int pos = start;
            int guard = 0;

            while (guard++ < 4096)
            {
                Vector2Int next = pos + dir;
                if (next.x < 0 || next.y < 0 || next.x >= width || next.y >= height) return false;

                CellModel nextCell = grid[next.x, next.y];
                if (nextCell.shape == PipeShape.Goal) return true;

                int conn = Connections(next);
                int entryBit = GridDirection.ToBit(GridDirection.Opposite(dir));
                if ((conn & entryBit) == 0) return false;

                int exits = conn & ~entryBit;
                if (exits == 0) return false;

                int preferred = GridDirection.ToBit(dir);
                int chosen = (exits & preferred) != 0 ? preferred : (exits & -exits);
                Vector2Int exit = GridDirection.FromBit(chosen);
                if (exit == Vector2Int.zero) return false;

                pos = next;
                dir = exit;
            }
            return false;
        }

        public static void BatchDebug()
        {
            DebugAll();
            EditorApplication.Exit(0);
        }

        public static void DebugAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:LevelDefinition");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
                if (level == null) { Debug.Log("[RollingBall] DBG null level " + path); continue; }

                Debug.Log($"[RollingBall] DBG {level.name} w={level.Width} h={level.Height} rows={(level.rows == null ? -1 : level.rows.Length)} palette={(level.palette == null ? -1 : level.palette.Length)}");

                for (int row = 0; row < level.Height; row++)
                {
                    char[] symbols = level.SymbolsIn(row);
                    var sb = new System.Text.StringBuilder();
                    foreach (char symbol in symbols)
                    {
                        CellEntry entry = level.FindEntry(symbol);
                        if (entry == null) sb.Append(symbol + ":NOENTRY ");
                        else if (entry.prefab == null) sb.Append(symbol + ":NOPREFAB ");
                        else sb.Append(symbol + ":" + entry.prefab.Shape + "/rot" + entry.initialRotation + " ");
                    }
                    int gridY = level.Height - 1 - row;
                    Debug.Log($"[RollingBall] DBG row{row} (y={gridY}) {sb}");
                }
            }
        }

        private static Vector2Int SingleOpening(int mask)
        {
            Vector2Int found = Vector2Int.zero;
            int count = 0;

            for (int i = 0; i < GridDirection.All.Length; i++)
            {
                if ((mask & GridDirection.ToBit(GridDirection.All[i])) == 0) continue;
                count++;
                if (count == 1) found = GridDirection.All[i];
            }
            return count == 1 ? found : Vector2Int.zero;
        }
    }
}
