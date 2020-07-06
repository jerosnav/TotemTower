// Copyright (C) 2018 Creative Spore - All Rights Reserved
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CreativeSpore
{
    [DefaultExecutionOrder(-10000)]
    public class SingletonBehaviour<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance { get; private set; }
		protected virtual void Awake()
        {
            if (!Instance)
                Instance = this as T;
            else
            {
                Debug.LogError("Only a single instance of " + GetType() + " is allowed.");
                DestroyImmediate(gameObject);
            }
        }

        public class WaitForInitialization : CustomYieldInstruction
        {
            public override bool keepWaiting
            {
                get
                {
                    return !SingletonBehaviour<T>.Instance;
                }
            }            
        }
    }
}
