// Copyright (C) 2018 Creative Spore - All Rights Reserved
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CreativeSpore
{
    #pragma warning disable 0414
    [CreateAssetMenu(fileName = "Block Scheme", menuName = "Game Data/Block Scheme", order = 1)]
    public class BlockScheme : ScriptableObject 
    {
        [SerializeField]
        private Material m_material = null;
        [SerializeField]
        private Texture2D m_texture = null;

        [Space, Header("Texture Parameters")]
        [SerializeField]
        private Texture2D m_background = null;
        [SerializeField]
        private Texture2D m_pattern = null;
        [SerializeField]
        private Texture2D m_portrait = null;
        [SerializeField]
        private Color m_patternTintColor = Color.white;

        private Material m_matCopy;
        public Material BlockMaterial
        {
            get
            {
                if(!m_matCopy)
                {
                    m_matCopy = new Material(m_material);
                    m_matCopy.name += "(" + name + ")";
                    m_matCopy.mainTexture = m_texture;
                    m_matCopy.hideFlags = HideFlags.HideAndDontSave;
                }
                return m_matCopy;
            }
        }
        public Texture2D BlockTexture => m_texture;

        private void OnValidate()
        {
            m_matCopy = null;
        }
    }
}
