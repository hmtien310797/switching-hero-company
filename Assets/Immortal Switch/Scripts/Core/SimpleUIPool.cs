using System.Collections.Generic;
using UnityEngine;

namespace Immortal_Switch.Scripts.Core
{
    public class SimpleUIPool<T> where T : Component
    {
        private readonly T prefab;
        private readonly Transform parent;
        private readonly List<T> pool = new();

        public SimpleUIPool(T prefab, Transform parent)
        {
            this.prefab = prefab;
            this.parent = parent;
        }

        public T Get(int index)
        {
            if (index < pool.Count)
            {
                var item = pool[index];
                item.gameObject.SetActive(true);
                return item;
            }

            var newItem = Object.Instantiate(prefab, parent);
            pool.Add(newItem);
            return newItem;
        }

        public void ReleaseFrom(int index)
        {
            for (int i = index; i < pool.Count; i++)
            {
                if (pool[i] != null)
                    pool[i].gameObject.SetActive(false);
            }
        }
    }
}