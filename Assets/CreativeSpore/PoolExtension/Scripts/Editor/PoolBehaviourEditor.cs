// Copyright (C) 2018 Creative Spore - All Rights Reserved
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace CreativeSpore.PoolExtension
{
    [CustomEditor(typeof(PoolBehaviour))]
	public class PoolBehaviourEditor : Editor
	{
        [MenuItem("GameObject/Pool Manager/PoolBehaviour", false, 10)]
        public static void AddPoolBehaviour(MenuCommand menuCommand)
        {
            PoolEditorUtils.CreateGameObjectWithComponent<PoolBehaviour>();
        }

        private PoolBehaviour m_target;
        private void OnEnable()
        {
            m_target = target as PoolBehaviour;
            m_target.Initialize();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if(GUI.changed)
            {
                //TODO: initialize only if prefab changes
                m_target.Initialize();
            }

            int preCachedCount = EditorGUILayout.DelayedIntField("Pre-Cached Instances", m_target.transform.childCount);
            if(preCachedCount != m_target.transform.childCount)
            {
                m_target.PreCacheInstances(preCachedCount);
            }

            if(GUILayout.Button("Add Scene Instances to Pool"))
            {
                List<GameObject> gameObjects = GetAllObjectsInScene();
                foreach(GameObject go in gameObjects)
                {
                    //Debug.Log(go.name + " --> " + PrefabUtility.GetPrefabObject(go) + ", " + PrefabUtility.GetPrefabParent(go) + ", " + PrefabUtility.GetPrefabType(go));
#if UNITY_2018_3_OR_NEWER
                    
                    if (PrefabUtility.GetCorrespondingObjectFromSource(go) == m_target.Prefab)
#else
                    if (PrefabUtility.GetPrefabParent(go) == m_target.Prefab)
#endif
                    {
                        if (m_target.TryAddGameObjectToPool(go))
                        {
                            Debug.Log("Found " + go.name);
                        }
                    }
                }
            }
        }

        List<GameObject> GetAllObjectsInScene()
        {
            List<GameObject> objectsInScene = new List<GameObject>();
            foreach (GameObject go in Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[])
            {                
                if (go.hideFlags == HideFlags.NotEditable || go.hideFlags == HideFlags.HideAndDontSave)
                    continue;
                if (EditorUtility.IsPersistent(go.transform.root.gameObject))
                    continue;
                objectsInScene.Add(go);
            }
            return objectsInScene;
        }
    }
}
