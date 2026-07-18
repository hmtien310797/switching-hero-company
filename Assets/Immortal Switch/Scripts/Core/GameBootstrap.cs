using System;
using Battle;
using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Addressable;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Equipment.Core;
using Immortal_Switch.Scripts.GrowthSystem;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Items;
using Immortal_Switch.Scripts.MissionSystem;
using Immortal_Switch.Scripts.PlayerSystem;
using Immortal_Switch.Scripts.Pooling;
using Immortal_Switch.Scripts.PowerUpSystem;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shop;
using Immortal_Switch.Scripts.Shop.IAP;
using Immortal_Switch.Scripts.Skill;
using Immortal_Switch.Scripts.Skill.UI;
using Immortal_Switch.Scripts.SummonSystem.HeroSummon;
using Immortal_Switch.Scripts.SummonSystem.WeaponSummon;
using Immortal_Switch.Scripts.TransmutationSystem;
using Immortal_Switch.Scripts.Tutorial;
using Immortal_Switch.Scripts.UI;
using Immortal_Switch.Scripts.RemoteUpdate;
using Immortal_Switch.Scripts.RemoteUpdate.Examples;
using UnityEngine;

namespace Immortal_Switch.Scripts.Core
{
    public class GameBootstrap : Singleton<GameBootstrap>
    {
        private void Start()
        {
            Application.targetFrameRate = 60;
        }

