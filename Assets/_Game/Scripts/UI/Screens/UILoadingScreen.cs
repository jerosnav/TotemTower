// Copyright (C) 2018 Creative Spore - All Rights Reserved
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CreativeSpore
{
    public class UILoadingScreen : MonoBehaviour 
    {
        [SerializeField]
        private TextMeshProUGUI  m_progressText = null;
        [SerializeField]
        private Image m_filledProgressBar = null;

        IEnumerator Start()
        {
            m_filledProgressBar.fillAmount = 0f;
            m_progressText.text = "";
            yield return new WaitForSeconds(2f);
            var asyncLoad = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex + 1);
            asyncLoad.allowSceneActivation = false;
            float progress = 0f;
            while (progress < 1f)
            {
                progress += (asyncLoad.progress + 0.01f - progress) * 2f * Time.deltaTime;
                if(progress >= .9f)
                {                    
                    progress = 1f;
                }
                m_filledProgressBar.fillAmount = progress;
                m_progressText.text = Mathf.RoundToInt(progress * 100) + "%";
                yield return null;
            }
            yield return new WaitForSeconds(1f);
            asyncLoad.allowSceneActivation = true;
        }
    }
}
