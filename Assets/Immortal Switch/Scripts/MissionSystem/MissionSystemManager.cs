using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.MissionSystem.Interfaces;
using Immortal_Switch.Scripts.MissionSystem.Models;
using Immortal_Switch.Scripts.PlayerSystem;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.Views;
using Newtonsoft.Json;
using UnityEngine;

namespace Immortal_Switch.Scripts.MissionSystem
{
    public class MissionSystemManager : Singleton<MissionSystemManager>
    {
        private IMissionSystemService Service { get; set; }
        private IMissionSystemStorage Storage { get; set; }

        /// <summary>
        /// event fire khi progress thay đổi
        /// 1: type nhiem vu
        /// 2: progress value
        /// 3: id nhiem vu
        /// </summary>
        public event Action<string, int, string> OnChangeProgress;

        /// <summary>
        /// event fire khi point task thay doi
        /// 1: gia tri point
        /// 2: type nhiem vu
        /// </summary>
        public event Action<int, string> OnChangePoint;

        /// <summary>
        /// event fire khi nhan thuong reward group
        /// 1: id nhiem vu
        /// 2: type nhiem vu
        /// </summary>
        public event Action<string, string> OnMissionClaimed;

        /// <summary>
        /// event fire khi nhan thuong reward group
        /// 1: trang thai cac moc point
        /// 2: type nhiem vu
        /// </summary>
        public event Action<List<MissionSystemPoint>, string> OnRewardGroupClaimed;

        // --- Private Field ---
        private MissionSystemDatabaseSO _database;

        protected override void OnSingletonAwake()
        {
            PlayerSystemManager.Instance.OnLoginNewDay += OnPlayerLoginNewDay;

            GameEventManager.Subscribe<int>(GameEvents.OnEnemyDead, OnEnemyDead);
            GameEventManager.Subscribe<int>(GameEvents.OnStageCleared, OnStageCleared);
            GameEventManager.Subscribe<int>(GameEvents.ON_SUMMON_HERO, OnSummonHero);
            GameEventManager.Subscribe(GameEvents.ON_AFK_REWARD_CLAIM_COUNT, OnAfkRewardClaimCount);
            GameEventManager.Subscribe(GameEvents.ON_DUNGEON_CLEAR, OnDungeonClear);
        }

        private void OnPlayerLoginNewDay()
        {
            var matches = Service.ChangeProgress(MissionEventKeys.EVENT_LOGIN, 1);
            DispatchChangeProgress(matches);
        }

        protected override void OnDestroy()
        {
            GameEventManager.Unsubscribe<int>(GameEvents.OnEnemyDead, OnEnemyDead);
            GameEventManager.Unsubscribe<int>(GameEvents.OnStageCleared, OnStageCleared);
            GameEventManager.Unsubscribe<int>(GameEvents.ON_SUMMON_HERO, OnSummonHero);
            GameEventManager.Unsubscribe(GameEvents.ON_AFK_REWARD_CLAIM_COUNT, OnAfkRewardClaimCount);
            GameEventManager.Unsubscribe(GameEvents.ON_DUNGEON_CLEAR, OnDungeonClear);
            base.OnDestroy();
        }

        private void OnEnemyDead(int deadCnt)
        {
            if (deadCnt >= 1)
            {
                var matches = Service.ChangeProgress(MissionEventKeys.EVENT_KILL_MONSTER, 1);
                DispatchChangeProgress(matches);
            }
        }

        private void OnDungeonClear()
        {
            var matches = Service.ChangeProgress(MissionEventKeys.EVENT_DUNGEON_CLEAR, 1);
            DispatchChangeProgress(matches);
        }

        private void OnAfkRewardClaimCount()
        {
            var matches = Service.ChangeProgress(MissionEventKeys.EVENT_CLAIM_IDLE, 1);
            DispatchChangeProgress(matches);
        }

        private void OnSummonHero(int times)
        {
            var matches = Service.ChangeProgress(MissionEventKeys.EVENT_HERO_SUMMON, times);
            DispatchChangeProgress(matches);
        }