        public async UniTask RunAsync(
            Action<float, string> onProgress = null,
            System.Threading.CancellationToken cancellationToken = default)
        {
            // 3 virtual steps reserved for remote content update (may involve
            // a long download — smooth sub-progress keeps the bar moving).
            // + 16 existing bootstrap steps = 19 total.
            const int totalSteps = 19;
            const int remoteUpdateReservedSteps = 3;

            var progress = new BootstrapProgress(
                totalSteps,
                onProgress
            );

            try
            {
                // ── Step 0: Remote content update (runs before every system init) ──
                progress.ReserveSteps(remoteUpdateReservedSteps);
                await RunRemoteContentUpdateAsync(progress, cancellationToken);
                progress.CompleteReservedSteps("Content ready");

                progress.ReportCurrent("Preparing game data");

                // 1
                if (!NakamaClient.Instance.IsLoggedIn)
                {
                    await NakamaClient.Instance.AuthenticateDeviceAsync();
                }

                progress.CompleteStep("Authenticated");

                // 2
                await DatabaseManager.Instance.InitializeAsync();
                progress.CompleteStep("Database initialized");

                TransmutationSystemManager.Instance.InitializeAsync();
                HeroProgressionManager.Instance.InitializeAsync();

                // 3
                await MissionSystemManager.Instance.InitializeAsync();
                progress.CompleteStep("Mission system initialized");

                // 4
                await ShopManager.Instance.InitializeAsync();
                progress.CompleteStep("Shop initialized");

                // 5
                await PlayerSystemManager.Instance.InitializeAsync();
                progress.CompleteStep("Player system initialized");

                // WeaponManager.saveData chỉ được gán trong InitializeAsync() (Load()) — phải
                // await xong TRƯỚC khi ApplyPlayerData (bước 6) gọi SyncWeaponListToManager, nếu
                // không saveData vẫn null và WeaponManager.SyncFromServer ném NullReferenceException,
                // khiến ApplyPlayerData bị abort giữa chừng TRƯỚC dòng GetPlayerDataFromServer — hệ
                // quả là UserDataCache.HeroList/WeaponList không bao giờ được set (rơi về lineup mặc
                // định) dù RPC player/me đã trả dữ liệu đúng. Trước đây gọi fire-and-forget ở bước 10,
                // sau cả bước 6 — luôn trễ.
                await WeaponManager.Instance.InitializeAsync();

                PlayerMeResponse player = null;

                // 6
                try
                {
                    player = await NakamaClient.Instance.GetPlayerMeAsync();
                    ApplyPlayerData(player);
                }
                catch (Exception ex)
                {
                    // Log đầy đủ (không chỉ ex.Message) — exception có thể xảy ra trong
                    // ApplyPlayerData (xử lý response), không hẳn ở chính RPC GetPlayerMeAsync,
                    // nên cần stack trace để biết chính xác dòng nào throw.
                    Debug.LogError(
                        $"[Bootstrap] player/me fetch or apply failed: {ex}"
                    );
                }

                progress.CompleteStep("Player data loaded");

                // 7
                try
                {
                    var skillList =
                        await NakamaClient.Instance.GetSkillListAsync();

                    if (skillList != null)
                    {
                        UserDataCache.Instance.SkillList = skillList;
                        SyncSkillListToInventory(skillList);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(
                        $"[Bootstrap] Failed to fetch skill/list: {ex.Message}"
                    );
                }

                progress.CompleteStep("Skill data loaded");
                await UniTask.Delay(TimeSpan.FromSeconds(1f));

                // 8
                try
                {
                    var summonState =
                        await NakamaClient.Instance.GetSummonStateAsync();

                    UserDataCache.Instance.SummonState = summonState;
                    ApplySummonState(summonState);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(
                        $"[Bootstrap] Failed to fetch summon state: {ex.Message}"
                    );
                }

                progress.CompleteStep("Summon data loaded");

                // 9
                try
                {
                    var bag = await NakamaClient.Instance.GetBagAsync();
                    ItemsManager.Instance.SyncFromServer(bag);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(
                        $"[Bootstrap] Failed to fetch bag state: {ex.Message}"
                    );
                }

                progress.CompleteStep("Bag data loaded");

                // 10
                await UserDataCache.Instance.InitializeAsync();
                progress.CompleteStep("User cache initialized");
                await UniTask.Delay(TimeSpan.FromSeconds(1f));

                // 11
                await GrowthManager.Instance.InitializeAsync();
                SyncGrowthToManager(player?.growth);
                progress.CompleteStep("Growth data initialized");

                // 12
                await PowerUpManager.Instance.InitializeAsync();
                progress.CompleteStep("Power-up system initialized");

                // 13
                try
                {
                    await IAPManager.Instance.InitializeAsync();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(
                        $"[Bootstrap] Failed to initialize IAP: {ex.Message}"
                    );
                }

                progress.CompleteStep("Store Service initialized");

                // 14
                await SkillViewDataProvider.Instance.InitializeAsync();
                progress.CompleteStep("Skill view data prepared");
                
                await AddressablePoolService.Instance.InitializeAsync();

                // 15
                await UIManager.Instance.InitializeAsync();
                progress.CompleteStep("UI initialized");
                
                await UniTask.WhenAll(HeroImageService.InitializeAsync(), SkillImageService.InitializeAsync());

                // 16
                progress.CompleteStep("Battle data initialized");
                await PvEBattleController.Instance.InitializeAsync();
                
                await TutorialManager.Instance.TryGuide(
                    TutorialGuideIds.NEW_USER_GUIDE
                );
                

                Debug.Log("Bootstrap completed");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);

                // Report the failure so the loading UI shows the error,
                // then rethrow so the caller (LoginScene) knows bootstrap failed
                // and does NOT proceed into the game.
                onProgress?.Invoke(
                    0f,
                    $"Bootstrap failed: {ex.Message}"
                );

                throw;
            }
        }

