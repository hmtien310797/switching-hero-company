using System;
using Battle;
using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.GrowthSystem;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.MissionSystem;
using Immortal_Switch.Scripts.PlayerSystem;
using Immortal_Switch.Scripts.Pooling;
using Immortal_Switch.Scripts.PowerUpSystem;
using Immortal_Switch.Scripts.Skill;
using Immortal_Switch.Scripts.Skill.UI;
using Immortal_Switch.Scripts.SummonSystem.HeroSummon;
using Immortal_Switch.Scripts.SummonSystem.WeaponSummon;
using Immortal_Switch.Scripts.UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.Core
{
    public class GameBootstrap : Singleton<GameBootstrap>
    {
        private void Start()
        {
            Application.targetFrameRate = 60;
        }

        public async UniTask RunAsync()
        {
            try
            {
                // Đảm bảo có session trước khi fetch data
                // (trường hợp chạy thẳng từ MainBattleScene trong Editor)
                if (!NakamaClient.Instance.IsLoggedIn)
                    await NakamaClient.Instance.AuthenticateDeviceAsync();

                // init dau tien. có các manager khác sử dụng tới. tránh lỗi.
 
                await MasterDataCache.Instance.InitializeAsync();
                await PlayerSystemManager.Instance.InitializeAsync();
                await MissionSystemManager.Instance.InitializeAsync();

                // Fetch player data — includes heroes/skills/weapons inventory
                try
                {
                    var player = await NakamaClient.Instance.GetPlayerMeAsync();
                    ApplyPlayerData(player);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Bootstrap] Failed to fetch player/me: {ex.Message}");
                }

                // Đồng bộ summon state (total_roll, pity, summon_level, claimed milestones)
                try
                {
                    var summonState = await NakamaClient.Instance.GetSummonStateAsync();
                    UserDataCache.Instance.SummonState = summonState;
                    ApplySummonState(summonState);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Bootstrap] Failed to fetch summon state: {ex.Message}");
                }
                await UserDataCache.Instance.InitializeAsync();
                await GrowthManager.Instance.InitializeAsync();
                await PowerUpManager.Instance.InitializeAsync();
                await SkillViewDataProvider.Instance.InitializeAsync();
                await AddressablePoolService.Instance.InitializeAsync();
                await UIManager.Instance.InitializeAsync();
                await PvEBattleController.Instance.InitializeAsync();

                Debug.Log("Bootstrap completed");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        // private async UniTask FetchPlayerDataAsync()
        // {
        //     if (NakamaClient.Instance.Session == null)
        //     {
        //         Debug.LogWarning("[Bootstrap] No Nakama session, skipping player data fetch.");
        //         return;
        //     }
        //
        //     try
        //     {
        //         string token = PlayerPrefs.GetString("auth_token", "");
        //
        //         if (string.IsNullOrEmpty(token))
        //         {
        //             Debug.LogWarning("[Bootstrap] No auth token found, skipping player data fetch.");
        //             return;
        //         }
        //
        //         string url = $"{NetworkManager.Instance.BaseUrl}/v1/player/me";
        //         using var webRequest = UnityWebRequest.Get(url);
        //         webRequest.SetRequestHeader("Authorization", $"Bearer {token}");
        //         webRequest.SetRequestHeader("Accept", "application/x-protobuf");
        //
        //         await webRequest.SendWebRequest();
        //
        //         if (webRequest.result != UnityWebRequest.Result.Success)
        //         {
        //             Debug.LogError($"[Bootstrap] FetchPlayerData failed: {webRequest.error}");
        //             return;
        //         }
        //
        //         var response = GetPlayerResponse.Parser.ParseFrom(webRequest.downloadHandler.data);
        //
        //         if (response.Meta.Code != 0)
        //         {
        //             Debug.LogError($"[Bootstrap] FetchPlayerData error {response.Meta.Code}: {response.Meta.Message}");
        //             return;
        //         }
        //
        //         ApplyPlayerData(response.Player);
        //     }
        //     catch (Exception e)
        //     {
        //         Debug.LogError($"[Bootstrap] FetchPlayerData failed: {e.Message}");
        //     }
        // }

        private void ApplyPlayerData(PlayerMeResponse player)
        {
            CurrencyManager.Instance.Set(CurrencyType.gold, player.coins);
            CurrencyManager.Instance.Set(CurrencyType.diamond, player.gems);
            CurrencyManager.Instance.Set(CurrencyType.HeroTicket, player.hero_ticket);
            CurrencyManager.Instance.Set(CurrencyType.SkillTicket, player.skill_ticket);

            if (player.heroes != null)
            {
                UserDataCache.Instance.HeroList = player.heroes;
                SyncHeroListToProgression(player.heroes);
            }

            if (player.skills != null)
                UserDataCache.Instance.SkillList = player.skills;

            if (player.weapons != null)
                UserDataCache.Instance.WeaponList = player.weapons;

            UserDataCache.Instance.GetPlayerDataFromServer(player.heroes, player.skills, player.weapons);

            Debug.Log($"[Bootstrap] Player data applied. Coins={player.coins}, Gems={player.gems}, HeroTicket={player.hero_ticket}, SkillTicket={player.skill_ticket}, Energy={player.energy}");
        }

        private void ApplySummonState(SummonStateResponse state)
        {
            if (state == null) return;
            HeroSummonManager.Instance?.ApplySummonState(state.Hero);
            SkillSummonManager.Instance?.ApplySummonState(state.Skill);
            WeaponSummonManager.Instance?.ApplySummonState(state.Weapon);
        }

        private void SyncHeroListToProgression(HeroInventory heroList)
        {
            if (heroList?.Owned == null) return;
            if (HeroProgressionManager.Instance == null) return;

            foreach (var heroInstance in heroList.Owned)
            {
                var heroData = MasterDataCache.Instance.GetHeroDataById(heroInstance.HeroId);
                if (heroData != null)
                    HeroProgressionManager.Instance.AcquireHeroIfNeeded(heroData);
            }
        }

        public override UniTask InitializeAsync()
        {
            return UniTask.CompletedTask;
        }
    }
}
