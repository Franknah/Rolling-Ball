using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RollingBall.EditorTools
{
    /// <summary>Drives the real BallController on the real scene prefabs and reports win/lose.</summary>
    public static class RuntimeTraceCheck
    {
        [MenuItem("Rolling Ball/Runtime Trace Check")]
        public static void RunMenu() => RunAll();

        public static void BatchRun()
        {
            EditorApplication.Exit(RunAll() ? 0 : 1);
        }

        public static bool RunAll()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", OpenSceneMode.Single);

            var level = Object.FindFirstObjectByType<LevelBuilder>();
            var ball = Object.FindFirstObjectByType<BallController>();
            if (level == null || ball == null)
            {
                Debug.LogError("[Trace] Missing LevelBuilder or BallController in GameScene.");
                return false;
            }

            level.level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(
                "Assets/Resources/Levels/Level_01.asset"
            );

            bool ok = true;

            // 0) the scene must already contain the level as prefab instances, and Build() must index them
            Transform firstBefore =
                level.gridRoot.childCount > 0 ? level.gridRoot.GetChild(0) : null;
            int nonPrefab = 0;
            for (int i = 0; i < level.gridRoot.childCount; i++)
            {
                GameObject child = level.gridRoot.GetChild(i).gameObject;
                if (PrefabUtility.GetPrefabInstanceStatus(child) != PrefabInstanceStatus.Connected)
                    nonPrefab++;
            }

            level.Build();
            Transform firstAfter =
                level.gridRoot.childCount > 0 ? level.gridRoot.GetChild(0) : null;

            bool kept = ReferenceEquals(firstBefore, firstAfter);
            Debug.Log(
                $"[Trace] scene cells={level.gridRoot.childCount} indexed={level.Cells.Count} start={(level.StartCell != null)} goal={(level.GoalCell != null)} nonPrefab={nonPrefab} keptInstances={kept}"
            );
            if (level.Cells.Count != 21 || level.StartCell == null || level.GoalCell == null)
                ok = false;
            if (nonPrefab != 0)
                ok = false;
            if (!kept)
                ok = false;

            // 1) initial state must dead-end
            var dead = false;
            var won = false;
            System.Action<BallController> onDead = _ => dead = true;
            System.Action<BallController> onWin = _ => won = true;
            ball.DeadEnd += onDead;
            ball.ReachedGoal += onWin;

            ball.level = level;
            ball.Begin(level.StartCell, 3f);
            Drive(ball);
            Debug.Log($"[Trace] initial: moving={ball.IsMoving} dead={dead} won={won}");
            if (!dead)
                ok = false;

            // 2) rotate the elbow at (3,0) once (c -> d) and expect a win
            if (level.TryGetCell(new Vector2Int(3, 0), out PipeCell elbow))
            {
                elbow.SetRotationImmediate(3);
            }
            else
            {
                Debug.LogError("[Trace] Expected elbow at (3,0).");
                ok = false;
            }

            dead = false;
            won = false;
            ball.Begin(level.StartCell, 3f);
            Drive(ball);
            Debug.Log($"[Trace] solved: moving={ball.IsMoving} dead={dead} won={won}");
            if (!won)
                ok = false;

            // 3) the ball must end on the goal cell
            if (level.GoalCell != null)
            {
                float distance = Vector2.Distance(
                    ball.transform.position,
                    level.GoalCell.WorldCenter
                );
                Debug.Log($"[Trace] distance to goal centre = {distance:F3}");
                if (distance > 0.01f)
                    ok = false;
            }

            ball.DeadEnd -= onDead;
            ball.ReachedGoal -= onWin;

            Debug.Log(ok ? "[Trace] PASS" : "[Trace] FAIL");
            return ok;
        }

        private static void Drive(BallController ball)
        {
            const float step = 0.02f;
            for (int i = 0; i < 6000 && ball.IsMoving; i++)
                ball.Tick(step);
        }
    }
}
