// Copyright (C) 2018 Creative Spore - All Rights Reserved
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace CreativeSpore
{
    public class UIDragInputHandler : UIInputHandlerBase, IDragHandler
    {
        [SerializeField]
        private EventBlock m_eventBlock = new EventBlock();

        public EventBlock Events => m_eventBlock;

        public delegate void OnDragDelegate(PointerEventData eventData);
        public OnDragDelegate OnDragPerformed = delegate { };


        public void OnDrag(PointerEventData eventData)
        {
            if (CheckConditions(eventData))
            {
                OnDragPerformed.Invoke(eventData);
                Vector2 delta = eventData.delta;
                m_eventBlock.onDragEvent.Invoke(delta);
                if (delta.x != 0f)
                    m_eventBlock.onDragHorizontal.Invoke(delta.x);
                if (delta.y != 0f)
                    m_eventBlock.onDragVertical.Invoke(delta.y);
            }
        }

        private bool CheckConditions(PointerEventData eventData)
        {
            if(Input.touchSupported)
            {
                return Input.touchCount == 1;
            }
            else
            {
                return eventData.button == PointerEventData.InputButton.Left && !Input.GetMouseButton(1) && !Input.GetMouseButton(2);
            }
        }

        [System.Serializable]
        public class EventBlock
        {
            [System.Serializable]
            public class Vector2Event : UnityEvent<Vector2> { }
            [System.Serializable]
            public class FloatEvent : UnityEvent<float> { }

            public Vector2Event onDragEvent;
            public FloatEvent onDragHorizontal;
            public FloatEvent onDragVertical;
        }
    }
}