        private async UniTask RunRemoteContentUpdateAsync(
            BootstrapProgress progress,
            System.Threading.CancellationToken cancellationToken)
        {
            var service = AddressableRemoteUpdateService.Instance;

            RemoteContentUpdateResult result;
            try
            {
                // Delegate to the reusable bootstrap-step utility.
                // Progress is forwarded into the reserved-steps block so the
                // loading bar moves smoothly during catalog checks and download.
                result = await RemoteUpdateBootstrapStep.RunAsync(
                    (percent, message) => progress.ReportReservedProgress(percent, message),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameBootstrap] Remote update threw: {ex}");
                throw new InvalidOperationException(
                    $"Content update failed: {ex.Message}", ex);
            }

            switch (result.Status)
            {
                case RemoteContentUpdateStatus.Complete:
                case RemoteContentUpdateStatus.NoUpdateNeeded:
                    Debug.Log($"[GameBootstrap] Remote update OK. " +
                              $"Downloaded {result.RequiredDownloadedBytes} bytes in " +
                              $"{result.ElapsedTime.TotalSeconds:F1}s.");
                    return;

                case RemoteContentUpdateStatus.Offline:
                    if (await service.IsContentAvailableOfflineAsync(cancellationToken))
                    {
                        Debug.Log("[GameBootstrap] Offline — using cached content.");
                        return;
                    }
                    throw new InvalidOperationException(
                        "No internet connection and required content is not cached. " +
                        "Please connect to the internet on first launch.");

                case RemoteContentUpdateStatus.Timeout:
                    if (await service.IsContentAvailableOfflineAsync(cancellationToken))
                    {
                        Debug.LogWarning("[GameBootstrap] Update timed out — using cached content.");
                        return;
                    }
                    throw new InvalidOperationException(
                        "Content update timed out and no cached content is available. " +
                        "Please check your connection and try again.");

                case RemoteContentUpdateStatus.Failed:
                    throw new InvalidOperationException(
                        $"Content update failed: {string.Join("; ", result.Errors)}");

                case RemoteContentUpdateStatus.Cancelled:
                    throw new OperationCanceledException(
                        "Remote content update was cancelled.");

                default:
                    throw new InvalidOperationException(
                        $"Unexpected update status: {result.Status}");
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
            UserDataCache.Instance.DisplayName = player.display_name ?? string.Empty;
            UserDataCache.Instance.Uid = player.user_id ?? string.Empty;
            UserDataCache.Instance.GoogleLinked = player.google_linked;
            UserDataCache.Instance.AppleLinked = player.apple_linked;
            TopMainView.Instance?.SetDisplayName(UserDataCache.Instance.DisplayName);

            UserDataCache.Instance.Exp = player.exp;

            CurrencyManager.Instance.Set(CurrencyType.gold, player.coins);
            CurrencyManager.Instance.Set(CurrencyType.diamond, player.gems);
            CurrencyManager.Instance.Set(CurrencyType.summon_ticket_hero, player.hero_ticket);
            CurrencyManager.Instance.Set(CurrencyType.summon_ticket_skill, player.skill_ticket);
            CurrencyManager.Instance.Set(CurrencyType.summon_ticket_weapon, player.weapon_ticket);
            CurrencyManager.Instance.Set(CurrencyType.weapon_ore, player.weapon_ore);


            if (player.heroes != null)
            {
                SyncHeroListToProgression(player.heroes);
            }

            if (player.weapons != null)
            {
                SyncWeaponListToManager(player.weapons);
            }

            UserDataCache.Instance.GetPlayerDataFromServer(player.heroes, player.skills, player.weapons);

            // current_stage/current_chapter/highest_stage_cleared — nguồn sự thật cho stage,
            // không cần gọi battle/progression riêng (chỉ gọi lại khi resync sau STAGE_MISMATCH/INVALID_STAGE)
            PvEBattleController.Instance.ApplyServerProgression(player.progression);

            Debug.Log(
                $"[Bootstrap] Player data applied. Coins={player.coins}, Gems={player.gems}, HeroTicket={player.hero_ticket}, SkillTicket={player.skill_ticket}, WeaponTicket={player.weapon_ticket}, WeaponOre={player.weapon_ore}, Energy={player.energy}");
        }

        private void ApplySummonState(SummonStateResponse state)
        {
            if (state == null)
                return;

            HeroSummonManager.Instance?.ApplySummonState(state.Hero);
            SkillSummonManager.Instance?.ApplySummonState(state.Skill);
            WeaponSummonManager.Instance?.ApplySummonState(state.Weapon);
        }

        private void SyncHeroListToProgression(HeroInventory heroList)
        {
            if (heroList == null)
                return;

            if (HeroProgressionManager.Instance == null)
                return;

            HeroProgressionManager.Instance.SyncFromServer(heroList.Owned, heroList.Shards);
        }

        private void SyncSkillListToInventory(SkillListResponse skills)
        {
            if (skills.Owned != null)
            {
                foreach (var skill in skills.Owned)
                {
                    if (skill.SkillId <= 0)
                        continue;

                    SkillInventorySaveService.SetOwned(skill.SkillId, true);
                    SkillInventorySaveService.SetLevel(skill.SkillId, skill.Level > 0 ? skill.Level : 1);
                }
            }

            if (skills.Shards != null)
            {
                foreach (var kv in skills.Shards)
                {
                    if (int.TryParse(kv.Key, out int skillId) &&
                        skillId > 0)
                        SkillInventorySaveService.SetCurrentShard(skillId, kv.Value);
                }
            }

            SkillInventorySaveService.Save();
        }

        private void SyncWeaponListToManager(WeaponListResponse weaponList)
        {
            if (weaponList == null)
                return;

            if (WeaponManager.Instance == null)
                return;

            WeaponManager.Instance.SyncFromServer(weaponList);
        }

        /// <summary>
        /// Áp player/me.growth vào GrowthManager — phải gọi SAU GrowthManager.InitializeAsync() (load
        /// ES3 xong) vì SaveData chỉ được tạo trong Load(), khác Hero/WeaponManager load sẵn trong Awake().
        /// </summary>
        private void SyncGrowthToManager(GrowthStateResponse growthState)
        {
            if (growthState == null)
                return;

            if (GrowthManager.Instance == null)
                return;

            GrowthManager.Instance.SyncFromServer(growthState);
        }

        public override UniTask InitializeAsync()
        {
            return UniTask.CompletedTask;
        }

        private sealed class BootstrapProgress
        {
            private readonly int _totalSteps;
            private readonly Action<float, string> _onProgress;

            private int _completedSteps;
            private int _reservedSteps;

            public BootstrapProgress(
                int totalSteps,
                Action<float, string> onProgress)
            {
                _totalSteps = Mathf.Max(1, totalSteps);
                _onProgress = onProgress;
            }

            /// <summary>
            /// Reserve a block of <paramref name="count"/> steps that will be
            /// completed together. Call <see cref="ReportReservedProgress"/> to
            /// report sub-progress within the block, then
            /// <see cref="CompleteReservedSteps"/> to finalise all of them.
            /// </summary>
            public void ReserveSteps(int count)
            {
                _reservedSteps = Mathf.Max(0, count);
            }

            /// <summary>
            /// Report progress within the currently reserved block.
            /// <paramref name="subPercent"/> should be 0.0 – 1.0.
            /// The progress bar never moves backward.
            /// </summary>
            public void ReportReservedProgress(float subPercent, string message)
            {
                if (_reservedSteps <= 0) return;

                float blockFraction = _reservedSteps / (float)_totalSteps;
                float blockStart = _completedSteps / (float)_totalSteps;
                float progress = blockStart + blockFraction * Mathf.Clamp01(subPercent);

                _onProgress?.Invoke(Mathf.Clamp01(progress), message);
            }

            /// <summary>
            /// Mark all reserved steps as complete and report the final
            /// progress position.
            /// </summary>
            public void CompleteReservedSteps(string message)
            {
                _completedSteps += _reservedSteps;
                _reservedSteps = 0;

                var progress = Mathf.Clamp01(
                    _completedSteps / (float)_totalSteps
                );

                _onProgress?.Invoke(progress, message);
            }

            public void ReportCurrent(string message)
            {
                _onProgress?.Invoke(
                    Mathf.Clamp01(_completedSteps / (float)_totalSteps),
                    message
                );
            }

            public void CompleteStep(string message)
            {
                _completedSteps++;

                var progress = Mathf.Clamp01(
                    _completedSteps / (float)_totalSteps
                );

                _onProgress?.Invoke(progress, message);
            }

            public void Complete(string message = "Complete")
            {
                _completedSteps = _totalSteps;
                _onProgress?.Invoke(1f, message);
            }
        }
    }
}