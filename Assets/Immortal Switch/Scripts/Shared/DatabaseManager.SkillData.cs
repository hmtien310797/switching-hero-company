using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Skill;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Immortal_Switch.Scripts.Shared
{
    public partial class DatabaseManager
    {
        private const string ClassSkillDataLabel = "class_skill_data";
        private const string UltimateSkillDataLabel = "ultimate_skill_data";
        private const string PassiveSkillDataLabel = "passive_skill_data";

        private readonly List<SkillDataSO> classSkillData = new();
        private readonly List<SkillDataSO> ultimateSkillData = new();
        private readonly List<SkillDataSO> passiveSkillData = new();

        private readonly Dictionary<int, SkillDataSO> skillDataDict = new();
        private readonly Dictionary<int, SkillDataSO> ultimateSkillDataByHeroId = new();
        private readonly Dictionary<int, SkillDataSO> passiveSkillDataByHeroId = new();

        private AsyncOperationHandle<IList<SkillDataSO>> classSkillDataHandle;
        private AsyncOperationHandle<IList<SkillDataSO>> ultimateSkillDataHandle;
        private AsyncOperationHandle<IList<SkillDataSO>> passiveSkillDataHandle;

        private bool isSkillDataLoaded;

        private async UniTask InitSkillDataAsync()
        {
            classSkillData.Clear();
            ultimateSkillData.Clear();
            passiveSkillData.Clear();

            skillDataDict.Clear();
            ultimateSkillDataByHeroId.Clear();
            passiveSkillDataByHeroId.Clear();

            isSkillDataLoaded = false;

            classSkillDataHandle = Addressables.LoadAssetsAsync<SkillDataSO>(
                ClassSkillDataLabel,
                null
            );

            ultimateSkillDataHandle = Addressables.LoadAssetsAsync<SkillDataSO>(
                UltimateSkillDataLabel,
                null
            );

            passiveSkillDataHandle = Addressables.LoadAssetsAsync<SkillDataSO>(
                PassiveSkillDataLabel,
                null
            );

            await UniTask.WhenAll(
                classSkillDataHandle.Task.AsUniTask(),
                ultimateSkillDataHandle.Task.AsUniTask(),
                passiveSkillDataHandle.Task.AsUniTask()
            );

            if (classSkillDataHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[DatabaseManager] Load ClassSkillData failed. Label: {ClassSkillDataLabel}");
                return;
            }

            if (ultimateSkillDataHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[DatabaseManager] Load UltimateSkillData failed. Label: {UltimateSkillDataLabel}");
                return;
            }

            if (passiveSkillDataHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[DatabaseManager] Load PassiveSkillData failed. Label: {PassiveSkillDataLabel}");
                return;
            }

            CacheSkillList(classSkillDataHandle.Result, classSkillData, null);
            CacheSkillList(ultimateSkillDataHandle.Result, ultimateSkillData, ultimateSkillDataByHeroId);
            CacheSkillList(passiveSkillDataHandle.Result, passiveSkillData, passiveSkillDataByHeroId);

            isSkillDataLoaded = true;

            Debug.Log(
                $"[DatabaseManager] Loaded SkillData. " +
                $"Class: {classSkillData.Count}, " +
                $"Ultimate: {ultimateSkillData.Count}, " +
                $"Passive: {passiveSkillData.Count}"
            );
        }

        private void CacheSkillList(
            IList<SkillDataSO> source,
            List<SkillDataSO> targetList,
            Dictionary<int, SkillDataSO> heroIdMapper)
        {
            foreach (SkillDataSO data in source)
            {
                if (data == null)
                {
                    continue;
                }

                targetList.Add(data);

                if (skillDataDict.ContainsKey(data.SkillId))
                {
                    Debug.LogError($"[DatabaseManager] Duplicate SkillData SkillId: {data.SkillId}, Asset: {data.name}");
                }
                else
                {
                    skillDataDict.Add(data.SkillId, data);
                }

                if (heroIdMapper == null)
                {
                    continue;
                }

                if (heroIdMapper.ContainsKey(data.HeroId))
                {
                    Debug.LogError($"[DatabaseManager] Duplicate SkillData HeroId: {data.HeroId}, Asset: {data.name}");
                    continue;
                }

                heroIdMapper.Add(data.HeroId, data);
            }
        }

        public SkillDataSO GetSkillDataById(int id)
        {
            if (!isSkillDataLoaded)
            {
                Debug.LogError("[DatabaseManager] SkillData has not been loaded yet.");
                return null;
            }

            if (!skillDataDict.TryGetValue(id, out SkillDataSO data))
            {
                Debug.LogError($"[DatabaseManager] Missing SkillData Id: {id}");
                return null;
            }

            return data;
        }

        public List<SkillDataSO> GetAllSkillData()
        {
            return classSkillData;
        }

        public SkillDataSO GetUltimateSkillDataByHeroId(int heroId)
        {
            if (!isSkillDataLoaded)
            {
                Debug.LogError("[DatabaseManager] SkillData has not been loaded yet.");
                return null;
            }

            if (!ultimateSkillDataByHeroId.TryGetValue(heroId, out SkillDataSO data))
            {
                Debug.LogError($"[DatabaseManager] Missing UltimateSkillData for HeroId: {heroId}");
                return null;
            }

            return data;
        }

        public SkillDataSO GetPassiveSkillDataByHeroId(int heroId)
        {
            if (!isSkillDataLoaded)
            {
                Debug.LogError("[DatabaseManager] SkillData has not been loaded yet.");
                return null;
            }

            if (!passiveSkillDataByHeroId.TryGetValue(heroId, out SkillDataSO data))
            {
                Debug.LogError($"[DatabaseManager] Missing PassiveSkillData for HeroId: {heroId}");
                return null;
            }

            return data;
        }

        private void ReleaseSkillData()
        {
            classSkillData.Clear();
            ultimateSkillData.Clear();
            passiveSkillData.Clear();

            skillDataDict.Clear();
            ultimateSkillDataByHeroId.Clear();
            passiveSkillDataByHeroId.Clear();

            isSkillDataLoaded = false;

            if (classSkillDataHandle.IsValid())
            {
                Addressables.Release(classSkillDataHandle);
            }

            if (ultimateSkillDataHandle.IsValid())
            {
                Addressables.Release(ultimateSkillDataHandle);
            }

            if (passiveSkillDataHandle.IsValid())
            {
                Addressables.Release(passiveSkillDataHandle);
            }
        }
    }
}
