using UnityEngine;
using UnityEngine.UI;

namespace RollingBall
{
    /// <summary>
    /// 极简 HUD：一行状态文字 + 开始/重开/重置三个按钮。
    /// 控件由编辑器引导脚本创建并塞进来，这里只负责绑定和更新显示。
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        public Text statusText;
        public Button startButton;
        public Button restartButton;
        public Button resetButton;

        private GameManager game;

        /// <summary>绑定按钮。先 RemoveAllListeners，避免重复绑定时触发多次。</summary>
        public void Bind(GameManager manager)
        {
            game = manager;

            if (startButton != null)
            {
                startButton.onClick.RemoveAllListeners();
                startButton.onClick.AddListener(() => game.BeginRun());
            }
            if (restartButton != null)
            {
                restartButton.onClick.RemoveAllListeners();
                restartButton.onClick.AddListener(() => game.Restart());
            }
            if (resetButton != null)
            {
                resetButton.onClick.RemoveAllListeners();
                resetButton.onClick.AddListener(() => game.Restart());
            }
        }

        /// <summary>球已经在跑、或局势已定时，开始按钮不可用。</summary>
        public void SetStartAvailable(bool available)
        {
            if (startButton != null) startButton.interactable = available;
        }

        /// <summary>更新状态文字；失败时变红。</summary>
        public void SetStatus(string message, bool isFailure)
        {
            if (statusText == null) return;

            statusText.text = message;
            statusText.color = isFailure ? new Color(1f, 0.45f, 0.4f) : Color.white;
        }
    }
}
