// Copyright (C) 2018 Creative Spore - All Rights Reserved
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CreativeSpore
{
    public class GameSettings : ScriptableObject 
    {

        [SerializeField]
        private GamePlaySettingsBlock m_gameplay = new GamePlaySettingsBlock();
        [Space, SerializeField]
        private VisualSettingsBlock m_visual = new VisualSettingsBlock();
        [Space, SerializeField]
        private SoundSettingsBlock m_sounds = new SoundSettingsBlock();

        public GamePlaySettingsBlock Gameplay => m_gameplay;
        public VisualSettingsBlock Visual => m_visual;
        public SoundSettingsBlock Sounds => m_sounds;

        [System.Serializable]
        public class GamePlaySettingsBlock
        {
            [Header("Camera")]
            [Range(0f, 1f)]
            public float cameraRotationSmoothFactor = 0.2f;

            [Header("Shooting")]
            public ProjectileBehaviour projectilePrefab;
            public float shotTimeToHit = .4f;
            public float shotBounceTime = .5f;

            [Header("Explosion")]
            public float explosionDelay = 0.1f;
            public float cameraShakeDuration = .5f;
            public float cameraShakeStrength = .2f;
            public int cameraShakeVibrato = 30;

            [Header("Tower")]
            public int towerActiveChunkSize = 8;
            public BlockBehaviour[] blockPrefabs = null;
            
            [Header("Level")]
            public float progressNeededToComplete = 15f / 16f;
        }

        [System.Serializable]
        public class VisualSettingsBlock
        {
            [Header("Tower Blocks")]
            public Material BlockDisabledMaterial = null;
            public Material BlockMaterial = null;
        }

        [System.Serializable]
        public class SoundSettingsBlock
        {
            [Header("Sounds")]
            public AudioClip Whoosh = null;
            public AudioClip BlockExplosion = null;
            public AudioClip BlockHit = null;
            public AudioClip WaterSplash = null;
            public AudioClip BlockFaceRotation = null;
        }


        public static GameSettings Default
        {
            get
            {
                if(s_default == null)
                {
                    InitializeDefaultSettings();
                }
                return s_default;
            }
        }

        private static GameSettings s_default = null;
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#endif
        private static void InitializeDefaultSettings()
        {
            // Try to find a suitable asset
#if UNITY_EDITOR
            if (s_default == null || (s_default.hideFlags & HideFlags.HideAndDontSave) != 0)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:GameSettings");
                if (guids.Length > 0)
                {
                    string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    s_default = UnityEditor.AssetDatabase.LoadAssetAtPath<GameSettings>(assetPath);
                }
                else
                {
                    // If no  asset was found, create a default asset
                    string assetPath = "Assets/Resources/GameSettings.asset";
                    s_default = ScriptableObject.CreateInstance<GameSettings>();
                    UnityEditor.AssetDatabase.CreateAsset(s_default, assetPath);
                    UnityEditor.AssetDatabase.Refresh();
                    s_default = UnityEditor.AssetDatabase.LoadAssetAtPath<GameSettings>(assetPath);
                    Debug.Log("Created " + assetPath, s_default);
                }
            }
#else
                s_default = Resources.FindObjectsOfTypeAll<GameSettings>().FirstOrDefault();
#endif

            // If no  asset was found, create a default asset
            if (s_default == null)
            {
                s_default = ScriptableObject.CreateInstance<GameSettings>();
                s_default.hideFlags = HideFlags.DontSave;
            }
        }                
    }
}
