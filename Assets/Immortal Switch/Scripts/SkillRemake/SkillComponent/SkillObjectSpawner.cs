using Common;
using UnityEngine;

namespace Immortal_Switch.Scripts.Skill
{
    public interface ISkillObjectSpawner
    {
        T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component, IPoolable;
        void Despawn<T>(T obj) where T : Component, IPoolable;
    }

    public sealed class PoolSkillObjectSpawner : ISkillObjectSpawner
    {
        public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component, IPoolable
        {
            if (prefab == null)
                return null;

            if (PoolManager.Instance != null)
                return PoolManager.Instance.Spawn(prefab, position, rotation);

            T instance = Object.Instantiate(prefab, position, rotation);
            instance.OnSpawnedFromPool();
            return instance;
        }

        public void Despawn<T>(T obj) where T : Component, IPoolable
        {
            if (obj == null)
                return;

            if (PoolManager.Instance != null)
                PoolManager.Instance.Despawn(obj);
            else
                Object.Destroy(obj.gameObject);
        }
    }
}
