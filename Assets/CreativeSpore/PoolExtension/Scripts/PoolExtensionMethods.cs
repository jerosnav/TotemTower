// Copyright (C) 2018 Creative Spore - All Rights Reserved
using UnityEngine;
using System.Collections;
using CreativeSpore.PoolExtension;

//NOTE: is not in a name space to avoid adding the using in project code

/// <summary>
/// Adds PoolInstantiate and PoolDestroy extension methods to MonoBehaviour so you can call this.PoolInstantiate and this.PoolDestroy 
/// from inside any MonoBehaviour script instead of calling PoolManager.Instanciate or PoolManager.Destroy.
/// </summary>
public static class PoolExtensionMethods
{
    /// <summary>
    /// For testing purposes, if false, the pool will call Object.Instanciate and Object.Destroy instead of the PoolManager methods.
    /// </summary>
    public static bool PoolEnabled
    {
        get { return s_poolEnabled; }
        set
        {
            if (s_poolEnabled != value)
            {
                s_poolEnabled = value;
                if(s_poolEnabled)
                    PoolManager.RemoveNullGameObjects(false);
            }
        }
    }

    private static bool s_poolEnabled = true;

    public static Object PoolInstantiate(this Component value, Object original, Transform parent = null, bool instantiateInWorldSpace = false)
    {
        if (s_poolEnabled)
            return PoolManager.Instantiate(original, parent, instantiateInWorldSpace);
        else
#if UNITY_2017_1_OR_NEWER
            return Object.Instantiate(original, parent, instantiateInWorldSpace);
#else
            return Object.Instantiate(original);
#endif
    }

    public static Object PoolInstantiate(this Component value, Object original, Vector3 position, Quaternion rotation = default(Quaternion), Transform parent = null)
    {
        if (s_poolEnabled)
            return PoolManager.Instantiate(original, position, rotation, parent);
        else
#if UNITY_2017_1_OR_NEWER
            return Object.Instantiate(original, position, rotation, parent);
#else
        return Object.Instantiate(original, position, rotation);
#endif
    }

    public static T PoolInstantiate<T>(this Component value, T original, Transform parent = null, bool instantiateInWorldSpace = false) where T : Object
    {
        return (T)PoolInstantiate(value, (Object)original, parent, instantiateInWorldSpace);
    }

    public static T PoolInstantiate<T>(this Component value, T original, Vector3 position, Quaternion rotation = default(Quaternion), Transform parent = null) where T : Object
    {
        return (T)PoolInstantiate(value, (Object)original, position, rotation, parent);
    }

    public static void PoolDestroy(this Component value, Object obj, float time = 0f)
    {
        if (s_poolEnabled)
            PoolManager.Destroy(obj, time);
        else
            Object.Destroy(obj, time);
    }

    public static void PoolDestroyImmediate(this Component value, Object obj, float time = 0f)
    {
        if (s_poolEnabled)
            PoolManager.DestroyImmediate(obj);
        else
            Object.DestroyImmediate(obj);
    }
}

/// <summary>
/// Extension Methods for Unity Static Methods
/// </summary>
public static class PoolAudioSource
{
    private static PoolBehaviour s_oneShotPool;
    private static GameObject s_prefab;

    /// <summary>
    /// Plays an AudioClip at a given position in world space. (Copy AudioSource.PlayClipAtPoint but using a Pool)
    /// </summary>
    /// <param name="clip">Audio data to play.</param>
    /// <param name="position">Position in world space from which sound originates.</param>
    /// <param name="volume">Playback volume.</param>
    /// <param pitch="volume">Playback pitch.</param>
    public static AudioSource PlayClipAtPoint(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
    {
        AudioSource audioSource;
        if (!s_oneShotPool)
        {
            s_prefab = new GameObject("One shot audio");
            s_prefab.transform.position = position;
            audioSource = (AudioSource)s_prefab.AddComponent(typeof(AudioSource));
            s_oneShotPool = PoolManager.CreatePool(s_prefab);
            s_prefab.hideFlags = HideFlags.HideAndDontSave;
        }

        GameObject gameObject = s_oneShotPool.Instantiate(position);
        audioSource = gameObject.GetComponent<AudioSource>();            

        audioSource.clip = clip;
        audioSource.spatialBlend = 1f;
        audioSource.pitch = pitch;
        audioSource.volume = volume;
        audioSource.outputAudioMixerGroup = null;
        audioSource.Play();
        float timeToLive = clip.length * Mathf.Max(Time.timeScale, 0.01f) / Mathf.Max(Mathf.Abs(pitch), 0.1f);
        s_oneShotPool.Destroy(gameObject, timeToLive);
        return audioSource;
    }

    /// <summary>
    /// 2D version of PlayClipAtPoint using a spatialBlend of 0f.
    /// </summary>
    /// <param name="clip">Audio data to play.</param>
    /// <param name="position">Position in world space from which sound originates.</param>
    /// <param name="volume">Playback volume.</param>
    /// <param pitch="volume">Playback pitch.</param>
    /// <returns></returns>
    public static AudioSource PlayClipAtPoint2D(AudioClip clip, Vector3 position = default(Vector3), float volume = 1f, float pitch = 1f)
    {
        AudioSource audioSource = PlayClipAtPoint(clip, position, volume, pitch);
        audioSource.spatialBlend = 0f;
        return audioSource;
    }

    /// <summary>
    /// Playing a random clip from the clips array
    /// </summary>
    public static AudioSource PlayClipAtPoint(AudioClip[] clips, Vector3 position, float volume = 1f, float pitch = 1f)
    {
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        return PlayClipAtPoint(clip, position, volume, pitch);
    }

    /// <summary>
    /// Playing a random clip from the clips array
    /// </summary>
    public static AudioSource PlayClipAtPoint2D(AudioClip[] clips, Vector3 position = default(Vector3), float volume = 1f, float pitch = 1f)
    {
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        return PlayClipAtPoint2D(clip, position, volume, pitch);
    }
}
