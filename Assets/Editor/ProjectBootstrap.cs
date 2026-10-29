using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RollingBall.EditorTools
{
    /// <summary>One-click project setup: art -> prefabs -> levels -> scene.</summary>
    public static class ProjectBootstrap
    {
        private const string CellDir = "Assets/Prefabs/Cells";
        private const string LevelDir = "Assets/Resources/Levels";
        private const string ScenePath = "Assets/Scenes/GameScene.unity";

        [MenuItem("Rolling Ball/Bootstrap All")]
        public static void BootstrapAll()
        {
            EnsureFolders();
            PlaceholderArt.GenerateAll();
            AssetDatabase.Refresh();

            CreatePrefabs();
            CreateLevels();
            BuildScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            LevelValidator.ValidateAll();
            Debug.Log("[RollingBall] Bootstrap finished.");
        }

        public static void BatchAll()
        {
            try
            {
                BootstrapAll();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("Rolling Ball/Rebuild Scene Only")]
        public static void RebuildSceneOnly()
        {
            EnsureFolders();
            BuildScene();
            AssetDatabase.SaveAssets();
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory(PlaceholderArt.Dir);
            Directory.CreateDirectory("Assets/Prefabs");
            Directory.CreateDirectory(CellDir);
            Directory.CreateDirectory(LevelDir);
            AssetDatabase.Refresh();
        }

        private static Sprite LoadSprite(string name)
        {
            string path = PlaceholderArt.Dir + "/" + name + ".png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) Debug.LogError("[RollingBall] Missing sprite: " + path);
            return sprite;
        }

        private static PipeCell CreateCell(string name, PipeShape shape, bool rotatable,
            string cellSprite, string pipeSprite, bool goal)
        {
            var root = new GameObject(name);

            var body = root.AddComponent<SpriteRenderer>();
            body.sprite = LoadSprite(cellSprite);
            body.sortingOrder = 0;

            Transform pipeTransform = null;
            if (!string.IsNullOrEmpty(pipeSprite))
            {
                var pipe = new GameObject("Pipe");
                pipe.transform.SetParent(root.transform, false);
                var pipeRenderer = pipe.AddComponent<SpriteRenderer>();
                pipeRenderer.sprite = LoadSprite(pipeSprite);
                pipeRenderer.sortingOrder = 1;
                pipeTransform = pipe.transform;
            }

            PipeCell cell;
            if (goal)
            {
                var goalCell = root.AddComponent<GoalCell>();

                var flag = new GameObject("Flag");
                flag.transform.SetParent(root.transform, false);
                flag.transform.localPosition = Vector3.zero;
                var flagRenderer = flag.AddComponent<SpriteRenderer>();
                flagRenderer.sprite = LoadSprite("flag");
                flagRenderer.sortingOrder = 2;

                goalCell.SetFlag(flag.transform);
                cell = goalCell;
            }
            else
            {
                cell = root.AddComponent<PipeCell>();
            }

            cell.Configure(shape, rotatable, 0);
            cell.SetPipeVisual(pipeTransform);

            string path = CellDir + "/" + name + ".prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);

            return prefab.GetComponent<PipeCell>();
        }

        private static void CreatePrefabs()
        {
            Directory.CreateDirectory(CellDir);

            CreateCell("Cell_Blank", PipeShape.Blank, false, "cell_bg", null, false);
            CreateCell("Cell_Straight", PipeShape.Straight, true, "cell_bg", "pipe_straight", false);
            CreateCell("Cell_Elbow", PipeShape.Elbow, true, "cell_bg", "pipe_elbow", false);
            CreateCell("Cell_Start", PipeShape.Start, false, "cell_bg", "pipe_straight", false);
            CreateCell("Cell_Goal", PipeShape.Goal, false, "cell_bg", null, true);

            var ball = new GameObject("Ball");
            var renderer = ball.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadSprite("ball");
            renderer.sortingOrder = 5;
            ball.AddComponent<BallController>();

            PrefabUtility.SaveAsPrefabAsset(ball, "Assets/Prefabs/Ball.prefab");
            Object.DestroyImmediate(ball);

            AssetDatabase.SaveAssets();
            Debug.Log("[RollingBall] Cell prefabs created.");
        }

        private static void CreateLevels()
        {
            Directory.CreateDirectory(LevelDir);

            PipeCell blank = LoadPrefabComponent("Cell_Blank");
            PipeCell straight = LoadPrefabComponent("Cell_Straight");
            PipeCell elbow = LoadPrefabComponent("Cell_Elbow");
            PipeCell start = LoadPrefabComponent("Cell_Start");
            PipeCell goal = LoadPrefabComponent("Cell_Goal");

            if (blank == null || straight == null || elbow == null || start == null || goal == null)
            {
                Debug.LogError("[RollingBall] Cannot create levels: cell prefabs missing.");
                return;
            }

            CreateLevel("Level_01", "Level 01 - 入门", new[]
            {
                ". . . b - - G",
                ". . . | . . .",
                "S - - c . . .",
            }, blank, straight, elbow, start, goal);

            CreateLevel("Level_02", "Level 02 - 两处转弯", new[]
            {
                ". . . . a - G",
                ". . . . | . .",
                ". . b - d . .",
                ". . | . . . .",
                "S - c . . . .",
            }, blank, straight, elbow, start, goal);

            CreateLevel("Level_03", "Level 03 - 三处转弯", new[]
            {
                ". . . . a - G",
                ". . . . | . .",
                ". . a - d . .",
                ". . | . . . .",
                "S - c . . . .",
            }, blank, straight, elbow, start, goal);

            AssetDatabase.SaveAssets();
            Debug.Log("[RollingBall] Levels created.");
        }

        private static PipeCell LoadPrefabComponent(string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CellDir + "/" + name + ".prefab");
            return prefab != null ? prefab.GetComponent<PipeCell>() : null;
        }

        private static void CreateLevel(string fileName, string displayName, string[] rows,
            PipeCell blank, PipeCell straight, PipeCell elbow, PipeCell start, PipeCell goal)
        {
            string path = LevelDir + "/" + fileName + ".asset";
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
            if (level == null)
            {
                level = ScriptableObject.CreateInstance<LevelDefinition>();
                AssetDatabase.CreateAsset(level, path);
            }

            level.displayName = displayName;
            level.rows = rows;
            level.cellSize = 1f;
            level.ballSpeed = 3f;
            level.maxTravelTime = 30f;
            level.palette = new[]
            {
                new CellEntry { symbol = '.', prefab = blank, initialRotation = 0 },
                new CellEntry { symbol = '-', prefab = straight, initialRotation = 1 },
                new CellEntry { symbol = '|', prefab = straight, initialRotation = 0 },
                new CellEntry { symbol = 'a', prefab = elbow, initialRotation = 0 },
                new CellEntry { symbol = 'b', prefab = elbow, initialRotation = 1 },
                new CellEntry { symbol = 'c', prefab = elbow, initialRotation = 2 },
                new CellEntry { symbol = 'd', prefab = elbow, initialRotation = 3 },
                new CellEntry { symbol = 'S', prefab = start, initialRotation = 1 },
                new CellEntry { symbol = 'G', prefab = goal, initialRotation = 0 },
            };

            EditorUtility.SetDirty(level);
        }

        private static void BuildScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            foreach (string name in new[] { "Level", "Game", "Ball", "HUD", "EventSystem" })
            {
                var existing = GameObject.Find(name);
                if (existing != null) Object.DestroyImmediate(existing);
            }

            Camera cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.position = new Vector3(0f, 0f, -10f);

            var levelGo = new GameObject("Level");
            var builder = levelGo.AddComponent<LevelBuilder>();
            var cellsGo = new GameObject("Cells");
            cellsGo.transform.SetParent(levelGo.transform, false);
            builder.gridRoot = cellsGo.transform;
            builder.level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(LevelDir + "/Level_01.asset");
            PopulateCells(builder);

            var gameGo = new GameObject("Game");
            var manager = gameGo.AddComponent<GameManager>();
            var rotator = gameGo.AddComponent<CellRotator>();

            var ballPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ball.prefab");
            GameObject ballGo = null;
            BallController ball = null;
            if (ballPrefab != null)
            {
                ballGo = (GameObject)PrefabUtility.InstantiatePrefab(ballPrefab);
                ballGo.name = "Ball";
                ball = ballGo.GetComponent<BallController>();
            }

            var canvasGo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var hud = canvasGo.AddComponent<GameHUD>();

            Text title = MakeText(canvasGo.transform, "Title", "Rolling Ball", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(32f, -28f), new Vector2(520f, 60f), 40, TextAnchor.UpperLeft);

            Text status = MakeText(canvasGo.transform, "Status", "调整管道，点击开始", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -32f), new Vector2(700f, 56f), 32, TextAnchor.UpperCenter);

            Button start = MakeButton(canvasGo.transform, "StartButton", "开始", new Vector2(-350f, 48f));
            Button restart = MakeButton(canvasGo.transform, "RestartButton", "重新开始 (R)", new Vector2(-190f, 48f));
            Button reset = MakeButton(canvasGo.transform, "ResetButton", "重置旋转", new Vector2(-30f, 48f));

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            manager.level = builder;
            manager.ball = ball;
            manager.hud = hud;

            if (ball != null) ball.level = builder;

            rotator.level = builder;
            rotator.ball = ball;
            rotator.game = manager;
            rotator.cam = cam;

            var gridCamera = cam.GetComponent<GridCamera>();
            if (gridCamera == null) gridCamera = cam.gameObject.AddComponent<GridCamera>();
            gridCamera.level = builder;

            hud.statusText = status;
            hud.startButton = start;
            hud.restartButton = restart;
            hud.resetButton = reset;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[RollingBall] GameScene built.");
        }

        /// <summary>Places real prefab instances in the scene so the level is visible before Play.</summary>
        private static void PopulateCells(LevelBuilder builder)
        {
            if (builder == null || builder.level == null || builder.gridRoot == null) return;

            LevelDefinition level = builder.level;
            Transform root = builder.gridRoot;

            for (int i = root.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.GetChild(i).gameObject);

            int height = level.Height;
            for (int row = 0; row < height; row++)
            {
                char[] symbols = level.SymbolsIn(row);
                int gridY = height - 1 - row;

                for (int x = 0; x < symbols.Length; x++)
                {
                    CellEntry entry = level.FindEntry(symbols[x]);
                    if (entry == null || entry.prefab == null) continue;

                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(entry.prefab.gameObject, root);
                    var cell = instance.GetComponent<PipeCell>();
                    instance.name = symbols[x] + "_" + x + "_" + gridY;
                    instance.transform.position = new Vector3(x * level.cellSize, gridY * level.cellSize, 0f);
                    cell.SetRotationImmediate(entry.initialRotation);
                    cell.SetGridPos(new Vector2Int(x, gridY));
                }
            }
        }

        private static Font GetFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 24);
            return font;
        }

        private static Text MakeText(Transform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size,
            int fontSize, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var label = go.GetComponent<Text>();
            label.text = text;
            label.font = GetFont();
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = Color.white;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        private static Button MakeButton(Transform parent, string name, string caption, Vector2 anchoredPosition)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(150f, 56f);

            var image = go.GetComponent<Image>();
            image.color = new Color(0.22f, 0.28f, 0.38f, 0.95f);

            var label = MakeText(go.transform, "Label", caption, Vector2.zero, Vector2.one,
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 24, TextAnchor.MiddleCenter);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;

            return go.GetComponent<Button>();
        }
    }
}