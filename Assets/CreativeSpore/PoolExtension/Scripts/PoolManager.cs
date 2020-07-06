// Copyright (C) 2018 Creative Spore - All Rights Reserved
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CreativeSpore.PoolExtension
{
    public static class PoolManager
    {
        private static Dictionary<GameObject, PoolBehaviour> s_instantiatePoolLookupDic = new Dictionary<GameObject, PoolBehaviour>(); // prefab -> PoolBehaviour (Should be an instance per pool)
        private static Dictionary<GameObject, PoolBehaviour> s_destroyPoolLookupDic = new Dictionary<GameObject, PoolBehaviour>(); // instance GameObject -> PoolBehaviour (Should be an instance per gameObject reused by all the pools)
        private static GameObject s_poolManagerObj = null;        

        /// <summary>
        /// Finds and return a Pool using the prefab or creates a new one if needed.
        /// </summary>
        /// <param name="prefab"></param>
        /// <returns></returns>
        public static PoolBehaviour FindOrCreatePool(GameObject prefab)
        {
            if(!prefab)
            {
                Debug.LogError("Prefab cannot be null!");
                return null;
            }
            PoolBehaviour poolBhv = null;
            if (!s_instantiatePoolLookupDic.TryGetValue(prefab, out poolBhv) || !poolBhv)
            {
                bool isPrefab = !prefab.scene.IsValid();
                if(isPrefab)
                    poolBhv = CreatePool(prefab);
            }
            return poolBhv;
        }

        /// <summary>
        /// Registers a pool in the pool manager.
        /// </summary>
        /// <param name="poolBhv"></param>
        public static void RegisterPool(PoolBehaviour poolBhv)
        {
            if (poolBhv.Prefab)
            {
                s_instantiatePoolLookupDic[poolBhv.Prefab] = poolBhv;
            }
        }

        /// <summary>
        /// Registers an object in a pool.
        /// </summary>
        /// <param name="poolBhv"></param>
        /// <param name="obj"></param>
        public static void RegisterObjInPool(PoolBehaviour poolBhv, GameObject obj)
        {
            s_destroyPoolLookupDic[obj] = poolBhv;
        }

        /// <summary>
        /// Gets how many active instances belonging to the prefab are there.
        /// </summary>
        /// <param name="original"></param>
        /// <returns></returns>
        public static int GetActiveInstances(Object original)
        {
            GameObject go = GetObjectAsGameObject(original);
            PoolBehaviour poolBhv = FindOrCreatePool(go);
            if (poolBhv)
            {
                return poolBhv.ActiveCount;
            }
            return 0;
        }

        /// <summary>
        /// Gets the number of available instances are in the pool cache.
        /// </summary>
        /// <param name="original"></param>
        /// <returns></returns>
        public static int GetCacheSize(Object original)
        {
            GameObject go = GetObjectAsGameObject(original);
            PoolBehaviour poolBhv = FindOrCreatePool(go);
            if (poolBhv)
            {
                return poolBhv.CacheSize;
            }
            return 0;
        }

        /// <summary>
        /// Generates a cache of instances for the prefab (creating a new pool if needed).
        /// </summary>
        /// <param name="original"></param>
        /// <param name="count"></param>
        public static void PreCacheInstances(Object original, int count)
        {
            count = Mathf.Max(0, count);
            if (original is Component || original is GameObject)
            {
                GameObject go = GetObjectAsGameObject(original);
                PoolBehaviour poolBhv = FindOrCreatePool(go);
                if (poolBhv)
                {
                    poolBhv.PreCacheInstances(count);
                }
            }
        }

        /// <summary>
        /// Instantiates a new instance using the prefab pool or creating a new pool if needed.
        /// </summary>
        /// <param name="original"></param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static Object Instantiate(Object original, Vector3 position, Quaternion rotation = default(Quaternion), Transform parent = null)
        {
            if (!original)
            {
                Debug.LogError("original cannot be null!");
                return null;
            }
            if (original is Component || original is GameObject)
            {
                GameObject go = GetObjectAsGameObject(original);
                PoolBehaviour poolBhv = FindOrCreatePool(go);
                if (poolBhv)
                {
                    Object obj = original is Component ?
                        poolBhv.Instantiate(original.GetType(), position, rotation, parent) as Object
                        :
                        poolBhv.Instantiate(position, rotation, parent) as Object
                        ;
                    return obj;
                }
                else
                {
#if UNITY_2017_1_OR_NEWER
                    return Object.Instantiate(original, position, rotation, parent);
#else
                    return Object.Instantiate(original, position, rotation);
#endif
                }
            }
            else
            {
#if UNITY_2017_1_OR_NEWER
                    return Object.Instantiate(original, position, rotation, parent);
#else
                    return Object.Instantiate(original, position, rotation);
#endif
            }
        }

        public static Object Instantiate(Object original, Transform parent = null, bool instantiateInWorldSpace = false)
        {
            if (!original)
            {
                Debug.LogError("original cannot be null!");
                return null;
            }
            if (original is Component || original is GameObject)
            {
                GameObject go = GetObjectAsGameObject(original);
                PoolBehaviour poolBhv = FindOrCreatePool(go);
                if (poolBhv)
                {
                    Object obj = original is Component ?
                        poolBhv.Instantiate(original.GetType(), parent, instantiateInWorldSpace) as Object
                        :
                        poolBhv.Instantiate(parent, instantiateInWorldSpace) as Object
                        ;
                    return obj;
                }
                else
                {
#if UNITY_2017_1_OR_NEWER
                    return Object.Instantiate(original, parent, instantiateInWorldSpace);
#else
                    return Object.Instantiate(original);
#endif

                }
            }
            else
            {
#if UNITY_2017_1_OR_NEWER
                    return Object.Instantiate(original, parent, instantiateInWorldSpace);
#else
                    return Object.Instantiate(original);
#endif
            }
        }

        /// <summary>
        /// Instantiates a new instance using the prefab pool or creating a new pool if needed.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="original"></param>
        /// <param name="parent"></param>
        /// <param name="instantiateInWorldSpace"></param>
        /// <returns></returns>
        public static T Instantiate<T>(T original, Transform parent = null, bool instantiateInWorldSpace = false) where T : Object
        {
            return (T)Instantiate(original, parent, instantiateInWorldSpace);
        }

        /// <summary>
        /// Destroys the instance using the Pool (disabling the instance if it was belonging to a pool)
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="time"></param>
        public static void Destroy(Object obj, float time = 0f)
        {
            PoolBehaviour poolBhv;
            GameObject go = GetObjectAsGameObject(obj);
            if (s_destroyPoolLookupDic.TryGetValue(go, out poolBhv))
            {
                poolBhv.TryDestroy(go, time);
            }
            else
            {
                //Debug.LogWarning("Pool for prefab " + objToDestroy.name + " was not created using CreateObjectFactory ");
                Object.Destroy(obj, time);
            }
        }

        /// <summary>
        /// Destroys the instance using the Pool (disabling the instance if it was belonging to a pool). It destroys the instance immediately, not waiting to the next Pool Update.
        /// </summary>
        /// <param name="obj"></param>
        public static void DestroyImmediate(Object obj)
        {
            Destroy(obj, -1f);
        }

        /// <summary>
        /// Destroys all the instances in all the Pools.
        /// </summary>
        public static void DestroyAll()
        {
            var iter = s_instantiatePoolLookupDic.GetEnumerator();
            while(iter.MoveNext())
            {
                PoolBehaviour objFactoryBhv = iter.Current.Value;
                if (objFactoryBhv) objFactoryBhv.DestroyAll();
            }
        }

        /// <summary>
        /// Fix null references in the Pools when an intance was destroyed using Object.Destroy instead using the PoolManager.Destroy or this.PoolDestroy methods.
        /// </summary>
        /// <param name="warningLogEnabled"></param>
        public static void RemoveNullGameObjects(bool warningLogEnabled = true)
        {
            var iter = s_instantiatePoolLookupDic.GetEnumerator();
            while (iter.MoveNext())
            {
                PoolBehaviour objFactoryBhv = iter.Current.Value;
                if (objFactoryBhv) objFactoryBhv.RemoveNullGameObjects(warningLogEnabled);
            }
        }

        /// <summary>
        /// Destroys all the instances in a pool using the prefab.
        /// </summary>
        /// <param name="original"></param>
        /// <returns></returns>
        public static bool DestroyAll(Object original)
        {
            PoolBehaviour objFactoryBhv;
            if (s_instantiatePoolLookupDic.TryGetValue(GetObjectAsGameObject(original), out objFactoryBhv))
            {
                if (objFactoryBhv) objFactoryBhv.DestroyAll();
                return true;
            }
            else
            {
                //Debug.LogWarning("Pool for prefab " + prefab.name + " was not created using CreateObjectFactory ");
                return false;
            }
        }

        /// <summary>
        /// Manually creates a Pool for a prefab.
        /// </summary>
        /// <param name="prefab"></param>
        /// <returns></returns>
        public static PoolBehaviour CreatePool(GameObject prefab)
        {
            CheckInitialize();

            GameObject obj = new GameObject("Pool: " + prefab.name);
            obj.transform.SetParent(s_poolManagerObj.transform);
            PoolBehaviour bhv = obj.AddComponent<PoolBehaviour>();
            bhv.Prefab = prefab;
            bhv.Initialize();            
            return bhv;
        }

        // Pool works only with Components and GameObjects, but to change the transform is easy to use the GameObject
        private static GameObject GetObjectAsGameObject(Object obj)
        {
            if (obj is GameObject) return obj as GameObject;
            else if (obj is Component) return (obj as Component).gameObject;
            else return null;
        }

        private static void CheckInitialize()
        {
            if (s_poolManagerObj == null)
            {
                s_poolManagerObj = GameObject.Find("PoolManager");
                if (s_poolManagerObj == null)
                {
                    s_poolManagerObj = new GameObject("PoolManager");
                }
            }
        }
    }
}
