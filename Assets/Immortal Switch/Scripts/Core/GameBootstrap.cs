using System;
using Battle;
using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.GrowthSystem;
using Immortal_Switch.Scripts.MissionSystem;
using Immortal_Switch.Scripts.PlayerSystem;
using Immortal_Switch.Scripts.PowerUpSystem;
using Immortal_Switch.Scripts.Skill.UI;
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
                // init dau tien. có các manager khác sử dụng tới. tránh lỗi.
                await PlayerSystemManager.Instance.InitializeAsync();
                await MissionSystemManager.Instance.InitializeAsync();

                //await TransmutationSystemManager.Instance.InitializeAsync();

                await MasterDataCache.Instance.InitializeAsync();
                await UserDataCache.Instance.InitializeAsync();
                await GrowthManager.Instance.InitializeAsync();
                await PowerUpManager.Instance.InitializeAsync();
                await UIManager.Instance.InitializeAsync();
                await SkillViewDataProvider.Instance.InitializeAsync();
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
        

        public override UniTask InitializeAsync()
        {
            return UniTask.CompletedTask;
        }
    }
}
