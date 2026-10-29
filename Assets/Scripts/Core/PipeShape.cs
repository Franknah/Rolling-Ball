namespace RollingBall
{
    /// <summary>格子的管道类型。</summary>
    public enum PipeShape
    {
        /// <summary>空地：没有开口，球进去就是死路。</summary>
        Blank = 0,
        /// <summary>直管：默认竖直（开口 N+S）。</summary>
        Straight = 1,
        /// <summary>弯管：默认连通右上（开口 N+E），可以旋转。</summary>
        Elbow = 2,
        /// <summary>起点：只有一个开口，球从格子中心出发。</summary>
        Start = 3,
        /// <summary>终点：四面都接收，球滚到中心即胜利。</summary>
        Goal = 4,
    }

    public static class PipeShapeExtensions
    {
        /// <summary>
        /// 旋转 0 次（未旋转）时的开口掩码。
        /// 注意：这只是“基础形状”，实际开口 = GridDirection.RotateCW(BaseMask, rotationSteps)。
        /// 因此起始旋转不同 = 同一张预制体摆出不同朝向，关卡里用 a/b/c/d 四个字符合并复用同一预制体。
        /// </summary>
        public static int BaseMask(this PipeShape shape)
        {
            switch (shape)
            {
                case PipeShape.Straight: return 1 | 4;            // N + S
                case PipeShape.Elbow: return 1 | 2;               // N + E
                case PipeShape.Start: return 1;                   // N（之后靠旋转指到实际出口）
                case PipeShape.Goal: return 1 | 2 | 4 | 8;        // 四个方向都接受
                default: return 0;                                // Blank
            }
        }
    }
}
