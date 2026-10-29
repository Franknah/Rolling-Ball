using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RollingBall.EditorTools
{
    /// <summary>Generates the placeholder sprites from signed-distance math. Runs only in the editor.</summary>
    public static class PlaceholderArt
    {
        public const string Dir = "Assets/Resources/ArtAssets/Generated";
        private const int Size = 64;
        private const float Half = Size * 0.5f;

        [MenuItem("Rolling Ball/Generate Art")]
        public static void GenerateMenu() => GenerateAll();

        public static void GenerateAll()
        {
            Directory.CreateDirectory(Dir);

            Write("cell_bg", CellBg, new Color32(52, 60, 76, 255));
            Write("pipe_straight", Straight, new Color32(150, 200, 255, 255));
            Write("pipe_elbow", Elbow, new Color32(150, 200, 255, 255));
            Write("pipe_cross", Cross, new Color32(150, 200, 255, 255));
            Write("ball", BallPixel, Color.clear);
            Write("flag", FlagPixel, Color.clear);

            AssetDatabase.Refresh();
            Debug.Log("[RollingBall] Placeholder art generated into " + Dir);
        }

        private static void Write(string name, Func<float, float, Color32?> pixel, Color fallback)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            var clear = new Color32(0, 0, 0, 0);

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    Color32? c = pixel(x + 0.5f, y + 0.5f);
                    tex.SetPixel(x, y, c ?? clear);
                }
            }
            tex.Apply();

            string path = Dir + "/" + name + ".png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 64f;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        // ---- shapes (pixel coordinates, y up, 0..64) ----

        private static float SdRoundRect(float x, float y, float hx, float hy, float r)
        {
            float qx = Mathf.Abs(x) - hx + r;
            float qy = Mathf.Abs(y) - hy + r;
            return Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f))
                   + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        private static float SdSegment(float px, float py, float ax, float ay, float bx, float by)
        {
            float pax = px - ax, pay = py - ay;
            float bax = bx - ax, bay = by - ay;
            float h = Mathf.Clamp01((pax * bax + pay * bay) / (bax * bax + bay * bay));
            float dx = pax - bax * h, dy = pay - bay * h;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        private static bool PointInTriangle(float px, float py,
            float ax, float ay, float bx, float by, float cx, float cy)
        {
            float d1 = (px - bx) * (ay - by) - (ax - bx) * (py - by);
            float d2 = (px - cx) * (by - cy) - (bx - cx) * (py - cy);
            float d3 = (px - ax) * (cy - ay) - (cx - ax) * (py - ay);
            bool hasNeg = d1 < 0f || d2 < 0f || d3 < 0f;
            bool hasPos = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(hasNeg && hasPos);
        }

        private static Color32? CellBg(float x, float y)
        {
            float cx = x - Half, cy = y - Half;
            float outer = SdRoundRect(cx, cy, 30f, 30f, 6f);
            float inner = SdRoundRect(cx, cy, 27f, 27f, 5f);
            return outer <= 0f && inner > 0f ? (Color32?)new Color32(52, 60, 76, 255) : null;
        }

        private static Color32? Straight(float x, float y) =>
            SdSegment(x, y, Half, 0f, Half, Size) <= 5.5f ? (Color32?)new Color32(150, 200, 255, 255) : null;

        private static Color32? Elbow(float x, float y)
        {
            if (x > Size || y > Size) return null;
            float dx = x - Size, dy = y - Size;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            return Mathf.Abs(d - Half) <= 5.5f ? (Color32?)new Color32(150, 200, 255, 255) : null;
        }

        private static Color32? Cross(float x, float y)
        {
            bool vertical = SdSegment(x, y, Half, 0f, Half, Size) <= 5.5f;
            bool horizontal = SdSegment(x, y, 0f, Half, Size, Half) <= 5.5f;
            return vertical || horizontal ? (Color32?)new Color32(150, 200, 255, 255) : null;
        }

        private static Color32? BallPixel(float x, float y)
        {
            float dx = x - Half, dy = y - Half;
            float d2 = dx * dx + dy * dy;
            if (d2 > 13f * 13f) return null;
            if (d2 < 5f * 5f) return new Color32(255, 240, 220, 255);
            return new Color32(255, 150, 90, 255);
        }

        private static Color32? FlagPixel(float x, float y)
        {
            if (SdSegment(x, y, 22f, 5f, 22f, 58f) <= 2.5f) return new Color32(235, 235, 245, 255);
            if (PointInTriangle(x, y, 22f, 58f, 51f, 58f, 22f, 30f)) return new Color32(235, 70, 70, 255);
            return null;
        }
    }
}