        private void OnStageCleared(int stage)
        {
            var matchClearStage = Service.ChangeProgress(MissionEventKeys.EVENT_CLEAR_STAGE, stage);
            var matchKillBoss = Service.ChangeProgress(MissionEventKeys.EVENT_KILL_BOSS, 1);
            var matches = matchClearStage.Concat(matchKillBoss);

            DispatchChangeProgress(matches);
        }

        public override async UniTask InitializeAsync()
        {
            Load();
            await SyncFromServerAsync();
        }

        private void Load()
        {
            _database = DatabaseManager.Instance.MissionSystemDatabase;
            Storage = new MissionSystemStorage(_database);
            Service = new MissionSystemService(Storage);

            Storage.Load();
            Storage.Initialize();
            Storage.OnAfterSave = () => SyncToServerAsync().Forget();
        }

        public List<DynamicHeroesGlobalSpecificationsMissionPointMilesStoneRow> GetMilesStone(string scope)
        {
            return _database.MissionPointMilesStoneConfig.rows.FindAll(v => v.scope == scope);
        }

        public int GetPoint(string missionType)
        {
            switch (missionType)
            {
                case MissionTypes.DAILY:
                    return Storage.Data.DailyTask.Point;

                case MissionTypes.WEEKLY:
                    return Storage.Data.WeeklyTask.Point;
            }

            return 0;
        }

        public List<MissionSystemEntry> GetTasks(string missionType)
        {
            switch (missionType)
            {
                case MissionTypes.DAILY:
                    return Storage.Data.DailyTask.Tasks;

                case MissionTypes.WEEKLY:
                    return Storage.Data.WeeklyTask.Tasks;

                case MissionTypes.REPEAT:
                    return Storage.Data.RepeatTask;
            }

            return new List<MissionSystemEntry>();
        }

        public List<MissionSystemPoint> GetStates(string missionType)
        {
            switch (missionType)
            {
                case MissionTypes.DAILY:
                    return Storage.Data.DailyTask.PointsClaimed;

                case MissionTypes.WEEKLY:
                    return Storage.Data.WeeklyTask.PointsClaimed;
            }

            return new List<MissionSystemPoint>();
        }

        public List<DynamicHeroesGlobalSpecificationsMissionConfigRow> GetMissions(string missionType)
        {
            return _database.MissionConfig.rows.FindAll(v => v.type == missionType);
        }

        public DynamicHeroesGlobalSpecificationsMissionConfigRow GetMission(string id)
        {
            return _database.MissionConfig.rows.Find(v => v.missionId == id);
        }

        public bool IsCompleted(DynamicHeroesGlobalSpecificationsMissionConfigRow cfg)
        {
            return Service.IsCompleted(cfg);
        }

        public void MissionClaimAndNotify(DynamicHeroesGlobalSpecificationsMissionConfigRow cfg)
        {
            var rewards = MissionClaim(cfg);
            NotifyIfAllMissionDailyCompleted();
        }

