using UnityEngine;
using System.Collections.Generic;

namespace QBlockyFighter.Utils
{
    public class ObjectPool : MonoBehaviour
    {
        public static ObjectPool Instance { get; private set; }

        private Dictionary<string, Queue<GameObject>> pools = new Dictionary<string, Queue<GameObject>>();
        private Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();

        void Awake()
        {
            Instance = this;
        }

        public void RegisterPool(string poolName, GameObject prefab, int initialSize = 10)
        {
            if (pools.ContainsKey(poolName)) return;

            prefabs[poolName] = prefab;
            pools[poolName] = new Queue<GameObject>();

            for (int i = 0; i < initialSize; i++)
            {
                var obj = Instantiate(prefab, transform);
                obj.SetActive(false);
                pools[poolName].Enqueue(obj);
            }
        }

        public GameObject Spawn(string poolName, Vector3 position, Quaternion rotation)
        {
            if (!pools.ContainsKey(poolName))
            {
                Debug.LogWarning($"[Pool] 池 '{poolName}' 不存在");
                return null;
            }

            GameObject obj;
            if (pools[poolName].Count > 0)
            {
                obj = pools[poolName].Dequeue();
            }
            else
            {
                obj = Instantiate(prefabs[poolName], transform);
            }

            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
            return obj;
        }

        public void Despawn(string poolName, GameObject obj, float delay = 0f)
        {
            if (delay > 0)
            {
                StartCoroutine(DespawnDelayed(poolName, obj, delay));
                return;
            }

            obj.SetActive(false);
            if (pools.ContainsKey(poolName))
            {
                pools[poolName].Enqueue(obj);
            }
            else
            {
                Destroy(obj);
            }
        }

        private System.Collections.IEnumerator DespawnDelayed(string poolName, GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (obj != null) Despawn(poolName, obj);
        }

        public void ClearPool(string poolName)
        {
            if (!pools.ContainsKey(poolName)) return;
            while (pools[poolName].Count > 0)
            {
                var obj = pools[poolName].Dequeue();
                if (obj != null) Destroy(obj);
            }
            pools.Remove(poolName);
            prefabs.Remove(poolName);
        }

        public void ClearAll()
        {
            foreach (var pool in pools)
            {
                while (pool.Value.Count > 0)
                {
                    var obj = pool.Value.Dequeue();
                    if (obj != null) Destroy(obj);
                }
            }
            pools.Clear();
            prefabs.Clear();
        }
    }
}
