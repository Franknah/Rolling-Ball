using System;
using UnityEngine;

namespace RollingBall
{
    /// <summary>
    /// 调色板条目：把一个字符映射到“格子预制体 + 初始旋转”。
    /// 这样同一张弯管预制体可以用 a/b/c/d 四个字符表示四种朝向。
    /// </summary>
    [Serializable]
    public class CellEntry
    {
        /// <summary>地图里使用的字符，例如 '.'、'-'、'S'。</summary>
        public char symbol = '.';
        /// <summary>该字符对应的格子预制体。</summary>
        public PipeCell prefab;
        /// <summary>摆放时顺时针旋转 90° 的次数（0..3）。</summary>
        public int initialRotation;
    }

    /// <summary>
    /// 关卡数据。地图用字符网格描述，每一格都只是“某个预制体的初始朝向”，
    /// 真正的格子对象由 LevelBuilder 实例化。
    ///
    /// 字符约定：
    ///   .  空地      -  水平直管    |  竖直直管
    ///   a/b/c/d  弯管四种朝向      S  起点      G  终点
    /// </summary>
    [CreateAssetMenu(menuName = "Rolling Ball/Level", fileName = "Level")]
    public class LevelDefinition : ScriptableObject
    {
        public string displayName = "Level";

        /// <summary>地图文本，第一行是最上面一行（y 最大的那一行）。</summary>
        [TextArea(3, 20)] public string[] rows = { ". . .", "S . G" };

        /// <summary>一格的世界尺寸，当前固定为 1。</summary>
        public float cellSize = 1f;

        /// <summary>球的初速度（世界单位/秒）。</summary>
        public float ballSpeed = 3f;

        /// <summary>兜底时间：超过这个秒数还没结束，就判定失败，防止玩家造出无限循环。</summary>
        public float maxTravelTime = 30f;

        /// <summary>字符 -> 预制体 的映射表。</summary>
        public CellEntry[] palette = Array.Empty<CellEntry>();

        /// <summary>地图行数。</summary>
        public int Height => rows != null ? rows.Length : 0;

        /// <summary>地图列数（取第一行按空格拆分后的数量）。</summary>
        public int Width
        {
            get
            {
                if (rows == null || rows.Length == 0) return 0;
                return Split(rows[0]).Length;
            }
        }

        /// <summary>按空格/制表符拆一行。</summary>
        private static string[] Split(string row) =>
            row.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

        /// <summary>取出某一行的所有符号（每个 token 取第一个字符）。</summary>
        public char[] SymbolsIn(int rowIndex)
        {
            if (rows == null || rowIndex < 0 || rowIndex >= rows.Length) return Array.Empty<char>();

            string[] parts = Split(rows[rowIndex]);
            var chars = new char[parts.Length];
            for (int i = 0; i < parts.Length; i++) chars[i] = parts[i][0];
            return chars;
        }

        /// <summary>按字符查调色板，找不到返回 null。</summary>
        public CellEntry FindEntry(char symbol)
        {
            if (palette == null) return null;
            foreach (var entry in palette)
            {
                if (entry != null && entry.symbol == symbol) return entry;
            }
            return null;
        }
    }
}
