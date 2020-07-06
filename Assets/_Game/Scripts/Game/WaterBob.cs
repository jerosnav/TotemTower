// Copyright (C) 2018 Creative Spore - All Rights Reserved
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CreativeSpore
{
    [RequireComponent(typeof(Rigidbody))]
    public class WaterBob : MonoBehaviour 
    {
        [SerializeField]
        private float m_waterLevel = 0f;
        [SerializeField]
        private float m_waterForce = 12f;
        [SerializeField]
        private float m_amplitude = 1f;
        [SerializeField]
        private float m_waveSpeed = 1f;

        private bool m_playWaterSplash;
        private Rigidbody m_rigidBody;
        private void Start()
        {
            m_rigidBody = GetComponent<Rigidbody>();
        }

        private void OnEnable()
        {
            m_playWaterSplash = true;
        }

        private void FixedUpdate()
        {
            float f = (transform.position.x - m_waveSpeed * Time.time);
            float waveHeight = m_amplitude * (2f * Mathf.PerlinNoise(f, 0.5f) - 1f);
            float waterDeep = (m_waterLevel + waveHeight) - transform.position.y;
            if (waterDeep > 0)
            {
                if (m_playWaterSplash)
                {
                    m_playWaterSplash = false;
                    PoolAudioSource.PlayClipAtPoint(GameSettings.Default.Sounds.WaterSplash, transform.position, 1f, Random.Range(0.8f, 1.2f));
                }
                m_rigidBody.AddForce(Vector3.up * m_waterForce * (waterDeep + 1f), ForceMode.Force);
            }
        }
    }
}