        public List<ItemRewardData> MissionClaim(DynamicHeroesGlobalSpecificationsMissionConfigRow cfg)
        {
            var isCompleted = IsCompleted(cfg);

            if (!isCompleted)
            {
                Debug.LogError("Mission claim failed");
                return new List<ItemRewardData>();
            }

            // xử lý logic cho các type mission
            switch (cfg.type)
            {
                // tiep nhiem vu moi.
                case MissionTypes.MAIN:
                    var nextCfg = _database.MissionConfig.rows.Find(v => v.missionId == cfg.nextMission);

                    if (nextCfg != null)
                    {
                        Service.NextMainMission(nextCfg);
                    }
                    else
                    {
                        Service.SetIsClaimed(cfg.missionId, cfg.type, true);
                    }

                    DispatchProgressMainMission();
                    break;

                case MissionTypes.DAILY:
                case MissionTypes.WEEKLY:
                {
                    var isClaimed = Service.SetIsClaimed(cfg.missionId, cfg.type, true);

                    if (isClaimed)
                    {
                        Service.IncreasePoint(cfg.type, cfg.points);
                        OnMissionClaimed?.Invoke(cfg.missionId, cfg.type);

                        OnChangePoint?.Invoke(
                            cfg.type == MissionTypes.DAILY ? Storage.Data.DailyTask.Point : Storage.Data.WeeklyTask.Point,
                            cfg.type
                        );
                    }

                    if (cfg.type == MissionTypes.DAILY)
                    {
                        NotifyIfAllMissionDailyCompleted();
                    }

                    break;
                }

                case MissionTypes.REPEAT:
                case MissionTypes.ACHIEVEMENT:
                {
                    var isClaimed = Service.SetIsClaimed(cfg.missionId, cfg.type, true);

                    if (isClaimed)
                    {
                        OnMissionClaimed?.Invoke(cfg.missionId, cfg.type);
                    }

                    break;
                }
            }

            var rewards = DatabaseManager.Instance.GetRewards(cfg.rewards);
            ClaimMissionOnServerAsync(cfg, rewards).Forget();
            PopupRewardService.Show(rewards);
            return rewards;
        }

        public void ClaimAll(string missionType)
        {
            var rewards = new List<ItemData>();

            switch (missionType)
            {
                case MissionTypes.DAILY:
                case MissionTypes.WEEKLY:
                {
                    // GetMissions trả về TOÀN BỘ mission của type này, kể cả chưa hoàn thành —
                    // chỉ claim những mission đã IsCompleted (progress đủ target + chưa claim),
                    // nếu không MissionClaim sẽ Debug.LogError "Mission claim failed" cho từng
                    // mission chưa xong (button Claim All chỉ cần AnyCompleted để bật, không phải
                    // AllCompleted).
                    var missions = GetMissions(missionType).Where(IsCompleted).ToList();

                    foreach (var cfg in missions)
                    {
                        rewards.AddRange(MissionClaim(cfg));
                    }

                    var milesStones = GetMilesStone(missionType);

                    foreach (var cfg in milesStones)
                    {
                        rewards.AddRange(RewardGroupClaim(cfg, false));
                    }

                    break;
                }

                case MissionTypes.REPEAT:
                {
                    var missions = GetMissions(missionType).Where(IsCompleted).ToList();

                    foreach (var cfg in missions)
                    {
                        rewards.AddRange(MissionClaim(cfg));
                    }

                    break;
                }
            }

            PopupRewardService.Show(rewards);
            NotifyIfAllMissionDailyCompleted();
        }

        public List<ItemRewardData> RewardGroupClaim(DynamicHeroesGlobalSpecificationsMissionPointMilesStoneRow row, bool useAds)
        {
            var rewards = Service.RewardGroupClaim(row, useAds);

            if (rewards.Count > 0)
            {
                switch (row.scope)
                {
                    case MissionTypes.DAILY:
                        OnRewardGroupClaimed?.Invoke(Storage.Data.DailyTask.PointsClaimed, row.scope);
                        break;

                    case MissionTypes.WEEKLY:
                        OnRewardGroupClaimed?.Invoke(Storage.Data.WeeklyTask.PointsClaimed, row.scope);
                        break;
                }

                ClaimMissionGroupOnServerAsync(row, useAds).Forget();
            }

            return rewards;
        }

        public bool AnyCompleted(string missionType)
        {
            switch (missionType)
            {
                case MissionTypes.DAILY:
                case MissionTypes.WEEKLY:
                case MissionTypes.REPEAT:
                    var missions = GetMissions(missionType);

                    if (missions.Any(IsCompleted))
                    {
                        return true;
                    }

                    break;
            }

            return false;
        }

        public void NotifyReady()
        {
            DispatchProgressMainMission();
            OnRewardGroupClaimed?.Invoke(Storage.Data.DailyTask.PointsClaimed, MissionTypes.DAILY);
            OnRewardGroupClaimed?.Invoke(Storage.Data.WeeklyTask.PointsClaimed, MissionTypes.WEEKLY);
        }

