using UnityEngine;

namespace RollingBall
{
    /// <summary>
    /// 正交相机自动取景：根据关卡宽高算出正交尺寸并把镜头对准网格中心，
    /// 这样换一张更大的地图也不用手动调相机。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class GridCamera : MonoBehaviour
    {
        public LevelBuilder level;
        /// <summary>四周留白（世界单位）。</summary>
        public float padding = 0.8f;
        public Color backgroundColor = new Color(0.12f, 0.14f, 0.18f, 1f);

        private void Start() => Fit();

        public void Fit()
        {
            if (level == null) level = FindObjectOfType<LevelBuilder>();

            var cam = GetComponent<Camera>();
            cam.orthographic = true;
            cam.backgroundColor = backgroundColor;

            if (level == null || level.Size == Vector2Int.zero) return;

            float width = level.Size.x * level.CellSize;
            float height = level.Size.y * level.CellSize;

            // 正交尺寸表示“半高”，所以要同时满足高度和“宽度 / 屏幕宽高比”
            float aspect = cam.aspect > 0f ? cam.aspect : 16f / 9f;
            float halfHeight = Mathf.Max(height * 0.5f, width * 0.5f / aspect);

            cam.orthographicSize = halfHeight + padding;

            // 网格左下角在 (0,0)，中心是 (w-1)/2, (h-1)/2
            transform.position = new Vector3(
                (level.Size.x - 1) * level.CellSize * 0.5f,
                (level.Size.y - 1) * level.CellSize * 0.5f,
                -10f);
        }
    }
}
