namespace Scripts.Common
{
    using System.Collections.Generic;
    using UnityEngine;

    public class PoolController : MonoBehaviour
    {
        public static PoolController Instance;

        private Dictionary<string, List<GameObject>> _poolDict = new Dictionary<string, List<GameObject>>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public T Get<T>(T prefab, Vector3 pos) where T : Component
        {
            string key = prefab.name;

            if (!_poolDict.ContainsKey(key))
            {
                _poolDict[key] = new List<GameObject>();
            }

            GameObject obj = _poolDict[key].Find(x => !x.activeInHierarchy);

            if (obj == null)
            {
                obj = Instantiate(prefab.gameObject, pos, Quaternion.identity);
                obj.name = key;
                _poolDict[key].Add(obj);
            }
            else
            {
                obj.transform.position = pos;
                obj.gameObject.SetActive(true);
            }

            return obj.GetComponent<T>();
        }

        public void ReturnToPool(GameObject obj)
        {
            obj.SetActive(false);
            obj.transform.SetParent(this.transform); // Trả về làm con của PoolController cho gọn Hierarchy
        }
    }
}
