// Copyright (C) 2018 Creative Spore - All Rights Reserved
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Transactions;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CreativeSpore
{
    [ExecuteAlways]
    public class BlockBehaviour : MonoBehaviour 
    {
        [SerializeField]
        private BlockScheme m_blockScheme = null;
        [SerializeField]
        private ParticleSystem m_explosionFx = null;

        public static System.Action<BlockBehaviour> OnBlockExploded = delegate { };

        public BlockScheme BlockScheme => m_blockScheme;
        public bool IsVisible { get; private set; }
        public bool IsDisabled => m_disabled;

        private float m_explosionTimeLeft;
        private static Collider[] s_explosionHits = new Collider[32];
        private bool m_disabled = false;
        private MeshRenderer m_meshRenderer;
        private Rigidbody m_rigidBody;

        private void OnValidate()
        {
            if (!m_blockScheme)
                return;
            var meshRenderer = GetComponentInChildren<MeshRenderer>();
            if (gameObject.scene.IsValid())
            {
                if (meshRenderer.sharedMaterial != m_blockScheme.BlockMaterial)
                    meshRenderer.sharedMaterial = m_blockScheme.BlockMaterial;
            }
            else if(meshRenderer.sharedMaterial != GameSettings.Default.Visual.BlockMaterial)
            {
                meshRenderer.sharedMaterial = GameSettings.Default.Visual.BlockMaterial;
            }
        }

        private void Start()
        {            
            m_meshRenderer = GetComponentInChildren<MeshRenderer>();
            m_rigidBody = GetComponent<Rigidbody>();
            m_meshRenderer.sharedMaterial = m_blockScheme.BlockMaterial;
        }        

        private void OnBecameVisible()
        {
            IsVisible = true;
        }

        private void OnBecameInvisible()
        {
            IsVisible = false;
        }

        public void Explode(float delay = 0f)
        {
            if (m_explosionTimeLeft > 0f)
                return;
            
            m_explosionTimeLeft = delay;            
            
            StartCoroutine(ExplosionCO());
        }

        public void RotateFace()
        {
            transform.DOLocalRotate(Vector3.up * 180f, 0.2f, RotateMode.LocalAxisAdd);
            PoolAudioSource.PlayClipAtPoint(GameSettings.Default.Sounds.BlockFaceRotation, transform.position);
        }

        public void EnablePhysics()
        {
            m_rigidBody.isKinematic = false;
        }

        public void Enable(bool playExplosionFx = true)
        {
            m_rigidBody.isKinematic = false;
            m_meshRenderer.sharedMaterial = m_blockScheme.BlockMaterial;
            if (m_disabled)
            {
                if (IsVisible && playExplosionFx)
                {
                    PlayExplosionFx();
                }
                m_disabled = false;
            }
        }

        public void Disable(bool playExplosionFx = true)
        {
            m_rigidBody.isKinematic = true;
            if (!m_disabled)
            {
                m_disabled = true;
                m_meshRenderer.sharedMaterial = GameSettings.Default.Visual.BlockDisabledMaterial;
                if (IsVisible && playExplosionFx)
                {
                    PlayExplosionFx();
                }
            }
        }

        private IEnumerator ExplosionCO()
        {
            yield return new WaitForEndOfFrame();

            // Explode near blocks
            int hits = Physics.OverlapSphereNonAlloc(transform.position, 0.6f, s_explosionHits);
            for (int i = 0; i < hits; i++)
            {
                BlockBehaviour blockBehaviour = s_explosionHits[i].GetComponent<BlockBehaviour>();
                if (blockBehaviour 
                    && !blockBehaviour.m_disabled 
                    && blockBehaviour != this 
                    && blockBehaviour.BlockScheme == BlockScheme)
                {
                    blockBehaviour.Explode(m_explosionTimeLeft + GameSettings.Default.Gameplay.explosionDelay);
                }
            }

            while (m_explosionTimeLeft > 0f)
            {
                m_explosionTimeLeft -= Time.deltaTime;
                yield return null;
            }
            Destroy(gameObject);
            OnBlockExploded.Invoke(this);

            // Explosion Shake Effect
            Vector3 savedCameraPosition = Camera.main.transform.localPosition;
            Camera.main.DOShakePosition(
                GameSettings.Default.Gameplay.cameraShakeDuration,
                GameSettings.Default.Gameplay.cameraShakeStrength,
                GameSettings.Default.Gameplay.cameraShakeVibrato);

            // Explosion Particles
            PlayExplosionFx();            
        }

        private void PlayExplosionFx()
        {
            PoolAudioSource.PlayClipAtPoint(GameSettings.Default.Sounds.BlockExplosion, transform.position, 1f, Random.Range(0.8f, 1.2f));
            var explosionParticles = this.PoolInstantiate<ParticleSystem>(m_explosionFx, transform.position, m_explosionFx.transform.rotation);
            var particleRenderer = explosionParticles.GetComponent<ParticleSystemRenderer>();
            if (particleRenderer)
            {
                particleRenderer.sharedMaterial = m_disabled? 
                    GameSettings.Default.Visual.BlockDisabledMaterial 
                    : 
                    m_blockScheme.BlockMaterial;                
            }
            this.PoolDestroy(explosionParticles.gameObject, explosionParticles.main.duration);
        }
    }
}
