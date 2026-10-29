using UnityEngine;
using UnityEngine.EventSystems;

namespace RollingBall
{
    /// <summary>
    /// 鼠标交互：点击弯管顺时针旋转 90°。
    /// 用“屏幕坐标 -> 世界坐标 -> 格子坐标”的换算，不需要给格子加碰撞体。
    /// 两条保护：点 UI 时不算点击地面；球正停着的格子不允许旋转。
    /// </summary>
    public class CellRotator : MonoBehaviour
    {
        [Header("场景引用（在 Inspector 中拖入）")]
        public LevelBuilder level;
        public BallController ball;
        public Camera cam;
        public GameManager game;

        private void Start()
        {
            if (level == null || ball == null || cam == null || game == null)
                Debug.LogError("[RollingBall] CellRotator 缺少引用：请在 Inspector 中把 Level / Ball / Camera / Game 依次拖入。", this);
        }

        private void Update()
        {
            if (level == null || cam == null) return;
            if (game != null && !game.CanInteract) return;      // 胜利/失败后不再接受旋转

            if (!Input.GetMouseButtonDown(0)) return;

            // 鼠标压在 UI 上时不穿透到场景，否则点按钮会顺带转管子
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int pos = level.WorldToCell(world);

            if (!level.TryGetCell(pos, out PipeCell cell)) return;
            if (!cell.Rotatable) return;

            // 球脚下的格子不允许转：否则会破坏“已经决定好的出口”，产生歧义
            if (ball != null && ball.CurrentCell == cell)
            {
                cell.PlayDenied();
                return;
            }

            cell.RotateClockwise();
        }
    }
}