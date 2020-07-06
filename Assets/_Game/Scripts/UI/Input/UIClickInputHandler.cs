// Copyright (C) 2018 Creative Spore - All Rights Reserved
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace CreativeSpore
{
    public class UIClickInputHandler : UIInputHandlerBase, IPointerClickHandler, IDragHandler
    {
        [SerializeField]
        private EventBlock m_eventBlock = new EventBlock();

        public EventBlock Events => m_eventBlock;

        public delegate void OnClickDelegate(PointerEventData eventData);
        public OnClickDelegate OnClickPerformed = delegate { };

        public float clickThreshold = 0.1f;
        public float doubleClickTime = 0.2f;

        private float m_prevClickTime;
        private bool m_isDoubleClick;
        private bool m_isThresholdExceeded;

        public bool IsDoubleClick { get { return m_isDoubleClick; } }

        public void OnPointerClick(PointerEventData eventData)
        {
            if(CheckClickWithThreshold(eventData))
            {
                float time = Time.unscaledTime;
                m_isDoubleClick = (time - m_prevClickTime) <= doubleClickTime;
                m_prevClickTime = time;
                OnClickPerformed.Invoke(eventData);
                m_eventBlock.onClickEvent.Invoke(eventData.position);
            }
            m_isThresholdExceeded = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            m_isThresholdExceeded |= Vector2.Distance(eventData.position, eventData.pressPosition) > clickThreshold;
        }

        public bool CheckClickWithThreshold(PointerEventData eventData)
        {            
            return !m_isThresholdExceeded;
        }

        [System.Serializable]
        public class EventBlock
        {
            [System.Serializable]
            public class Vector2Event : UnityEvent<Vector2> { }
            [System.Serializable]
            public class FloatEvent : UnityEvent<float> { }

            public Vector2Event onClickEvent;
        }

    }
}
