// Copyright (C) 2018 Creative Spore - All Rights Reserved
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CreativeSpore.DebuggingTools
{
	public class DebugWidgetBase : MonoBehaviour 
	{
        public bool DisplayInReleaseBuild = false;

        protected virtual void Start()
        {
            if(!Debug.isDebugBuild && !DisplayInReleaseBuild)
            {
                Destroy(gameObject);
            }
        }

        protected virtual void Update () 
		{
			
		}
	}
}
