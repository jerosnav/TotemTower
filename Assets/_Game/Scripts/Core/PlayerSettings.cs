// Copyright (C) 2018 Creative Spore - All Rights Reserved
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Schema;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace CreativeSpore
{
	public class PlayerSettings : SingletonBehaviour<PlayerSettings> 
	{
		[SerializeField] bool m_initializeOnLoad = false;
		[SerializeField] AudioMixer m_audioMixer = null;

		private static string[] s_soundsPrefs = null;

		private void OnValidate()
		{
			if(!m_audioMixer)
				m_audioMixer = Resources.FindObjectsOfTypeAll<AudioMixer>().FirstOrDefault();
			InitializeSettings();
		}

		private void Start()
		{
			//NOTE: Volume is not updated if this is called in Awake
			if(m_initializeOnLoad)
				InitializeSettings();			
		}

        public float Sounds_Global { get => GetSound(Sounds.Global); set => SetSound(Sounds.Global, value); }
		public float Sounds_Music { get => GetSound(Sounds.Music); set => SetSound(Sounds.Music, value); }
		public float Sounds_Ambient { get => GetSound(Sounds.Ambient); set => SetSound(Sounds.Ambient, value); }
		public float Sounds_Effects { get => GetSound(Sounds.Effects); set => SetSound(Sounds.Effects, value); }
		public ScreenResolution Screen_Resolution { get => GetScreenResolution(); set => SetScreenResolutution(value);}		
		public bool Device_Vibration 
		{ 
			get => PlayerPrefs.GetInt("Device_Vibration", 1) != 0; 
			set => PlayerPrefs.SetInt("Device_Vibration", value? 1 : 0);
		}

		public enum Sounds
		{
			Global,
			Music,
			Ambient,
			Effects,
		}

		public enum ScreenResolution
        {
			Full,
			Half,
        }

		public void SwitchScreenResolution()
		{
			SetScreenResolutution(1 - Screen_Resolution);
		}

		private float GetSound(Sounds sound)
		{
			return PlayerPrefs.GetFloat(s_soundsPrefs[(int)sound], 1f);
		}

		private void SetSound(Sounds sound, float value)
		{
			PlayerPrefs.SetFloat(s_soundsPrefs[(int)sound], value);
			//ref:https://gamedevbeginner.com/the-right-way-to-make-a-volume-slider-in-unity-using-logarithmic-conversion/
			m_audioMixer?.SetFloat(sound.ToString(), Mathf.Log10(value) * 20f); // this will set the attenuation properly from -80 to 0
		}

		private ScreenResolution GetScreenResolution()
        {
			string screenResolution = PlayerPrefs.GetString("Screen_Resolution", "Full");
			return (ScreenResolution)System.Enum.Parse(typeof(ScreenResolution), screenResolution);
		}

		private void SetScreenResolutution(ScreenResolution value)
		{
			PlayerPrefs.SetString("Screen_Resolution", value.ToString());
			if (value == ScreenResolution.Half)
				Screen.SetResolution(Display.main.systemWidth / 2, Display.main.systemHeight / 2, Screen.fullScreen, 60);
			else
				Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, Screen.fullScreen, 60);
		}

		private void InitializeSettings()
		{
			// Initialize Screen Settings
			Application.targetFrameRate = 99999999;
			QualitySettings.vSyncCount = 0;
			SetScreenResolutution(Screen_Resolution);
			Screen.sleepTimeout = SleepTimeout.NeverSleep;

			// Initialize Sound Settings
			InitializeEnumPrefs<Sounds>(ref s_soundsPrefs);
			foreach (var sound in typeof(Sounds).GetEnumValues().Cast<Sounds>())
			{
				SetSound(sound, GetSound(sound));
			}
		}

		private void InitializeEnumPrefs<T>(ref string[] prefs) where T : System.Enum
		{
			if (prefs == null || prefs.Length == 0)
			{
				string enumName = typeof(T).Name;
				prefs = System.Enum.GetNames(typeof(T)).Select(o => "Settings/" + enumName + "/" + o).ToArray();
			}
		}
	}
}
