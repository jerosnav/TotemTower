// Copyright (C) 2018 Creative Spore - All Rights Reserved
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CreativeSpore
{
    public class GameView : SingletonBehaviour<GameView> 
    {
        [SerializeField]
        private float m_projectileHoldPivotDistance = 4f;
        [SerializeField]
        private float m_projectileNextPivotDistance = 6f;
        [Header("References"), Space]
        [SerializeField]
        private RectTransform m_gameHUD = null;
        [SerializeField]
        private RectTransform m_mainMenuHUD = null;
        [SerializeField]
        private RectTransform m_projectileHoldPivot = null;
        [SerializeField]
        private RectTransform m_projectileNextPivot = null;
        [SerializeField]
        private TextMeshProUGUI m_projectilesLeft = null;
        [SerializeField]
        private Image m_levelProgressBar = null;
        [SerializeField]
        private Image m_savedLevelProgressBar = null;
        [SerializeField]
        private TextMeshProUGUI m_playerLevelText = null;
        [SerializeField]
        private TextMeshProUGUI m_levelProgressText = null;
        [SerializeField]
        private RectTransform m_getMoreBallsPanel = null;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(GetHoldProjectilePivotWorldPosition(), 0.25f);
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(GetNextProjectilePivotWorldPosition(), 0.25f);
        }

        private void OnValidate()
        {
            UpdatePlayerLevel();
        }

        private IEnumerator Start()
        {
            UpdatePlayerLevel();
            m_gameHUD.gameObject.SetActive(false);
            m_getMoreBallsPanel.gameObject.SetActive(false);
            yield return new GameLogic.WaitForInitialization();
            GameLogic.Instance.OnProjectileInstantiated += HandleProjectileInstantiated;
            GameLogic.Instance.OnLevelProgressChanged += HandleLevelProgressChanged;
            GameLogic.Instance.OnLevelStarted += HandleLevelStarted;
            GameLogic.Instance.OnLevelIntroStarted += HandleLevelIntroStarted;
            GameLogic.Instance.OnLevelCompleted += HandleLevelCompleted;
            GameLogic.Instance.OnProjectileLeftChanged += HandleProjectileLeftChanged;
            HandleLevelProgressChanged(0);
        }

        private void HandleProjectileLeftChanged()
        {
            m_projectilesLeft.text = GameLogic.Instance.ProjectileLeft.ToString();
            if (GameLogic.Instance.ProjectileLeft == 0)
            {
                m_getMoreBallsPanel.gameObject.SetActive(true);
            }
        }

        public void UpdatePlayerLevel()
        {
            m_playerLevelText.text = string.Format("Level {0}", PlayerProfile.PlayerLevel);
        }
        private void HandleLevelIntroStarted()
        {
            m_mainMenuHUD.gameObject.SetActive(false);
        }
        private void HandleLevelStarted()
        {
            m_gameHUD.gameObject.SetActive(true);
            m_savedLevelProgressBar.fillAmount = PlayerProfile.SavedLevelProgress;
        }
        private void HandleLevelCompleted()
        {
            m_gameHUD.gameObject.SetActive(false);
        }

        private Vector3 GetHoldProjectilePivotWorldPosition()
        {
            Ray ray = Camera.main.ScreenPointToRay(m_projectileHoldPivot.position);
            return ray.GetPoint(m_projectileHoldPivotDistance);
        }

        private Vector3 GetNextProjectilePivotWorldPosition()
        {
            Ray ray = Camera.main.ScreenPointToRay(m_projectileNextPivot.position);
            return ray.GetPoint(m_projectileNextPivotDistance);
        }

        private void HandleProjectileInstantiated()
        {
            if (GameLogic.Instance.NextProjectile)
            {
                GameLogic.Instance.NextProjectile.transform.parent = Camera.main.transform;
                GameLogic.Instance.NextProjectile.transform.position = GameLogic.Instance.HoldProjectile ?
                    GetNextProjectilePivotWorldPosition()
                    :
                    GetHoldProjectilePivotWorldPosition();
            }

            if (GameLogic.Instance.HoldProjectile)
            {
                GameLogic.Instance.NextProjectile.transform.parent = Camera.main.transform;
                Vector3 localPos = Camera.main.transform.InverseTransformPoint(GetHoldProjectilePivotWorldPosition());
                GameLogic.Instance.HoldProjectile.transform.DOLocalMove(localPos, 0.2f).SetEase(Ease.InQuad);
            }         

        }        

        private void HandleLevelProgressChanged(float progress)
        {
            m_levelProgressBar.fillAmount = progress;
            m_levelProgressText.text = Mathf.FloorToInt(progress * 100) + "%";
        }
    }
}
