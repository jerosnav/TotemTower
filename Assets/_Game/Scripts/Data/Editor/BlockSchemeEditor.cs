// Copyright (C) 2018 Creative Spore - All Rights Reserved
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

namespace CreativeSpore
{
    [CustomEditor(typeof(BlockScheme))]
    public class BlockSchemeEditor : Editor
    {
        private SerializedProperty m_texture;
        private SerializedProperty m_background;
        private SerializedProperty m_pattern;
        private SerializedProperty m_portrait;
        private SerializedProperty m_patternTintColor;

        private void OnEnable()
        {
            m_texture = serializedObject.FindProperty("m_texture");
            m_background = serializedObject.FindProperty("m_background");
            m_pattern = serializedObject.FindProperty("m_pattern");
            m_portrait = serializedObject.FindProperty("m_portrait");
            m_patternTintColor = serializedObject.FindProperty("m_patternTintColor");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUI.enabled = CheckCompositeTexturesRequirements();
            if (m_texture.objectReferenceValue && GUILayout.Button("Update Texture")
                || 
                !m_texture.objectReferenceValue && GUILayout.Button("Create Texture"))
            {
                UpdateTexture();
            }
            GUI.enabled = true;
        }

        private bool CheckCompositeTexturesRequirements()
        {
            Texture2D background = m_background.objectReferenceValue as Texture2D;
            Texture2D pattern = m_pattern.objectReferenceValue as Texture2D;
            Texture2D portrait = m_portrait.objectReferenceValue as Texture2D;
            if(!background || !pattern || !portrait)
            {
                EditorGUILayout.HelpBox("There are some textures not defined!", MessageType.Info);
                return false;
            }
            if(background.width != background.height)
            {
                EditorGUILayout.HelpBox("Background texture width and height should be equal!", MessageType.Info);
                return false;
            }
            if(pattern.width != background.width || pattern.height != background.height)
            {
                EditorGUILayout.HelpBox("Pattern texture width and height should be " + background.width + " x " + (background.height), MessageType.Info);
                return false;
            }
            if (portrait.width != background.width || portrait.height != background.height)
            {
                EditorGUILayout.HelpBox("Portrait texture width and height should be " + background.width + " x " + (background.height), MessageType.Info);
                return false;
            }
            return true;
        }

        private void UpdateTexture()
        {
            Texture2D background = m_background.objectReferenceValue as Texture2D;
            Texture2D pattern = m_pattern.objectReferenceValue as Texture2D;
            Texture2D portrait = m_portrait.objectReferenceValue as Texture2D;
            Texture2D texture = m_texture.objectReferenceValue ?
                m_texture.objectReferenceValue as Texture2D
                :
                new Texture2D(background.width * 2, background.height * 2);

            string texturePath = AssetDatabase.GetAssetPath(texture);
            if(string.IsNullOrEmpty(texturePath))
            {
                texturePath = AssetDatabase.GetAssetPath(target);
                texturePath = texturePath.Substring(0, texturePath.LastIndexOf('/') + 1) + target.name + ".png";

                File.WriteAllBytes(texturePath, texture.EncodeToPNG());
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                m_texture.objectReferenceValue = texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                Debug.Log("Created texture: " + texturePath, m_texture.objectReferenceValue);
                serializedObject.ApplyModifiedProperties();
            }

            PrepareTextureImportSettings(texture, background, pattern, portrait);
            MergeTextures(texture, background, pattern, portrait);

            //Save Changes
            File.WriteAllBytes(texturePath, texture.EncodeToPNG());
            Debug.Log("Saved texture : " + texturePath, texture);
        }

        private void MergeTextures(Texture2D target, Texture2D background, Texture2D pattern, Texture2D portrait)
        {
            if(target.width != background.width * 2 || target.width != background.height * 2)
            {
                Debug.LogErrorFormat("target texture should be {0} x {1}", background.width, background.height);
                return;
            }

            int tileSize = background.width; //NOTE: background should be squared in size

            //Copy Background in each cornet of the target texture
            Color32[] colors = background.GetPixels32();
            BlendPixels32(target, 0, 0, tileSize, tileSize, colors, 1f, m_patternTintColor.colorValue);
            BlendPixels32(target, tileSize, 0, tileSize, tileSize, colors, 1f, m_patternTintColor.colorValue);
            BlendPixels32(target, 0, tileSize, tileSize, tileSize, colors, 1f, m_patternTintColor.colorValue);
            BlendPixels32(target, tileSize, tileSize, tileSize, tileSize, colors, 1f, m_patternTintColor.colorValue);

            //Copy Pattern in the middle row
            colors = pattern.GetPixels32();
            BlendPixels32(target, 0, tileSize / 2, tileSize, tileSize, colors, 0f, Color.gray);
            BlendPixels32(target, tileSize, tileSize / 2, tileSize, tileSize, colors, 0f, Color.gray);

            //Copy Portrait in the middle
            colors = portrait.GetPixels32();
            BlendPixels32(target, tileSize / 2, tileSize / 2, tileSize, tileSize, colors, 0.8f, m_patternTintColor.colorValue);

            // Copy Pattern
            target.Apply();
        }

        private void BlendPixels32(Texture2D destTexture, int x, int y, int blockWidth, int blockHeight, Color32[] colors, float alpha = 1f, Color tintColor = default(Color))
        {
            Color32[] destColors = destTexture.GetPixels32();
            int colorsIdx = 0;
            for (int yf = 0; yf < blockHeight; yf++)
            {
                for (int xf = 0; xf < blockWidth; xf++, colorsIdx++)
                {
                    int destIndex = (y + yf) * destTexture.width + (x + xf);
                    Color a = destColors[destIndex];
                    Color b = colors[colorsIdx];
                    if(tintColor != default(Color32))
                    {
                        tintColor.a = 1f;
                        b *= tintColor;
                    }
                    Color c = Color32.Lerp(a, b, b.a * alpha);
                    c.a = 255;
                    destColors[destIndex] = c;
                }
            }
            destTexture.SetPixels32(destColors);
        }

        private void PrepareTextureImportSettings(params Texture2D[] textures)
        {
            foreach (var texture in textures)
            {
                if (null == texture) continue;

                string assetPath = AssetDatabase.GetAssetPath(texture);
                var tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (tImporter != null)
                {
                    tImporter.textureType = TextureImporterType.Default;
                    tImporter.isReadable = true;
                    tImporter.textureCompression = TextureImporterCompression.Uncompressed;

                    AssetDatabase.ImportAsset(assetPath);
                    AssetDatabase.Refresh();
                }
            }
        }
    }
}