        public void NotifyIfAllMissionDailyCompleted()
        {
            var missions = GetMissions(MissionTypes.DAILY);

            if (missions.All(IsCompleted))
            {
                Service.ChangeProgress(MissionEventKeys.EVENT_COMPLETE_DAILY, 1);
            }
        }

        private void DispatchChangeProgress(
            IEnumerable<KeyValuePair<string, MissionSystemEntry>> matches
        )
        {
            foreach (var entry in matches)
            {
                OnChangeProgress?.Invoke(entry.Key, entry.Value.Progress, entry.Value.Id);
            }
        }

        private void DispatchProgressMainMission()
        {
            OnChangeProgress?.Invoke(MissionTypes.MAIN, Storage.Data.Main.Progress, Storage.Data.Main.Id);
        }

        // ── Server sync ───────────────────────────────────────────────────────

        private async UniTask SyncFromServerAsync()
        {
            if (!IsClientReady())
                return;

            try
            {
                // Always send current local state so server can store it on first login (1 round-trip).
                string localJson = JsonConvert.SerializeObject(Storage.Data);
                MissionStateResponse response = await NakamaClient.Instance.GetMissionStateAsync(localJson);

                if (response?.Success != true ||
                    !response.HasState ||
                    response.State == null)
                    return;

                MissionSystemData serverData = response.State.ToObject<MissionSystemData>();

                if (serverData?.Main != null)
                    Storage.LoadFromData(serverData);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MissionSystem] SyncFromServer failed: {e.Message}");
            }
        }

        private async UniTaskVoid SyncToServerAsync()
        {
            if (!IsClientReady())
                return;

            try
            {
                await NakamaClient.Instance.SyncMissionStateAsync(JsonConvert.SerializeObject(Storage.Data));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MissionSystem] SyncToServer failed: {e.Message}");
            }
        }

        private async UniTaskVoid ClaimMissionOnServerAsync(
            DynamicHeroesGlobalSpecificationsMissionConfigRow cfg,
            List<ItemRewardData> rewards)
        {
            if (!IsClientReady())
                return;

            try
            {
                MissionClaimResponse response = await NakamaClient.Instance.ClaimMissionAsync(
                    new MissionClaimRequest
                    {
                        MissionId = cfg.missionId,
                        MissionType = cfg.type,
                        Points = cfg.points,
                        Rewards = ToRewardDtos(rewards)
                    });

                if (response?.Success == true &&
                    response.Balances != null)
                    CurrencyManager.Instance?.ApplyServerBalances(response.Balances);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MissionSystem] ClaimMission server failed: {e.Message}");
            }
        }

        private async UniTaskVoid ClaimMissionGroupOnServerAsync(
            DynamicHeroesGlobalSpecificationsMissionPointMilesStoneRow row,
            bool isAdsX2)
        {
            if (!IsClientReady())
                return;

            try
            {
                // Always send base (undoubled) rewards; server doubles internally for x2.
                var baseRewards = DatabaseManager.Instance.GetRewards(row.rewards);

                MissionClaimResponse response = await NakamaClient.Instance.ClaimMissionGroupAsync(
                    new MissionClaimGroupRequest
                    {
                        Scope = row.scope,
                        PointThreshold = row.pointThreshold,
                        Rewards = ToRewardDtos(baseRewards),
                        IsAdsX2 = isAdsX2
                    });

                if (response?.Success == true &&
                    response.Balances != null)
                    CurrencyManager.Instance?.ApplyServerBalances(response.Balances);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MissionSystem] ClaimMissionGroup server failed: {e.Message}");
            }
        }

        private static List<MissionRewardDto> ToRewardDtos(List<ItemRewardData> entries)
        {
            var list = new List<MissionRewardDto>();

            if (entries == null)
                return list;

            foreach (var e in entries)
                list.Add(new MissionRewardDto { ItemKey = e.ItemKey, Quantity = e.Quantity.FloorToIntSafe(), });

            return list;
        }

        private static bool IsClientReady() => NakamaClient.Instance != null && NakamaClient.Instance.IsLoggedIn;
    }
}