using DG.Tweening;
using UnityEngine;

namespace RollingBall
{
    /// <summary>
    /// 单个方格。挂在每个格子预制体的根节点上，负责：
    /// 1. 根据“形状 + 旋转步数”算出当前开口；
    /// 2. 告诉球“从某个方向进来后，应该从哪个方向出去”；
    /// 3. 被点击时旋转 90°（带 DOTween 动画）。
    /// 注意：格子本身不做物理碰撞，球是按几何路径走的，所以不需要 Collider。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PipeCell : MonoBehaviour
    {
        [SerializeField]
        private PipeShape shape = PipeShape.Blank;

        [SerializeField]
        private bool rotatable; // 是否可以旋转

        [SerializeField, Range(0, 3)]
        private int rotationSteps; // 顺时针 90° 的次数，0..3

        [SerializeField]
        private Transform pipeVisual; // 管道贴图子物体（跟着格子一起转）

        private bool rotating; // 旋转动画进行中，用来防连点

        public PipeShape Shape => shape;
        public bool Rotatable => rotatable;
        public int RotationSteps => rotationSteps;

        /// <summary>格子坐标，由 LevelBuilder 在生成/索引时写入。</summary>
        public Vector2Int GridPos { get; private set; }

        /// <summary>格子中心的世界坐标。</summary>
        public Vector2 WorldCenter => transform.position;

        /// <summary>
        /// 当前实际开口掩码 = 基础开口顺时针旋转 rotationSteps 次。
        /// 每次询问都现算，保证旋转后立即生效。
        /// </summary>
        public int Connections => GridDirection.RotateCW(shape.BaseMask(), rotationSteps);

        public void Configure(PipeShape newShape, bool canRotate, int steps)
        {
            shape = newShape;
            rotatable = canRotate;
            SetRotationImmediate(steps);
        }

        public void SetPipeVisual(Transform visual) => pipeVisual = visual;

        public void SetGridPos(Vector2Int pos) => GridPos = pos;

        /// <summary>
        /// 球沿着 moveDir 方向进入本格时，本格是否收得下。
        /// 球是从 moveDir 的反方向那条边进来的，所以要看 Opposite(moveDir) 这一位。
        /// </summary>
        public bool AcceptsEntry(Vector2Int moveDir)
        {
            int bit = GridDirection.ToBit(GridDirection.Opposite(moveDir));
            return (Connections & bit) != 0;
        }

        /// <summary>
        /// 给定进入方向，求出离开方向。
        /// 步骤：先确认入口那一边有开口；再在所有开口里去掉入口这一个；
        /// 如果剩下多个（十字/丁字），优先直行，否则取剩下的唯一一个。
        /// </summary>
        public bool TryGetExit(Vector2Int moveDir, out Vector2Int exitDir)
        {
            exitDir = Vector2Int.zero;

            int entryBit = GridDirection.ToBit(GridDirection.Opposite(moveDir));
            int conn = Connections;
            if ((conn & entryBit) == 0)
                return false; // 入口没开口：撞墙

            int exits = conn & ~entryBit; // 去掉入口方向后的其余开口
            if (exits == 0)
                return false; // 只有入口：死路

            int preferred = GridDirection.ToBit(moveDir); // 直行方向
            int chosen = (exits & preferred) != 0 ? preferred : (exits & -exits); // 否则取最低位

            exitDir = GridDirection.FromBit(chosen);
            return exitDir != Vector2Int.zero;
        }

        /// <summary>直接设置旋转（不做动画），用于关卡初始化/重置。</summary>
        public void SetRotationImmediate(int steps)
        {
            rotationSteps = ((steps % 4) + 4) % 4;
            // Unity 里顺时针是负角度，所以乘 -90
            transform.localRotation = Quaternion.Euler(0f, 0f, -90f * rotationSteps);
        }

        /// <summary>
        /// 顺时针转 90°。
        /// 逻辑（rotationSteps）立即更新，球下一次判断就用新朝向；
        /// 视觉动画只是表现，二者短暂不一致不影响判定。
        /// </summary>
        public bool RotateClockwise()
        {
            if (!rotatable || rotating)
                return false;

            rotationSteps = (rotationSteps + 1) % 4;
            rotating = true;

            transform.DOKill();
            transform
                .DOLocalRotate(new Vector3(0f, 0f, -90f), 0.18f, RotateMode.LocalAxisAdd)
                .OnComplete(() => rotating = false);
            return true;
        }

        /// <summary>球正停在本格里时点击它：拒绝旋转，只抖一下作为反馈。</summary>
        public void PlayDenied()
        {
            transform.DOKill();
            transform.DOPunchRotation(new Vector3(0f, 0f, 7f), 0.25f, 8, 0.6f);
        }
    }
}