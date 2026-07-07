using System;
using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts;
using Immortal_Switch.Scripts.Common;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Shared;
using UnityEngine;

namespace Battle
{
    public sealed class BattleHeroSpawnService
    {
        public async UniTask<HeroActor> SpawnAsync(
            int heroId,
            Vector3 spawnPosition,
            IHeroBattleContext battleContext,
            HeroTeamController heroTeamController,
            bool autoSkill,
            Action<HeroActor> onHeroDead = null)
        {
            HeroDataSO heroData = DatabaseManager.Instance.GetHeroDataById(heroId);
            return await SpawnAsync(heroData, spawnPosition, battleContext, heroTeamController, autoSkill, onHeroDead);
        }

        public async UniTask<HeroActor> SpawnAsync(
            HeroDataSO heroData,
            Vector3 spawnPosition,
            IHeroBattleContext battleContext,
            HeroTeamController heroTeamController,
            bool autoSkill,
            Action<HeroActor> onHeroDead = null)
        {
            if (heroData == null)
            {
                Debug.LogError("[BattleHeroSpawnService] HeroData is null.");
                return null;
            }

            if (string.IsNullOrEmpty(heroData.HeroAddressKey))
            {
                Debug.LogError($"[BattleHeroSpawnService] HeroAddressKey is empty. heroId={heroData.Id}");
                return null;
            }

            if (battleContext == null)
            {
                Debug.LogError($"[BattleHeroSpawnService] BattleContext is null. heroId={heroData.Id}");
                return null;
            }

            HeroActor hero = null;
            try
            {
                hero = await AddressableSpawnService.SpawnAsync<HeroActor>(
                    string.Empty,
                    heroData.HeroAddressKey,
                    spawnPosition,
                    Quaternion.identity
                );

                if (hero == null)
                {
                    Debug.LogError($"[BattleHeroSpawnService] Cannot spawn hero. heroId={heroData.Id}");
                    return null;
                }

                hero.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);
                hero.gameObject.SetActive(true);
                await hero.Init(heroData, battleContext, heroTeamController, autoSkill);

                if (onHeroDead != null)
                {
                    hero.OnDead -= onHeroDead;
                    hero.OnDead += onHeroDead;
                }

                return hero;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[BattleHeroSpawnService] Spawn failed. heroId={heroData.Id}\n{exception}");

                if (hero != null)
                {
                    if (onHeroDead != null)
                        hero.OnDead -= onHeroDead;

                    AddressableSpawnService.ReleaseInstance(hero);
                }

                return null;
            }
        }

        public void Despawn(HeroActor hero, Action<HeroActor> onHeroDead = null)
        {
            if (hero == null)
                return;

            if (onHeroDead != null)
                hero.OnDead -= onHeroDead;

            AddressableSpawnService.ReleaseInstance(hero);
        }
    }
}
