// Copyright (C) 2018 Creative Spore - All Rights Reserved
using UnityEngine;
using System.Collections;

//namespace CreativeSpore.PoolExtension
//{
    /// <summary>
    /// Used before a field to make this field be restored with prefab value when the instance is re-enabled by the pool.
    /// </summary>
    public sealed class PoolRestoreValue : System.Attribute
    {
    }

    /// <summary>
    /// Used before a MonoBehaviour definition to make all fields be restored with prefab value when the instance is re-enabled by the pool.
    /// </summary>
    public sealed class PoolRestoreAllValues : System.Attribute
    {
    }
//}
