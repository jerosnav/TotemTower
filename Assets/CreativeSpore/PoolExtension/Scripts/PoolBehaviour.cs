// Copyright (C) 2018 Creative Spore - All Rights Reserved

// For debugging purposes, displays a warning when you destroy a pool object using Object.Destroy instead of PoolBehaviour.Destroy.
//#define CHECK_FOR_NULL_OBJECTS

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CreativeSpore.PoolExtension
{
    [AddComponentMenu("Pool Manager/Pool Behaviour")]
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public class PoolBehaviour : MonoBehaviour
    {
        /// <summary>
        /// The prefab used to instantiate gameObjects with this pool
        /// </summary>
        public GameObject Prefab
        {
            get { return m_prefab; }
            set {
#if UNITY_EDITOR
                Debug.Assert(!m_prefab, "PoolBehaviour Prefab was already set. You cannot change a PoolBehaviour prefab once it was set.");
                Debug.Assert(value, "PoolBehaviour Prefab cannot be null.");
                //Debug.Assert(isPrefab, "PoolBehaviour Prefab should be a prefab gameObject, not an scene gameObject.");
#endif
                //NOTE: removed !isPrefab special case (fix issues with AudioSource extension when destroying AudioSource instances)
                //bool isPrefab = !value.scene.IsValid(); //UnityEditor.PrefabUtility.GetPrefabParent(value) == null && UnityEditor.PrefabUtility.GetPrefabObject(value.transform) != null;
                if (!m_prefab && value)
                {
                    m_prefab = value;
                    //if (isPrefab)
                        PoolManager.RegisterPool(this);
                    /*
                    else // Special Case: using a gameObject as prefab
                    {
                        m_prefab.transform.parent = this.transform;
                        m_active.Add(m_prefab);
                    }
                    */
                    Initialize();
                }
            }
        }

        /// <summary>
        /// How many instances are active in this pool
        /// </summary>
        public int ActiveCount { get { return m_active.Count; } }
        /// <summary>
        /// How many instances are available in the pool to be activated without instantiating a new instance
        /// </summary>
        public int CacheSize { get { return m_poolCache.Count + m_pool.Count; } }

        [SerializeField]
        private GameObject m_prefab = null;                       

        [SerializeField]
        private List<GameObject> m_pool = new List<GameObject>(50);
        [SerializeField]
        private List<GameObject> m_poolCache = new List<GameObject>(); //Used for cached objects to avoid calling Start twice
        [SerializeField]
        private List<GameObject> m_active = new List<GameObject>(50);
        private List<GameObject> m_justInstantiated = new List<GameObject>();
        private Vector3 m_prefabPosition;
        private Quaternion m_prefabRotation;
        private Vector3 m_prefabLocalScale;
        private Dictionary<GameObject, float> m_delayedDestroyDic = new Dictionary<GameObject, float>(); // dictionary for gameObject that should be destroyed after a time delay
        private GameObjectComponentValues[] m_prefabCompValues; // cache with all the prefab reflection data to restore the components values when a gameObject is enabled by the pool
        private Dictionary<GameObject, ComponentCache> m_dicGoComp = new Dictionary<GameObject, ComponentCache>(); // component cache for each gameObject managed by the pool

        // temporal static lists to avoid garbage allocation
        private static List<GameObject> s_tempGoList = new List<GameObject>();
        private static List<GameObjectComponentValues> s_tempGoCompValueList = new List<GameObjectComponentValues>();
        private static List<Component> s_tempCompList = new List<Component>();

        public void Initialize()
        {
            if (m_prefab)
            {
                InitializeGameObjectComponentValues(m_prefab);
                m_prefabPosition = m_prefab.transform.localPosition;
                m_prefabRotation = m_prefab.transform.localRotation;
                m_prefabLocalScale = m_prefab.transform.localScale;
            }
        }

        //NOTE: Awake is not called after compiling code but cache data is lost
        private void OnEnable()
        {
            Initialize();
            // pre-cache the components
            CacheObjComponents(m_pool);
            CacheObjComponents(m_poolCache);
            CacheObjComponents(m_active);            
            PoolManager.RegisterPool(this);
        }        

        #region MONOBEHAVIOUR MESSAGES
        private void OnValidate()
        {
            if(m_prefab)
            {
                name = "Pool: " + m_prefab.name;
            }

        }

        void Update()
        {
            for (int i = 0; i < m_justInstantiated.Count; ++i)
            {
                m_justInstantiated[i].BroadcastMessage("Start", SendMessageOptions.DontRequireReceiver);
            }
            m_justInstantiated.Clear();

#if UNITY_EDITOR && CHECK_FOR_NULL_OBJECTS
            RemoveForNullObjects();
#endif
        }

        void LateUpdate()
        {
            s_tempGoList.Clear();
            var iter = m_delayedDestroyDic.GetEnumerator();
            while(iter.MoveNext())
            {
                if(iter.Current.Value <= Time.time)
                {
                    s_tempGoList.Add(iter.Current.Key);                        
                }
            }

            for (int i = 0, s = s_tempGoList.Count; i < s; ++i)
            {
                GameObject obj = s_tempGoList[i];
                m_delayedDestroyDic.Remove(obj);
                if(obj)
                    Destroy(obj, -1f);
            }
        }
        #endregion

        /// <summary>
        /// This will destroy all the children gameObjects and instantiate as many prefab instances in the pool as the count value.
        /// </summary>
        /// <param name="count"></param>
        public void PreCacheInstances( int count )
        {
            count = Mathf.Max(count, 0);
            while (transform.childCount > 0)
                DestroyImmediate(transform.GetChild(0).gameObject);
            m_pool.AddRange(m_poolCache);
            for (int i = 0, s = m_pool.Count; i < s; ++i)
            {
                m_dicGoComp.Remove(m_pool[i]);
            }
            m_pool.Clear();
            for (int i = 0; i < count; ++i)
            {
                GameObject obj = Instantiate();
                CacheObjComponents(obj);
            }
            DestroyAll();
            m_poolCache.AddRange(m_pool);
            m_pool.Clear();
            m_justInstantiated = new List<GameObject>(m_poolCache.Count);
            m_delayedDestroyDic = new Dictionary<GameObject, float>(m_poolCache.Count);
            s_tempGoList = new List<GameObject>(m_poolCache.Count);
        }

        /// <summary>
        /// This method should be called by PoolBehaviourEditor
        /// </summary>
        public bool TryAddGameObjectToPool(GameObject obj)
        {
            if (!m_dicGoComp.ContainsKey(obj))
            {
                if (obj.activeInHierarchy)
                    m_active.Add(obj);
                else
                    m_pool.Add(obj);
                CacheObjComponents(obj);
                return true;
            }
            return false;
        }


        /// <summary>
        /// If an instance of the pool is destroyed using Object.Destroy, instead of using this.PoolDestroy, the instance will become null.
        /// This will force a cleaning of the pool removing all the null instances.
        /// </summary>
        /// <param name="warningLogEnabled"></param>
        public void RemoveNullGameObjects(bool warningLogEnabled = true)
        {
            RemoveNullGameObjects(m_pool, warningLogEnabled, "Pool");
            RemoveNullGameObjects(m_poolCache, warningLogEnabled, "PoolCache");
            RemoveNullGameObjects(m_active, warningLogEnabled, "Active");            
        }

        private void RemoveNullGameObjects(List<GameObject> list, bool warningLogEnabled, string listName)
        {
            for (int i = 0; i < list.Count; ++i)
            {
                if (list[i] == null)
                {
                    if (warningLogEnabled)
                        Debug.LogWarning(name + ": Found null object in " + listName + " List. You should call PoolManager.Destroy for objects created from the pool!");
                    list.RemoveAt(i--);
                }
            }
        }

        private GameObject PeekFromPool(List<GameObject> poolList)
        {
            GameObject obj = null;
            int whileLoops = 0;
            while (!obj && poolList.Count > 0) // just in case an object is removed
            {
                ++whileLoops;
                obj = poolList[poolList.Count - 1];
                poolList.RemoveAt(poolList.Count - 1);
            }
            if (whileLoops > 1)
                Debug.LogWarning(name + ": Found null object in Pool List. You should call PoolManager.Destroy for objects created from the pool!");
            return obj;
        }

        //TODO: add a max elements & option to destroy the last created element if there no more available
        /// <summary>
        /// Instantiate a gameObject using the pool. The prefab property will be used as template.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="parent"></param>
        /// <param name="instantiateInWorldSpace"></param>
        /// <returns></returns>
        public GameObject Instantiate(Vector3 position, Quaternion rotation = default(Quaternion), Transform parent = null, bool instantiateInWorldSpace = false)
        {
            GameObject obj = PeekFromPool(m_poolCache);
            bool isCached = obj != null;
            if (!obj) obj = PeekFromPool(m_pool);
            if (obj != null)
            {
                obj.transform.localPosition = position;
                obj.transform.localRotation = rotation;
                obj.transform.localScale = m_prefabLocalScale;
                RestorePrefabValues(obj); // this needs to be called before SetActive to call Awake method before OnEnable
                obj.SetActive(true);
                if(!isCached)
                    m_justInstantiated.Add(obj);                
                //NOTE: Only difference here is, Awake will be called after OnEnable
                //obj.BroadcastMessage("Awake", SendMessageOptions.DontRequireReceiver);
            }
            else
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    obj = UnityEditor.PrefabUtility.InstantiatePrefab(m_prefab) as GameObject;
                    obj.transform.localPosition = position;
                    obj.transform.localRotation = rotation;
                }
                else
                {
                    obj = Object.Instantiate(m_prefab, position, rotation) as GameObject;
                }
#else
                obj = Object.Instantiate(m_prefab, position, rotation) as GameObject;
#endif
                // Fix set transform in UI gameObjects (This is not done by Unity Instantiate)
                if(obj.transform is RectTransform)
                {
                    (obj.transform as RectTransform).anchoredPosition3D = position;
                }
                obj.transform.SetParent(transform);
                PoolManager.RegisterObjInPool(this, obj);
            }
            m_active.Add(obj);
            if (parent != null)
            {
                obj.transform.SetParent(parent, instantiateInWorldSpace);
            }
            return obj;
        }
        public GameObject Instantiate(Transform parent = null, bool instantiateInWorldSpace = false)
        {
            return Instantiate( m_prefabPosition, m_prefabRotation, parent, instantiateInWorldSpace);
        }

        public Component Instantiate(System.Type componentType, Transform parent = null, bool instantiateInWorldSpace = false)
        {
            GameObject obj = Instantiate(parent, instantiateInWorldSpace) as GameObject;
            return obj.GetComponent(componentType); //TODO: cache components
        }

        public Component Instantiate(System.Type componentType, Vector3 position, Quaternion rotation = default(Quaternion), Transform parent = null, bool instantiateInWorldSpace = false)
        {
            GameObject obj = Instantiate(position, rotation, parent, instantiateInWorldSpace) as GameObject;
            return obj.GetComponent(componentType); //TODO: cache components
        }

        public void Destroy(GameObject obj, float time = 0f)
        {
            if (!TryDestroy(obj, time) && obj.activeSelf)
            {
                Debug.LogWarning("Object " + obj.name + " not found in the active pool!");
            }
        }

        public bool TryDestroy(GameObject obj, float time = 0f)
        {
            if (time >= 0f)
            {
                //NOTE: Unity Object.Destroy method doesn't overwrite the time
                m_delayedDestroyDic[obj] = Time.time + time;
                return true;
            }
            else if (m_active.Remove(obj))
            {
                RecycleObj(obj);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Destroy all the active instances of the pool. The instances will be deactivated, not removed from memory.
        /// </summary>
        public void DestroyAll()
        {
            for (int i = 0, s = m_active.Count; i < s; ++i)
            {
                GameObject obj = m_active[i];
                if(obj)
                    RecycleObj(obj);
            }
            m_active.Clear();
        }

        private void RecycleObj(GameObject obj)
        {
            //NOTE: the order of OnDestroy and OnDisable is changed because BroadcastMessage won't work after the gameObject is disabled
            obj.BroadcastMessage("OnDestroy", SendMessageOptions.DontRequireReceiver);
            obj.SetActive(false);
            m_justInstantiated.Remove(obj);
            m_pool.Add(obj);
            obj.transform.SetParent(gameObject.transform);
        }

        #region REFLECTION COMPONENT VALUE RESTORING

        private struct GameObjectComponentValues
        {
            public System.Type type;
            public KeyValuePair<FieldInfo, System.Object>[] fieldValueArr;
            public KeyValuePair<PropertyInfo, System.Object>[] propertyValueArr;
            public MethodInfo awakeMethod;
            public bool IsEmpty { get { return awakeMethod == null && (fieldValueArr.Length | propertyValueArr.Length) == 0; } }

            private static List<KeyValuePair<FieldInfo, System.Object>> s_tempFieldValueList = new List<KeyValuePair<FieldInfo, object>>();
            private static List<KeyValuePair<PropertyInfo, System.Object>> s_tempPropertyValueList = new List<KeyValuePair<PropertyInfo, object>>();
            public GameObjectComponentValues(Component component)
            {
                //Debug.Log("<color=green> PrefabComponentData " + component + "</color>");
                type = component.GetType();
                s_tempFieldValueList.Clear();
                s_tempPropertyValueList.Clear();

                bool reloadAllFields = type.IsDefined(typeof(PoolRestoreAllValues), false);

                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                //Debug.LogWarning("Fields " + fields.Length);
                for (int i = 0, s = fields.Length; i < s; ++i)
                {
                    var field = fields[i];
                    if (reloadAllFields || field.IsDefined(typeof(PoolRestoreValue), false))
                    {
                        //Debug.Log(field.Name);
                        s_tempFieldValueList.Add(new KeyValuePair<FieldInfo, System.Object>(field, field.GetValue(component)));
                    }
                }
                var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                properties = properties.Where(x => x.CanWrite).ToArray();
                //Debug.LogWarning("Properties " + properties.Length);
                for (int i = 0, s = properties.Length; i < s; ++i)                    
                {
                    var property = properties[i];
                    if (reloadAllFields || property.IsDefined(typeof(PoolRestoreValue), false))
                    {
                        //Debug.Log(property.Name);
                        s_tempPropertyValueList.Add(new KeyValuePair<PropertyInfo, System.Object>(property, property.GetValue(component, null)));
                    }
                }

                fieldValueArr = s_tempFieldValueList.ToArray();
                propertyValueArr = s_tempPropertyValueList.ToArray();
                awakeMethod = type.GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);                
            }

            // TODO: found out, private fields (even with serializeProperty) are not retrieved when calling GetFields.
            // I should call this method in the constructor
            // NOTE: I found this out while working on another project and I will leave this until I can test it propetly
            public static IEnumerable<FieldInfo> GetAllFields(System.Type t)
            {
                if (t == null)
                    return Enumerable.Empty<FieldInfo>();

                BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
                                     BindingFlags.Static | BindingFlags.Instance |
                                     BindingFlags.DeclaredOnly;
                return t.GetFields(flags).Concat(GetAllFields(t.BaseType));
            }

            public void RestorePrefabValues(Component target)
            {
                for (int i = 0, s = fieldValueArr.Length; i < s; ++i)
                {
                    fieldValueArr[i].Key.SetValue(target, fieldValueArr[i].Value);
                }
                for (int i = 0, s = propertyValueArr.Length; i < s; ++i)
                {
                    propertyValueArr[i].Key.SetValue(target, propertyValueArr[i].Value, null);
                }

                //stop all coroutines
                MonoBehaviour bhv = target as MonoBehaviour;
                if (bhv) bhv.StopAllCoroutines();

                //call awake
                if (awakeMethod != null)
                    awakeMethod.Invoke(target, null);
            }
        }

        // Initialized the cache of component values for the prefab
        private void InitializeGameObjectComponentValues(GameObject prefab, bool includeChildren = true)
        {
            s_tempGoCompValueList.Clear();
            s_tempCompList.Clear();

            if (includeChildren)
                prefab.GetComponentsInChildren<Component>(true, s_tempCompList);
            else
                prefab.GetComponents<Component>(s_tempCompList);

            for (int i = 0, s = s_tempCompList.Count; i < s; ++i)
            {
                Component compIter = s_tempCompList[i];
                GameObjectComponentValues prefabCompValues = new GameObjectComponentValues(compIter);
                if (!prefabCompValues.IsEmpty)
                    s_tempGoCompValueList.Add(prefabCompValues);
            }
            m_prefabCompValues = s_tempGoCompValueList.ToArray();
        }

        private class ComponentCache
        {
            public Component[] components;
            public Rigidbody rigidBody;
            public Rigidbody2D rigidBody2D;
        }        
        private void RestorePrefabValues(GameObject go, bool includeChildren = true)
        {
            ComponentCache componentCache = GetObjComponentCache(go, includeChildren);
                       
            Debug.Assert(m_prefabCompValues.Length == componentCache.components.Length, "The number of components in the prefab and gameObject is not the same. Did you destroy any component by mistake?");
            //TODO: if it mismatches destroy go and instantiate a new go until it works fine. Remember to remove it from m_dicGoComp.

            if (m_prefabCompValues.Length == componentCache.components.Length)
            {
                for (int i = 0, s = componentCache.components.Length; i < s; ++i)
                {
                    Component comp = componentCache.components[i];
                    m_prefabCompValues[i].RestorePrefabValues(comp);
                }
            }
            // reset rigidBody velocity
            if(componentCache.rigidBody)
            {
                componentCache.rigidBody.velocity = Vector3.zero;
                componentCache.rigidBody.angularVelocity = Vector3.zero;
            }
            // reset rigidBody2D velocity
            else if (componentCache.rigidBody2D)
            {
                componentCache.rigidBody2D.velocity = Vector2.zero;
                componentCache.rigidBody2D.angularVelocity = 0f;
            }                
        }

        private ComponentCache GetObjComponentCache(GameObject go, bool includeChildren = true)
        {
            ComponentCache componentCache;
            if (!m_dicGoComp.TryGetValue(go, out componentCache))
            {
                componentCache = CacheObjComponents(go, includeChildren);
            }
            return componentCache;
        }

        private void CacheObjComponents(List<GameObject> list)
        {
            for (int i = 0, s = list.Count; i < s; ++i)
            {
                GameObject obj = list[i];
                if (obj)
                {
                    CacheObjComponents(obj);
                    PoolManager.RegisterObjInPool(this, obj);
                }
                else
                {
                    Debug.LogWarning("Found null gameObject in list at " + i);
                }
            }
        }

        private ComponentCache CacheObjComponents(GameObject go, bool includeChildren = true)
        {
            s_tempCompList.Clear();
            if (includeChildren)
                go.GetComponentsInChildren<Component>(true, s_tempCompList);
            else
                go.GetComponents<Component>(s_tempCompList);
            //Keep only the components contained in the m_prefabCompValues
            for (int i = 0, s = s_tempCompList.Count, s2 = m_prefabCompValues.Length; i < s; ++i)
            {
                if (i >= s2 || s_tempCompList[i].GetType() != m_prefabCompValues[i].type) //Opt: Use RemoveRange for i >= s2.
                {
                    s_tempCompList.RemoveAt(i--);
                    s = s_tempCompList.Count;
                }
            }
            ComponentCache componentCache = new ComponentCache();
            componentCache.components = s_tempCompList.ToArray();
            componentCache.rigidBody = go.GetComponent<Rigidbody>();
            if (!componentCache.rigidBody)
                componentCache.rigidBody2D = go.GetComponent<Rigidbody2D>();
            m_dicGoComp[go] = componentCache;
            return componentCache;
        }

        #endregion
    }
}
