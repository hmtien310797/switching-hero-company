using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Boss;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Immortal_Switch.Scripts.Shared
{
    public partial class DatabaseManager
    {
        private const string BossDataLabel = "boss_data";

        private readonly Dictionary<int, BossDataSO> bossDataMapper = new();

        private AsyncOperationHandle<IList<BossDataSO>> bossDataHandle;
        private bool isBossDataLoaded;

        private async UniTask InitBossDataAsync()
        {
            bossDataMapper.Clear();

            bossDataHandle = Addressables.LoadAssetsAsync<BossDataSO>(
                BossDataLabel,
                null
            );

            await bossDataHandle.Task;

            if (bossDataHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[DatabaseManager] Load BossData failed. Label: {BossDataLabel}");
                return;
            }

            foreach (BossDataSO data in bossDataHandle.Result)
            {
                if (data == null)
                {
                    continue;
                }

                if (bossDataMapper.ContainsKey(data.Id))
                {
                    Debug.LogError($"[DatabaseManager] Duplicate BossData Id: {data.Id}, Asset: {data.name}");
                    continue;
                }

                bossDataMapper.Add(data.Id, data);
            }

            isBossDataLoaded = true;

            Debug.Log($"[DatabaseManager] Loaded BossData count: {bossDataMapper.Count}");
        }

        public bool TryGetBossData(int bossId, out BossDataSO bossData)
        {
            bossData = null;

            if (!isBossDataLoaded)
            {
                Debug.LogError("[DatabaseManager] BossData has not been loaded yet.");
                return false;
            }

            if (!bossDataMapper.TryGetValue(bossId, out bossData))
            {
                Debug.LogError($"[DatabaseManager] Missing BossData Id: {bossId}");
                return false;
            }

            return bossData != null;
        }

        private void ReleaseBossData()
        {
            bossDataMapper.Clear();
            isBossDataLoaded = false;

            if (bossDataHandle.IsValid())
            {
                Addressables.Release(bossDataHandle);
            }
        }
    }
}