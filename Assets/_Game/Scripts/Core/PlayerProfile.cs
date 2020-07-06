// Copyright (C) 2018 Creative Spore - All Rights Reserved
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CreativeSpore
{
    public static class PlayerProfile
    {
        public static int PlayerLevel
        {
            get => PlayerPrefs.GetInt("PlayerLevel", 1);
            set => PlayerPrefs.SetInt("PlayerLevel", value);
        }
        public static int LevelSeed 
        {
            get => PlayerPrefs.GetInt("LevelSeed", (int)(Random.value * int.MaxValue));
            set => PlayerPrefs.SetInt("LevelSeed", value);
        }

        public static float SavedLevelProgress
        {
            get => PlayerPrefs.GetFloat("SavedLevelProgress", 0f);
            set => PlayerPrefs.SetFloat("SavedLevelProgress", value);
        }
    }
}
