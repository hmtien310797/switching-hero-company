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
        private const string ClassSkillDataLabel = "skill_data";

        private readonly List<SkillDataSO> classSkillData = new();
        private readonly List<SkillDataSO> ultimateSkillData = new();
        private readonly List<SkillDataSO> passiveSkillData = new();

        private readonly Dictionary<int, SkillDataSO> classSkillDataDict = new();
        private readonly Dictionary<int, SkillDataSO> ultimateSkillDataByHeroId = new();
        private readonly Dictionary<int, SkillDataSO> passiveSkillDataByHeroId = new();

        private AsyncOperationHandle<IList<SkillDataSO>> skillDataHandle;

        private bool isSkillDataLoaded;

        private async UniTask InitSkillDataAsync()
        {
            classSkillData.Clear();
            ultimateSkillData.Clear();
            passiveSkillData.Clear();

            classSkillDataDict.Clear();
            ultimateSkillDataByHeroId.Clear();
            passiveSkillDataByHeroId.Clear();

            isSkillDataLoaded = false;

            skillDataHandle = Addressables.LoadAssetsAsync<SkillDataSO>(
                ClassSkillDataLabel,
                null
            );

            await UniTask.WhenAll(
                skillDataHandle.Task.AsUniTask()
            );

            if (skillDataHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[DatabaseManager] Load ClassSkillData failed. Label: {ClassSkillDataLabel}");
                return;
            }

            CacheSkillList(skillDataHandle.Result);

            isSkillDataLoaded = true;

            Debug.Log(
                $"[DatabaseManager] Loaded SkillData. " +
                $"Class: {classSkillData.Count}, " +
                $"Ultimate: {ultimateSkillData.Count}, " +
                $"Passive: {passiveSkillData.Count}"
            );
        }

        private void CacheSkillList(IList<SkillDataSO> source)
        {
            foreach (SkillDataSO data in source)
            {
                if (data == null)
                {
                    continue;
                }

                switch (data.OwnerType)
                {
                    case SkillOwnerType.ClassSkill:
                        classSkillData.Add(data);
                        if (!classSkillDataDict.TryAdd(data.SkillId, data))
                        {
                            Debug.LogError($"[DatabaseManager] Duplicate SkillData SkillId: {data.SkillId}, Asset: {data.name}");
                        }
                        break;
                    case SkillOwnerType.UltimateSkill:
                        ultimateSkillData.Add(data);
                        if (!ultimateSkillDataByHeroId.TryAdd(data.HeroId, data))
                        {
                            Debug.LogError($"[DatabaseManager] Duplicate SkillData HeroId: {data.HeroId}, Asset: {data.name}");
                        }
                        break;
                    default:
                        passiveSkillData.Add(data);
                        if (!passiveSkillDataByHeroId.TryAdd(data.HeroId, data))
                        {
                            Debug.LogError($"[DatabaseManager] Duplicate SkillData HeroId: {data.HeroId}, Asset: {data.name}");
                        }
                        break;
                }
            }
        }

        public SkillDataSO GetSkillDataById(int id)
        {
            if (!isSkillDataLoaded)
            {
                Debug.LogError("[DatabaseManager] SkillData has not been loaded yet.");
                return null;
            }

            if (!classSkillDataDict.TryGetValue(id, out SkillDataSO data))
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

            classSkillDataDict.Clear();
            ultimateSkillDataByHeroId.Clear();
            passiveSkillDataByHeroId.Clear();

            isSkillDataLoaded = false;

            if (skillDataHandle.IsValid())
            {
                Addressables.Release(skillDataHandle);
            }
        }
    }
}
