using UnityEngine;

namespace RollingBall
{
    /// <summary>游戏流程状态。</summary>
    public enum GameState
    {
        Paused, // 球停在起点，等待玩家调整管道
        Playing, // 滚动中
        Won, // 已到达终点
        Failed, // 走进死路/超时
    }

    /// <summary>
    /// 游戏总控：负责待机、开局、胜负判定、重开。
    /// 它本身不关心管道怎么走，只订阅 BallController 的事件。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("场景引用（在 Inspector 中拖入）")]
        public LevelBuilder level;
        public BallController ball;
        public GameHUD hud;

        public GameState State { get; private set; } = GameState.Paused;

        /// <summary>待机中和滚动中都可以旋转管道；结束后不行。</summary>
        public bool CanInteract => State == GameState.Paused || State == GameState.Playing;

        private void Start()
        {
            if (level == null || ball == null || hud == null)
            {
                Debug.LogError("[RollingBall] GameManager 缺少引用：请在 Inspector 中把 Level / Ball / HUD 分别拖到对应字段。", this);
                enabled = false;
                return;
            }

            ball.DeadEnd += OnDeadEnd;
            ball.ReachedGoal += OnGoal;
            hud.Bind(this);

            PrepareRun();
        }

        /// <summary>球归位到起点并停住，把控制权交给玩家。</summary>
        public void PrepareRun()
        {
            State = GameState.Paused;

            if (ball != null)
            {
                ball.Stop();
                if (level != null)
                    ball.ParkAt(level.StartCell);
            }

            if (hud != null)
            {
                hud.SetStatus("调整管道，点击开始", false);
                hud.SetStartAvailable(true);
            }
        }

        /// <summary>开始按钮：球从起点出发。</summary>
        public void BeginRun()
        {
            if (State != GameState.Paused)
                return;
            if (ball == null || level == null || level.StartCell == null)
                return;

            State = GameState.Playing;
            if (hud != null)
            {
                hud.SetStatus("滚动中…", false);
                hud.SetStartAvailable(false);
            }

            ball.Begin(level.StartCell, level.level != null ? level.level.ballSpeed : 3f);
        }

        /// <summary>重置管道朝向，回到待机状态。</summary>
        public void Restart()
        {
            if (level != null)
                level.ResetRotations();
            PrepareRun();
        }

        private void OnDeadEnd(BallController source)
        {
            if (State != GameState.Playing)
                return;

            State = GameState.Failed;
            if (hud != null)
            {
                hud.SetStatus("死路！点击重新开始", true);
                hud.SetStartAvailable(false);
            }
        }

        private void OnGoal(BallController source)
        {
            if (State != GameState.Playing)
                return;

            State = GameState.Won;
            if (level != null && level.GoalCell is GoalCell goal)
                goal.PlayWin();

            if (hud != null)
            {
                hud.SetStatus("到达终点！", false);
                hud.SetStartAvailable(false);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
                Restart();
            if (Input.GetKeyDown(KeyCode.Space))
                BeginRun();
        }
    }
}