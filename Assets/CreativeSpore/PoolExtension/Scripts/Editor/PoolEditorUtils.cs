// Copyright (C) 2018 Creative Spore - All Rights Reserved
using UnityEngine;
using UnityEditor;
using System.Collections;

namespace CreativeSpore.PoolExtension
{
	public static class PoolEditorUtils
	{
        public static Component CreateGameObjectWithComponent(System.Type componentType)
        {
            if (componentType.IsSubclassOf(typeof(Component)))
            {
                string componentName = componentType.Name;
                string name = GameObjectUtility.GetUniqueNameForSibling(Selection.activeTransform, componentName);
                GameObject go = new GameObject(name);
                Component comp = go.AddComponent(componentType);
                go.transform.SetParent(Selection.activeTransform);
                go.transform.localPosition = Vector3.zero;
                Selection.activeGameObject = go;
                Undo.RegisterCreatedObjectUndo((UnityEngine.Object)go, "Create " + componentName);
                return comp;
            }
            return null;
        }

        public static T CreateGameObjectWithComponent<T>() where T : Component
        {
            return CreateGameObjectWithComponent(typeof(T)) as T;
        }
    }
}
