using UnityEngine;
using UnityEditor;
using System.IO;

namespace BubblesGame.Editor
{
    public class CircleSpriteGenerator : EditorWindow
    {
        private int spriteSize = 256;
        private Color circleColor = Color.white;
        private string spriteName = "Circle";

        [MenuItem("Tools/Bubbles/Generate Circle Sprite")]
        public static void ShowWindow()
        {
            GetWindow<CircleSpriteGenerator>("Circle Sprite Generator");
        }

        void OnGUI()
        {
            GUILayout.Label("Generate Circle Sprite", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            spriteSize = EditorGUILayout.IntField("Sprite Size", spriteSize);
            circleColor = EditorGUILayout.ColorField("Color", circleColor);
            spriteName = EditorGUILayout.TextField("Sprite Name", spriteName);

            EditorGUILayout.Space();

            if (GUILayout.Button("Generate Circle Sprite"))
            {
                GenerateCircleSprite();
            }
        }

        void GenerateCircleSprite()
        {
            // Cria textura
            Texture2D texture = new Texture2D(spriteSize, spriteSize, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[spriteSize * spriteSize];

            float centerX = spriteSize / 2f;
            float centerY = spriteSize / 2f;
            float radius = spriteSize / 2f - 2f; // -2 para borda suave

            for (int y = 0; y < spriteSize; y++)
            {
                for (int x = 0; x < spriteSize; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                    
                    if (distance <= radius)
                    {
                        pixels[y * spriteSize + x] = circleColor;
                    }
                    else if (distance <= radius + 2f)
                    {
                        // Anti-aliasing na borda
                        float alpha = 1f - (distance - radius) / 2f;
                        pixels[y * spriteSize + x] = new Color(circleColor.r, circleColor.g, circleColor.b, alpha);
                    }
                    else
                    {
                        pixels[y * spriteSize + x] = Color.clear;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            // Cria sprite
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, spriteSize, spriteSize),
                new Vector2(0.5f, 0.5f), // Pivot no centro
                100f // Pixels per unit
            );

            // Salva o asset
            string folderPath = "Assets/Games/Bubbles/Materials";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                string parentFolder = "Assets/Games/Bubbles";
                if (!AssetDatabase.IsValidFolder(parentFolder))
                {
                    AssetDatabase.CreateFolder("Assets/Games", "Bubbles");
                }
                AssetDatabase.CreateFolder(parentFolder, "Materials");
            }

            string assetPath = $"{folderPath}/{spriteName}.png";
            byte[] pngData = texture.EncodeToPNG();
            File.WriteAllBytes(assetPath, pngData);

            AssetDatabase.Refresh();

            // Importa como sprite
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 100;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            }

            // Seleciona o sprite criado
            Object asset = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);

            Debug.Log($"Circle sprite criado em: {assetPath}");
        }
    }
}

