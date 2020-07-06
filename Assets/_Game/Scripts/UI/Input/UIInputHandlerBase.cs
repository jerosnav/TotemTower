// Copyright (C) 2018 Creative Spore - All Rights Reserved
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CreativeSpore
{
	public class UIInputHandlerBase : MonoBehaviour 
	{
		public int TouchCount { get { return Input.touchSupported ? Input.touchCount : 1; } }
	}
}
