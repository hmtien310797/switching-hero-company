using System.Collections.Generic;
using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts;
using Immortal_Switch.Scripts.Enemy;
using Immortal_Switch.Scripts.Pooling;
using Immortal_Switch.Scripts.Shared;
using UnityEngine;

namespace Battle
{
    /// <summary>
    /// Quản lý tập creep pool đang được dùng bởi battle session hiện tại.
    /// Chapter và Dungeon phải dùng chung đúng một instance service này.
    /// </summary>
    public sealed class BattleCreepPoolService : MonoBehaviour
    {
        [SerializeField, Min(1f)]
        private float capacityBufferMultiplier = 1.5f;

        private readonly HashSet<string> activePoolKeys = new();
        private readonly Dictionary<string, int> requiredPools = new();
        private readonly List<string> obsoletePoolKeys = new();

        public async UniTask<bool> PrepareAsync(
            IReadOnlyList<CreepPoolWarmupRequest> requests,
            string sourceName)
        {
            AddressablePoolService poolService = AddressablePoolService.Instance;

            if (poolService == null)
            {
                Debug.LogError($"[{sourceName}] AddressablePoolService.Instance is null.");
                return false;
            }

            requiredPools.Clear();
            obsoletePoolKeys.Clear();

            if (requests != null)
            {
                for (int i = 0; i < requests.Count; i++)
                {
                    CreepPoolWarmupRequest request = requests[i];

                    if (request.EnemyId <= 0 || request.WarmupCount <= 0)
                        continue;

                    if (!DatabaseManager.Instance.TryGetCreepData(
                            request.EnemyId,
                            out CreepDataSo creepData) ||
                        creepData == null)
                    {
                        Debug.LogError(
                            $"[{sourceName}] Missing CreepDataSO. EnemyId={request.EnemyId}"
                        );
                        return false;
                    }

                    string poolKey = creepData.CreepAddressKey;

                    if (string.IsNullOrWhiteSpace(poolKey))
                    {
                        Debug.LogError(
                            $"[{sourceName}] Empty CreepAddressKey. EnemyId={request.EnemyId}"
                        );
                        return false;
                    }

                    if (requiredPools.TryGetValue(poolKey, out int currentCount))
                    {
                        requiredPools[poolKey] = Mathf.Max(
                            currentCount,
                            request.WarmupCount
                        );
                    }
                    else
                    {
                        requiredPools.Add(poolKey, request.WarmupCount);
                    }
                }
            }

            foreach (string activeKey in activePoolKeys)
            {
                if (!requiredPools.ContainsKey(activeKey))
                    obsoletePoolKeys.Add(activeKey);
            }

            for (int i = 0; i < obsoletePoolKeys.Count; i++)
            {
                string obsoleteKey = obsoletePoolKeys[i];

                if (poolService.DisposePool(obsoleteKey))
                    activePoolKeys.Remove(obsoleteKey);
            }

            foreach (KeyValuePair<string, int> pair in requiredPools)
            {
                string poolKey = pair.Key;

                if (poolService.HasPool(poolKey))
                {
                    activePoolKeys.Add(poolKey);
                    continue;
                }

                int capacity = Mathf.Max(
                    1,
                    Mathf.CeilToInt(
                        pair.Value * Mathf.Max(1f, capacityBufferMultiplier)
                    )
                );

                bool created = await poolService.CreatePoolAsync(
                    poolKey,
                    capacity
                );

                if (!created)
                {
                    Debug.LogError(
                        $"[{sourceName}] Failed to create creep pool. " +
                        $"Key={poolKey}, Capacity={capacity}"
                    );
                    return false;
                }

                activePoolKeys.Add(poolKey);
            }

            Debug.Log(
                $"[{sourceName}] Creep pools prepared. " +
                $"RequiredPoolCount={requiredPools.Count}"
            );

            return true;
        }
        
        public void DisposeAllEnemyPools()
        {
            AddressablePoolService poolService = AddressablePoolService.Instance;
            obsoletePoolKeys.Clear();

            foreach (string activeKey in activePoolKeys)
            {
                if (string.IsNullOrEmpty(activeKey))
                    continue;

                obsoletePoolKeys.Add(activeKey);
            }

            for (int i = 0; i < obsoletePoolKeys.Count; i++)
            {
                string key = obsoletePoolKeys[i];

                poolService.DespawnAndDisposePool(key);
            }

            activePoolKeys.Clear();
            requiredPools.Clear();
            obsoletePoolKeys.Clear();
        }
    }
}
