using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Immortal_Switch.Scripts.Shared
{
    public partial class DatabaseManager
    {
        private const string CreepDataLabel = "creep_data";

        private readonly List<CreepDataSo> creepData = new();
        private readonly Dictionary<int, CreepDataSo> creepDataMapper = new();

        private AsyncOperationHandle<IList<CreepDataSo>> creepDataHandle;
        private bool isCreepDataLoaded;

        private async UniTask InitCreepDataAsync()
        {
            creepData.Clear();
            creepDataMapper.Clear();
            isCreepDataLoaded = false;

            creepDataHandle = Addressables.LoadAssetsAsync<CreepDataSo>(
                CreepDataLabel,
                null
            );

            await creepDataHandle.Task;

            if (creepDataHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[DatabaseManager] Load CreepData failed. Label: {CreepDataLabel}");
                return;
            }

            foreach (CreepDataSo data in creepDataHandle.Result)
            {
                if (data == null)
                {
                    continue;
                }

                if (creepDataMapper.ContainsKey(data.Id))
                {
                    Debug.LogError($"[DatabaseManager] Duplicate CreepData Id: {data.Id}, Asset: {data.name}");
                    continue;
                }

                creepData.Add(data);
                creepDataMapper.Add(data.Id, data);
            }

            isCreepDataLoaded = true;
            Debug.Log($"[DatabaseManager] Loaded CreepData count: {creepDataMapper.Count}");
        }

        public bool TryGetCreepData(int enemyId, out CreepDataSo creepData)
        {
            creepData = null;

            if (!isCreepDataLoaded)
            {
                Debug.LogError("[DatabaseManager] CreepData has not been loaded yet.");
                return false;
            }

            if (!creepDataMapper.TryGetValue(enemyId, out creepData))
            {
                Debug.LogError($"[DatabaseManager] Missing CreepData Id: {enemyId}");
                return false;
            }

            return creepData != null;
        }

        private void ReleaseCreepData()
        {
            creepData.Clear();
            creepDataMapper.Clear();
            isCreepDataLoaded = false;

            if (creepDataHandle.IsValid())
            {
                Addressables.Release(creepDataHandle);
            }
        }
    }
}
