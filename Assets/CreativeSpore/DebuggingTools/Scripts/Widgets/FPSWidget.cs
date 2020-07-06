// Copyright (C) 2018 Creative Spore - All Rights Reserved
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CreativeSpore.DebuggingTools
{
	public class FPSWidget : DebugWidgetBase 
	{
        public Text FPSText;

        float m_dt = 0.0f;

        protected override void Update()
        {            
            m_dt += (Time.unscaledDeltaTime - m_dt) * 0.1f;
            float msec = m_dt * 1000.0f;
            float fps = 1.0f / m_dt;
            string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
            FPSText.text = text;
        }
    }
}
