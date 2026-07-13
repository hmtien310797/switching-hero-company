using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Hero;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Immortal_Switch.Scripts.Shared
{
    public partial class DatabaseManager
    {
        private const string HeroDataLabel = "hero_data";

        private readonly List<HeroDataSO> heroData = new();
        private readonly Dictionary<int, HeroDataSO> heroDataDict = new();

        private AsyncOperationHandle<IList<HeroDataSO>> heroDataHandle;
        private bool isHeroDataLoaded;

        private async UniTask InitHeroDataAsync()
        {
            heroData.Clear();
            heroDataDict.Clear();
            isHeroDataLoaded = false;

            heroDataHandle = Addressables.LoadAssetsAsync<HeroDataSO>(
                HeroDataLabel,
                null
            );

            await heroDataHandle.Task;

            if (heroDataHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[DatabaseManager] Load HeroData failed. Label: {HeroDataLabel}");
                return;
            }

            foreach (HeroDataSO data in heroDataHandle.Result)
            {
                if (data == null)
                {
                    continue;
                }

                if (heroDataDict.ContainsKey(data.Id))
                {
                    Debug.LogError($"[DatabaseManager] Duplicate HeroData Id: {data.Id}, Asset: {data.name}");
                    continue;
                }

                heroData.Add(data);
                heroDataDict.Add(data.Id, data);
            }

            isHeroDataLoaded = true;
            Debug.Log($"[DatabaseManager] Loaded HeroData count: {heroDataDict.Count}");
        }

        public HeroDataSO GetHeroDataById(int id)
        {
            if (!isHeroDataLoaded)
            {
                Debug.LogError("[DatabaseManager] HeroData has not been loaded yet.");
                return null;
            }

            if (!heroDataDict.TryGetValue(id, out HeroDataSO data))
            {
                Debug.LogError($"[DatabaseManager] Missing HeroData Id: {id}");
                return null;
            }

            return data;
        }

        public List<HeroDataSO> GetAllHeroData()
        {
            return heroData;
        }

        private void ReleaseHeroData()
        {
            heroData.Clear();
            heroDataDict.Clear();
            isHeroDataLoaded = false;

            if (heroDataHandle.IsValid())
            {
                Addressables.Release(heroDataHandle);
            }
        }
    }
}