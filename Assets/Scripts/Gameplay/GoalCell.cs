using DG.Tweening;
using UnityEngine;

namespace RollingBall
{
    /// <summary>终点格：球到达时把旗子从下方弹上来，作为胜利表现。</summary>
    public class GoalCell : PipeCell
    {
        [SerializeField] private Transform flag;
        /// <summary>旗子上升的高度（世界单位）。</summary>
        [SerializeField] private float raiseHeight = 0.35f;

        public void SetFlag(Transform flagTransform) => flag = flagTransform;

        public void PlayWin()
        {
            if (flag == null) return;

            flag.DOKill();

            // 先瞬移到目标下方，再补间回原位，形成“升起”的效果
            Vector3 target = flag.localPosition;
            flag.localPosition = target + Vector3.down * raiseHeight;

            flag.DOLocalMove(target, 0.45f).SetEase(Ease.OutBack);
            flag.DOPunchRotation(new Vector3(0f, 0f, 12f), 0.4f, 6, 0.7f);
        }
    }
}
