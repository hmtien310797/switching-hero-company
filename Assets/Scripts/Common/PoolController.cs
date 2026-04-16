using System;
using System.Collections.Generic;
using UnityEngine;

namespace Scripts.Common
{
    [Serializable]
    public class KeyValueDict
    {
        public string KeyPool;
        public int CountKeyPool;
    }

    public class PoolController : MonoBehaviour
    {
        public static PoolController Instance;

        private Dictionary<string, List<GameObject>> _poolDict = new Dictionary<string, List<GameObject>>();

        public List<KeyValueDict> keyValueDict = new List<KeyValueDict>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public (T,bool) Get<T>(T prefab, Vector3 pos) where T : Component
        {
            string key = prefab.name;

            if (!_poolDict.ContainsKey(key))
            {
                _poolDict[key] = new List<GameObject>();
                keyValueDict.Add(new KeyValueDict() { KeyPool = key, CountKeyPool = 0 });
            }

            GameObject obj = _poolDict[key].Find(x => !x.activeInHierarchy);

            bool isNew = false;
            if (obj == null)
            {
                obj = Instantiate(prefab.gameObject, pos, Quaternion.identity);
                obj.name = key;
                _poolDict[key].Add(obj);
                isNew = true;
                obj.gameObject.SetActive(true);

                var kv = keyValueDict.Find(x => x.KeyPool == key);
                kv.CountKeyPool++;
            }
            else
            {
                obj.transform.position = pos;
                obj.gameObject.SetActive(true);
            }

            return (obj.GetComponent<T>(), isNew);
        }

        public void ReturnToPool(GameObject obj)
        {
            obj.SetActive(false);
            obj.transform.SetParent(this.transform); // Trả về làm con của PoolController cho gọn Hierarchy
        }
    }
}
