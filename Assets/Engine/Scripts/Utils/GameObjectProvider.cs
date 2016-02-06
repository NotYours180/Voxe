﻿using System;
using Assets.Engine.Scripts.Common;
using Assets.Engine.Scripts.Common.Collections;
using UnityEngine;

namespace Assets.Engine.Scripts.Provider
{
    [AddComponentMenu("Voxe/Singleton/GameObjectProvider")]
    public class GameObjectProvider: MonoSingleton<GameObjectProvider>
    {
        private GameObject m_go;
        public ObjectPoolEntry [] ObjectPools = new ObjectPoolEntry [0];

        // Called after the singleton instance is created
        private void Awake()
        {
            m_go = new GameObject("GameObjects");
            m_go.transform.parent = gameObject.transform;

            // Iterate pool entries and create a pool of prefabs for each of them
            foreach (ObjectPoolEntry pool in Instance.ObjectPools)
            {
                if (pool.Prefab==null)
                {
                    Debug.LogError("No prefab specified in one of the object pool's entries");
                    continue;
                }
                
                pool.Init(m_go, pool.Prefab);
            }
        }

        // Returns a pool of a given name if it exists
        public static ObjectPoolEntry GetPool(string poolName)
        {
            foreach (ObjectPoolEntry pool in Instance.ObjectPools)
            {
                if (pool.Name==poolName)
                    return pool;
            }
            return null;
        }

        public static void PushObject(string poolName, GameObject go)
        {
            if (go==null)
                throw new ArgumentNullException(string.Format("Trying to pool a null game object in pool {0}", poolName));

            ObjectPoolEntry pool = GetPool(poolName);
            if (pool==null)
                throw new InvalidOperationException(string.Format("Object pool {0} does not exist", poolName));

            pool.Push(go);
        }

        public static GameObject PopObject(string poolName)
        {
            ObjectPoolEntry pool = GetPool(poolName);
            if (pool == null)
                throw new InvalidOperationException(string.Format("Object pool {0} does not exist", poolName));

            return pool.Pop();
        }

        [Serializable]
        public class ObjectPoolEntry
        {
            public string Name;
            public GameObject Prefab;
            public int InitialSize = 128;

            [HideInInspector] public ObjectPool<GameObject> Cache;

            private GameObject m_parentGo;

            public ObjectPoolEntry()
            {
                //Name = "";
                //InitialSize = 0;
                m_parentGo = null;
                Prefab = null;
                Cache = null;
            }

            public void Init(GameObject parentGo, GameObject prefab)
            {
                m_parentGo = parentGo;
                Prefab = prefab;

                Cache = new ObjectPool<GameObject>(
                    () =>
                    {
                        GameObject newGO = Instantiate(Prefab);
                        newGO.name = Prefab.name;
                        newGO.SetActive(false);
                        newGO.transform.parent = m_parentGo.transform; // Make this object a parent of the pooled object
                        return newGO;
                    },
                    InitialSize
                    );
            }

            public void Push(GameObject go)
            {
                // Deactive object, reset its' transform and physics data
                go.SetActive(false);                

                Rigidbody rbody = go.GetComponent<Rigidbody>();
                if (rbody != null)
                    rbody.velocity = Vector3.zero;

                // Place a pointer to our object to the back of our cache list
                Cache.Push(go);
            }

            public GameObject Pop()
            {
                GameObject go = Cache.Pop();
                
                // Reset transform and active it
                //go.transform.parent = null;
                go.SetActive(true);

                return go;
            }
        }
    }
}