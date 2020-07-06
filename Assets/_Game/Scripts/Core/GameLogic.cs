// Copyright (C) 2018 Creative Spore - All Rights Reserved
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace CreativeSpore
{
    public class GameLogic : SingletonBehaviour<GameLogic> 
    {
        [Space, Header("References")]
        [SerializeField]
        private Transform m_cameraPivot = null;
        [SerializeField]
        private TowerGenerator m_towerGenerator = null;
        [SerializeField]
        private GameObject m_victoryEffect = null;

        public event System.Action OnProjectileInstantiated = delegate { };
        public event System.Action OnProjectileLeftChanged = delegate { };
        public event System.Action<float> OnLevelProgressChanged = delegate { };
        public event System.Action OnLevelIntroStarted = delegate { };
        public event System.Action OnLevelStarted = delegate { };
        public event System.Action OnLevelCompleted = delegate { };

        public ProjectileBehaviour HoldProjectile => m_holdProjectile;
        public ProjectileBehaviour NextProjectile => m_nextProjectile;
        public bool AllowShooting { get; set; }
        public int ProjectileLeft 
        {
            get => m_projectileLeft;
            private set
            {
                m_projectileLeft = value;
                OnProjectileLeftChanged.Invoke();
            }
        }
        public float LevelProgress
        {
            get => m_levelProgress;
            private set
            {
                if(m_levelProgress != value)
                {
                    m_levelProgress = PlayerProfile.SavedLevelProgress = value;
                    OnLevelProgressChanged.Invoke(value);
                }
            }
        }

        private float m_totalCameraRotation;
        private ProjectileBehaviour m_holdProjectile;
        private ProjectileBehaviour m_nextProjectile;
        private Vector3 m_cameraLocalPosition;        
        private List<BlockBehaviour> m_scoredBlocks = new List<BlockBehaviour>();
        private float m_levelProgress;
        private int m_nonRepatingMaxIdx;
        private BlockBehaviour[] m_nonRepeatingBlocks;
        private int m_projectileLeft;

        private void OnValidate()
        {
            if (!m_towerGenerator)
                m_towerGenerator = FindObjectOfType<TowerGenerator>();
        }

        private void Start()
        {            
            m_cameraLocalPosition = Camera.main.transform.localPosition;
            m_towerGenerator.OnActiveTowerRingChanged += HandleActiveTowerRingChanged;
            GenerateNewTower(3);
        }

        void Update () 
        {
            UpdateCameraRotation();            
        }

        public void StartLevel()
        {            
            StartCoroutine(StartLevelCO());
        }        

        public void RotateCamera(float delta)
        {
            float deltaNormalized = delta / Screen.width;
            m_totalCameraRotation += deltaNormalized * 360;
        }

        public void ExitToMainMenu()
        {
            //Reload Scene
            SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
        }

        public void AddProjectiles(int value)
        {
            ProjectileLeft += value;
        }

        public void ShootProjectile(Vector2 screenPos)
        {
            if (AllowShooting && m_holdProjectile)
            {
                Ray ray = Camera.main.ScreenPointToRay(screenPos);
                RaycastHit hitInfo;
                if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity))
                {
                    var blockBhv = hitInfo.collider.GetComponent<BlockBehaviour>();
                    if (blockBhv && !blockBhv.IsDisabled && ProjectileLeft > 0)
                    {
                        --ProjectileLeft;
                        m_holdProjectile.Shoot(blockBhv, hitInfo);
                        InstanceProjectile();
                    }
                }
            }
        }

        private void HandleActiveTowerRingChanged()
        {
            Vector3 activeRingCenter = m_towerGenerator.transform.position + Vector3.up * m_towerGenerator.ActiveTowerRingtHeight;
            m_cameraPivot.DOLocalMove(activeRingCenter, 0.2f).SetEase(Ease.InQuad);
        }

        private void GenerateNewTower(int blockTypeCount)
        {
            Random.InitState(PlayerProfile.LevelSeed);
            blockTypeCount = Mathf.Clamp(blockTypeCount, 1, GameSettings.Default.Gameplay.blockPrefabs.Length);
            List<BlockBehaviour> blockPrefabs = new List<BlockBehaviour>(GameSettings.Default.Gameplay.blockPrefabs);
            while(blockPrefabs.Count > blockTypeCount)
            {
                blockPrefabs.RemoveAt(Random.Range(0, blockPrefabs.Count));
            }
            m_towerGenerator.GenerateTower(blockPrefabs.ToArray());
        }

        private void ScoreBlock(BlockBehaviour blockBhv)
        {
            if (!m_scoredBlocks.Contains(blockBhv))
            {
                m_scoredBlocks.Add(blockBhv);

                float blocksNeededToCompleteLevel = m_towerGenerator.TotalBlockCounter * GameSettings.Default.Gameplay.progressNeededToComplete;
                LevelProgress = (float)m_scoredBlocks.Count / blocksNeededToCompleteLevel;
            }
        }        

        private void InstanceProjectile()
        {
            m_holdProjectile = m_nextProjectile;
            m_nextProjectile = this.PoolInstantiate(GameSettings.Default.Gameplay.projectilePrefab);
            int randBlock = UnityEngine.Random.Range(0, m_towerGenerator.BlockPrefabs.Length);
            var blockPrefab = GetRandomBlockPrefab();
            m_nextProjectile.BlockScheme = blockPrefab.BlockScheme;


            OnProjectileInstantiated.Invoke();
        }

        private BlockBehaviour GetRandomBlockPrefab()
        {
            BlockBehaviour[] blockPrefabs = m_towerGenerator.BlockPrefabs;
            if (m_nonRepeatingBlocks == null 
                || m_nonRepeatingBlocks.Length != blockPrefabs.Length)
            {
                m_nonRepeatingBlocks = (BlockBehaviour[])blockPrefabs.Clone();
                m_nonRepatingMaxIdx = m_nonRepeatingBlocks.Length;
            }

            int randIdx = UnityEngine.Random.Range(0, m_nonRepatingMaxIdx);
            BlockBehaviour blockPrefab = m_nonRepeatingBlocks[randIdx];
            --m_nonRepatingMaxIdx;
            if (m_nonRepatingMaxIdx == 0)
                m_nonRepatingMaxIdx = m_nonRepeatingBlocks.Length - 1;
            m_nonRepeatingBlocks[randIdx] = m_nonRepeatingBlocks[m_nonRepatingMaxIdx];
            m_nonRepeatingBlocks[m_nonRepatingMaxIdx] = blockPrefab;
            return blockPrefab;
        }

        private void UpdateCameraRotation()
        {
            if (m_totalCameraRotation != 0)
            {
                float rotationDelta = m_totalCameraRotation 
                    * Mathf.Clamp01(Time.deltaTime / GameSettings.Default.Gameplay.cameraRotationSmoothFactor);
                m_cameraPivot.Rotate(Vector3.up, rotationDelta);
                m_totalCameraRotation -= rotationDelta;
                if (Mathf.Abs(m_totalCameraRotation) < 1f)
                    m_totalCameraRotation = 0f;
            }
        }

        private IEnumerator StartLevelCO()
        {
            OnLevelIntroStarted.Invoke();
            AllowShooting = false;
            ProjectileLeft = 23;
            m_cameraPivot.transform.localPosition = m_towerGenerator.GetRingCenterWorldPosition(0);
            m_cameraPivot.transform.rotation = Quaternion.identity;

            yield return new WaitForSeconds(1f);

            // Rotate 360 degrees animation
            const float timeToReachTop = 2f;
            m_cameraPivot.DORotate(
                new Vector3(0f, 360f, 0f),
                timeToReachTop,
                RotateMode.FastBeyond360
                ).SetEase(Ease.InOutFlash);

            // Move to the top animation           
            int activeRing = Mathf.Max(0, m_towerGenerator.RingCount - GameSettings.Default.Gameplay.towerActiveChunkSize);
            Vector3 activeRingCenter = m_towerGenerator.GetRingCenterWorldPosition(activeRing);
            m_cameraPivot.DOLocalMove(activeRingCenter, timeToReachTop).SetEase(Ease.InOutFlash);

            PoolAudioSource.PlayClipAtPoint2D(GameSettings.Default.Sounds.Whoosh, Vector3.zero, 1f);

            yield return new WaitForSeconds(timeToReachTop);

            m_towerGenerator.ActiveTowerRing = activeRing;

            yield return new WaitForSeconds(1f);

            OnLevelStarted.Invoke();

            m_towerGenerator.EnableBlockPhysics();

            AllowShooting = true;
            LevelProgress  = 0;
            m_scoredBlocks.Clear();

            // Instance twice to crate the hold and next projectiles
            InstanceProjectile();
            InstanceProjectile();

            m_towerGenerator.OnBlockFallen += ScoreBlock;
            BlockBehaviour.OnBlockExploded = ScoreBlock;

            yield return LevelMainLoop();

            this.PoolDestroy(m_holdProjectile);
            this.PoolDestroy(m_nextProjectile);
            
            // Level Completed !!!
            OnLevelCompleted.Invoke();
            PlayerProfile.LevelSeed = (int)(Random.value * int.MaxValue);
            PlayerProfile.PlayerLevel++;
            PlayerProfile.SavedLevelProgress = 0f;
            m_victoryEffect.gameObject.SetActive(true);
            Camera.main.transform.DOLocalMove(new Vector3(0f, 15f, -30f), 2f).SetEase(Ease.OutQuint);

            yield return new WaitForSeconds(2f);

            m_cameraPivot.DOLocalRotate(Vector3.up * 180f, 10f, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetRelative(true)
                .SetLoops(-1, LoopType.Incremental);

            m_towerGenerator.OnBlockFallen -= ScoreBlock;
            BlockBehaviour.OnBlockExploded -= ScoreBlock;

            // Wait for screen touched
            while(!Input.GetMouseButtonDown(0))
            {
                yield return null;
            }

            //Reload Scene
            ExitToMainMenu();

        }

        private IEnumerator LevelMainLoop()
        {
            while(m_levelProgress < 1f)
            {
                if (Application.isEditor && Input.GetKeyDown(KeyCode.Return))
                    break;

                // Restore camera local position (could be moved by some effects like explosions)
                Camera.main.transform.localPosition = Vector3.Slerp(
                    Camera.main.transform.localPosition,
                    m_cameraLocalPosition,
                    4f * Time.deltaTime);

                yield return null;
            }
        }
    }
}
