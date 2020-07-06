// Copyright (C) 2018 Creative Spore - All Rights Reserved
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CreativeSpore
{
    public class TowerGenerator : MonoBehaviour 
    {
        public event System.Action OnActiveTowerRingChanged = delegate { };
        public event System.Action<BlockBehaviour> OnBlockFallen = delegate { };

        [SerializeField]
        private int m_ringsCount = 5;
        [SerializeField]
        private int m_ringBlockCount = 16;
        [SerializeField]
        private float m_blockRadious = 1f;
        [SerializeField]
        private float m_blockHeight = 1f;
        [SerializeField, Range(0f, 1f)]
        private float m_rotationOffsetNormalized = 0.5f;
        [SerializeField]
        private bool m_lockRandomSeed = false;
        [SerializeField]
        private int m_activeTowerRing;
        [SerializeField] 
        private BlockBehaviour[] m_blockPrefabs = null;

        [Header("References")]
        [SerializeField]
        private Transform m_blocksParent = null;
        [SerializeField]
        private Transform m_fallenBlocksDetector = null;

        [Header("Values Managed By Tower Generator")]
        [Space, Space]
        [SerializeField]
        private int m_topTowerRing;        
        [SerializeField]
        private float m_stepAngle;
        [SerializeField]
        private float m_ringRadious;
        [SerializeField]
        private Vector3 m_ringBoxColliderSize;


        public BlockBehaviour[] BlockPrefabs => m_blockPrefabs;
        public int RingCount => m_ringsCount;
        public int RingBlockCount => m_ringBlockCount;
        public int TotalBlockCounter => m_ringsCount * m_ringBlockCount;
        public float BlockRadious => m_blockRadious;
        public float BlockHeight => m_blockHeight;
        public int ActiveTowerRing { get => m_activeTowerRing; set => SetActiveTowerRingInternal(value); }        

        public float TopTowerRingtHeight => GetRingCenterLocalPosition(m_topTowerRing).y;
        public float ActiveTowerRingtHeight => GetRingCenterLocalPosition(m_activeTowerRing).y;


        private UnityEngine.Random.State m_savedRandomState;
        private BlockBehaviour[][] m_blocksLookup;        

        private void Start()
        {
            StartCoroutine(UpdateTopTowerRingCO());
            StartCoroutine(UpdateActiveTowerRingCO());
        }

        private void OnTriggerExit(Collider other)
        {
            OnBlockFallen.Invoke(other.GetComponentInParent<BlockBehaviour>());
        }

        private void OnDrawGizmosSelected()
        {
            // Draw Top Tower Ring
            Gizmos.color = Color.green * 0.5f;
            Gizmos.DrawCube(transform.position + Vector3.up * TopTowerRingtHeight, m_ringBoxColliderSize);            

            // Draw Active Tower Ring
            Gizmos.color = Color.magenta * 0.5f;
            Gizmos.DrawCube(transform.position + Vector3.up * ActiveTowerRingtHeight, m_ringBoxColliderSize);
        }

        [ContextMenu("GenerateTower")]
        public void GenerateTower()
        {
            GenerateTowerInternal();
        }

        public void GenerateTower(BlockBehaviour[] blockPrefabs)
        {
            if (blockPrefabs == null || blockPrefabs.Length == 0)
                return;
            m_blockPrefabs = blockPrefabs;            
            GenerateTowerInternal();
        }

        public void EnableBlockPhysics()
        {
            foreach (var ring in m_blocksLookup)
            {
                foreach (var block in ring)
                {
                    if(block && !block.IsDisabled)
                    {
                        block.EnablePhysics();
                    }
                }
            }
        }

        public Vector3 GetRingCenterWorldPosition(int ring)
        {
            return transform.TransformPoint(GetRingCenterLocalPosition(ring));
        }

        public Vector3 GetRingCenterLocalPosition(int ring)
        {
            return Vector3.up * ((ring + 0.5f) * BlockHeight);
        }

        private void SetActiveTowerRingInternal(int value)
        {
            int clampValue = Mathf.Clamp(value, 0, m_ringsCount - 1);
            m_activeTowerRing = clampValue;
            m_fallenBlocksDetector.transform.position = GetRingCenterWorldPosition(m_activeTowerRing - 1);
            OnActiveTowerRingChanged();
        }

        private void GenerateTowerInternal()
        {
            if (m_lockRandomSeed)
                UnityEngine.Random.state = m_savedRandomState;
            else
                m_savedRandomState = UnityEngine.Random.state;
            ClearTower();
            m_topTowerRing = RingCount - 1;
            ActiveTowerRing = 0;
            m_stepAngle = 360f / RingBlockCount;
            m_ringRadious = BlockRadious / Mathf.Sin(Mathf.Deg2Rad * m_stepAngle / 2f);
            m_ringBoxColliderSize = new Vector3(3f * m_ringRadious, BlockHeight * 0.1f, 3f * m_ringRadious);
            
            m_blocksLookup = new BlockBehaviour[RingCount][];
            for (int i = 0; i < m_ringsCount; i++)
            {
                float rotOffset = i * m_rotationOffsetNormalized * m_stepAngle;
                m_blocksLookup[i] = GenerateRing((i + .5f) * m_blockHeight, RingBlockCount, BlockRadious, rotOffset);
            }
        }

        private BlockBehaviour[] GenerateRing(float height, int blocks, float blockRadious, float rotAng)
        {            
            Vector3 center = Vector3.up * height;
            Vector3 startBlockPos = Vector3.right * m_ringRadious;
            BlockBehaviour[] blockArr = new BlockBehaviour[blocks];
            for (int i = 0; i < blocks; i++)
            {
                Vector3 position = center + Quaternion.Euler(0f, m_stepAngle * i, 0f) * startBlockPos;
                var blockBhv = InstantiateBlock(position, Quaternion.Euler(0f, rotAng, 0f));
                blockArr[i] = blockBhv;
                Vector3 facingDirection = blockBhv.transform.localPosition - center;
                // Alternate randomly between facing in and outside of the tower
                if (Random.value >= .5f)
                {
                    facingDirection = -facingDirection;
                }
                blockBhv.transform.forward = facingDirection;
            }
            return blockArr;
        }

        private BlockBehaviour InstantiateBlock(Vector3 position, Quaternion rotation)
        {
            BlockBehaviour blockPrefab = m_blockPrefabs[UnityEngine.Random.Range(0, m_blockPrefabs.Length)];
            Vector3 rotatedPosition = rotation * position;
            var blockObj = Instantiate(blockPrefab, m_blocksParent.TransformPoint(rotation * position), Quaternion.identity, m_blocksParent);
            return blockObj;
        }

        private void ClearTower()
        {
            while(m_blocksParent.childCount > 0)
            {
                DestroyImmediate(m_blocksParent.GetChild(0).gameObject);
            }
        }

        private IEnumerator UpdateTopTowerRingCO()
        {
            while (true)
            {
                // Check if there is no left blocks on this ring (all have fallen down)
                if (!Physics.CheckBox(
                    transform.position + Vector3.up * TopTowerRingtHeight, 
                    m_ringBoxColliderSize / 2f))
                {
                    m_topTowerRing--;
                    m_fallenBlocksDetector.gameObject.SetActive(false);
                    ActiveTowerRing--;
                    m_fallenBlocksDetector.gameObject.SetActive(true);
                }
                yield return new WaitForSeconds(0.2f);
            }
        }

        private IEnumerator UpdateActiveTowerRingCO()
        {
            int currentActiveRing = m_activeTowerRing;
            while (true)
            {
                // Disable rings below active ring one by one
                while (currentActiveRing < m_activeTowerRing)
                {
                    int blockCount = 0;
                    foreach (var block in m_blocksLookup[currentActiveRing])
                    {
                        if (block) // if block was not destroyed
                        {
                            bool playExplosionFx = (currentActiveRing == m_activeTowerRing - 1) && ++blockCount % 2 == 0;
                            block.Disable(playExplosionFx);
                        }
                    }
                    ++currentActiveRing;
                    yield return new WaitForSeconds(0.05f);
                }

                // Enable rings above active ring one by one
                while (currentActiveRing > m_activeTowerRing)
                {
                    --currentActiveRing;
                    foreach (var block in m_blocksLookup[currentActiveRing])
                    {
                        block?.Enable();
                    }
                    yield return new WaitForSeconds(0.2f);
                }

                yield return null;
            }
        }
    }
}
