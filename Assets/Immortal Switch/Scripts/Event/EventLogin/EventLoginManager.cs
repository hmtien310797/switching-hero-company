using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.MissionSystem;
using Immortal_Switch.Scripts.Shared;
using Nakama;
using UnityEngine;

namespace Immortal_Switch.Scripts.Event.EventLogin
{
    /// <summary>
    /// Cache trạng thái event Tân Thủ (7 ngày id 1004 / 30 ngày id 1003) lấy từ server (RPC
    /// eventlogin/*) — server là nguồn sự thật duy nhất, thay cho EventMissionManager/Service/
    /// Storage (ES3) cục bộ trước đây, vốn chỉ hiển thị popup mà không hề cấp vật phẩm thật (xem
    /// project memory "Newbie checkin client-local only"). Cùng kiến trúc với
    /// EventLeHoiBangLongManager. State được cache theo eventId vì client có thể mở cả 2 event
    /// (7 ngày và 30 ngày chạy song song cho cùng tài khoản).
    /// </summary>
    public class EventLoginManager : Singleton<EventLoginManager>
    {
        public event Action<int> OnDataChanged;

        private readonly Dictionary<int, EventLoginStateResponse> _states = new();

        public override UniTask InitializeAsync()
        {
            SubscribeEvents();
            return UniTask.CompletedTask;
        }

        protected override void OnDestroy()
        {
            UnsubscribeEvents();
            base.OnDestroy();
        }

        public EventLoginStateResponse GetState(int eventId)
        {
            return _states.GetValueOrDefault(eventId);
        }

        /// <summary>Tải lại state của 1 event từ server. Gọi khi mở EventLoginView và sau mỗi
        /// claim thành công để đồng bộ tiến trình/số dư.</summary>
        public async UniTask RefreshAsync(int eventId)
        {
            try
            {
                var state = await NakamaClient.Instance.GetEventLoginStateAsync(eventId);

                if (state.Success)
                {
                    _states[eventId] = state;
                }
                else
                {
                    Debug.LogWarning($"[EventLoginManager] eventlogin/state event_id={eventId} failed: {state.Error}");
                }

                OnDataChanged?.Invoke(eventId);
            }
            catch (ApiResponseException ex)
            {
                Debug.LogError($"[EventLoginManager] eventlogin/state error {ex.StatusCode}: {ex.Message}");
            }
        }

        /// <summary>Nhận thưởng 1 nhiệm vụ đã hoàn thành.</summary>
        public async UniTask<List<ItemData>> ClaimMission(int eventId, string missionId)
        {
            EventLoginClaimMissionResponse response;

            try
            {
                response = await NakamaClient.Instance.EventLoginClaimMissionAsync(eventId, missionId);
            }
            catch (ApiResponseException ex)
            {
                Debug.LogError($"[EventLoginManager] eventlogin/claim_mission error {ex.StatusCode}: {ex.Message}");
                return new List<ItemData>();
            }

            if (!response.Success)
            {
                Debug.LogWarning($"[EventLoginManager] eventlogin/claim_mission {missionId} failed: {response.Error}");
                return new List<ItemData>();
            }

            CurrencyManager.Instance?.ApplyServerBalances(response.Balances);
            await RefreshAsync(eventId);

            return response.Reward != null
                ? new List<ItemData> { new(response.Reward.ItemId, response.Reward.Amount) }
                : new List<ItemData>();
        }

        /// <summary>Nhận tất cả nhiệm vụ đã hoàn thành trong 1 ngày cụ thể.</summary>
        public async UniTask<List<ItemData>> ClaimAllMissions(int eventId, int day)
        {
            EventLoginClaimAllMissionsResponse response;

            try
            {
                response = await NakamaClient.Instance.EventLoginClaimAllMissionsAsync(eventId, day);
            }
            catch (ApiResponseException ex)
            {
                Debug.LogError($"[EventLoginManager] eventlogin/claim_all_missions error {ex.StatusCode}: {ex.Message}");
                return new List<ItemData>();
            }

            if (!response.Success)
            {
                Debug.LogWarning($"[EventLoginManager] eventlogin/claim_all_missions day={day} failed: {response.Error}");
                return new List<ItemData>();
            }

            CurrencyManager.Instance?.ApplyServerBalances(response.Balances);
            await RefreshAsync(eventId);

            return ToItemDataList(response.Rewards);
        }

        /// <summary>Nhận 1 mốc điểm nhiệm vụ.</summary>
        public async UniTask<List<ItemData>> ClaimMilestone(int eventId, int milestone)
        {
            EventLoginMilestoneResponse response;

            try
            {
                response = await NakamaClient.Instance.EventLoginClaimMilestoneAsync(eventId, milestone);
            }
            catch (ApiResponseException ex)
            {
                Debug.LogError($"[EventLoginManager] eventlogin/claim_milestone error {ex.StatusCode}: {ex.Message}");
                return new List<ItemData>();
            }

            if (!response.Success)
            {
                Debug.LogWarning($"[EventLoginManager] eventlogin/claim_milestone {milestone} failed: {response.Error}");
                return new List<ItemData>();
            }

            CurrencyManager.Instance?.ApplyServerBalances(response.Balances);
            await RefreshAsync(eventId);

            return response.Reward != null
                ? new List<ItemData> { new(response.Reward.ItemId, response.Reward.Amount) }
                : new List<ItemData>();
        }

        /// <summary>Nhận tất cả mốc điểm nhiệm vụ đang đủ điều kiện trong 1 lần gọi.</summary>
        public async UniTask<List<ItemData>> ClaimAllMilestones(int eventId)
        {
            EventLoginClaimAllMilestonesResponse response;

            try
            {
                response = await NakamaClient.Instance.EventLoginClaimAllMilestonesAsync(eventId);
            }
            catch (ApiResponseException ex)
            {
                Debug.LogError($"[EventLoginManager] eventlogin/claim_all_milestones error {ex.StatusCode}: {ex.Message}");
                return new List<ItemData>();
            }

            if (!response.Success)
            {
                Debug.LogWarning($"[EventLoginManager] eventlogin/claim_all_milestones failed: {response.Error}");
                return new List<ItemData>();
            }

            CurrencyManager.Instance?.ApplyServerBalances(response.Balances);
            await RefreshAsync(eventId);

            return ToItemDataList(response.Rewards);
        }

        /// <summary>
        /// Báo tiến độ nhiệm vụ theo trigger lên server (gọi khi gameplay event tương ứng xảy ra
        /// — xem SubscribeEvents). Cập nhật lạc quan (optimistic) vào state cục bộ đang cache
        /// trước để UI phản hồi ngay; RPC chạy nền, không chặn UI. Server áp dụng cho MỌI event
        /// Tân Thủ đang active của tài khoản, không chỉ event đang cache ở client (xem
        /// handler/event_login.js) — cache ở đây chỉ phục vụ hiển thị lạc quan cho view đang mở.
        /// </summary>
        public void ChangeMissionProgress(string trigger, int value)
        {
            if (string.IsNullOrWhiteSpace(trigger) || value <= 0)
            {
                return;
            }

            foreach (var (eventId, state) in _states)
            {
                if (state?.Missions == null)
                {
                    continue;
                }

                var changed = false;

                foreach (var mission in state.Missions)
                {
                    if (mission.Trigger != trigger ||
                        mission.IsClaimed ||
                        mission.Day > state.Progress.CurrentDay)
                    {
                        continue;
                    }

                    mission.Progress = Mathf.Min(mission.Target, mission.Progress + value);
                    changed = true;
                }

                if (changed)
                {
                    OnDataChanged?.Invoke(eventId);
                }
            }

            ReportMissionProgressAsync(trigger, value).Forget();
        }

        private async UniTaskVoid ReportMissionProgressAsync(string trigger, int value)
        {
            try
            {
                await NakamaClient.Instance.EventLoginMissionProgressAsync(trigger, value);
            }
            catch (ApiResponseException ex)
            {
                Debug.LogError($"[EventLoginManager] eventlogin/mission_progress error {ex.StatusCode}: {ex.Message}");
            }
        }

        // ── Gameplay event subscriptions (báo tiến độ nhiệm vụ) ─────────────────────────────
        // Không subscribe LOGIN — server tự cộng nhiệm vụ LOGIN của ngày mới mỗi khi bất kỳ RPC
        // eventlogin/* nào chạm tới event đó (xem handler/event_login.js's nlGetEventState),
        // nên chỉ cần 1 request bất kỳ khác trong ngày là đủ, không cần client tự báo riêng.
        // Không có ARENA_WIN — chưa có gameplay event nào bắn trigger này trong codebase (tính
        // năng Arena PvP chưa gắn vào hệ mission chung), cùng gap đã tồn tại ở bản client-local
        // cũ và ở EventLeHoiBangLongManager.

        private void SubscribeEvents()
        {
            GameEventManager.Subscribe<int>(GameEvents.OnEnemyDead, OnEnemyDead);
            GameEventManager.Subscribe<int>(GameEvents.OnStageCleared, OnStageCleared);
            GameEventManager.Subscribe<int>(GameEvents.ON_SUMMON_HERO, OnSummonHero);
            GameEventManager.Subscribe(GameEvents.ON_ENHANCE_GEAR, OnEnhanceGear);
            GameEventManager.Subscribe(GameEvents.ON_EQUIP_ITEM, OnEquipItem);
            GameEventManager.Subscribe(GameEvents.ON_HERO_LEVEL_UP, OnHeroLevelUp);
            GameEventManager.Subscribe<int>(GameEvents.ON_SKILL_UPGRADE, OnSkillUpgrade);
            GameEventManager.Subscribe(GameEvents.ON_AFK_REWARD_CLAIM_COUNT, OnClaimIdleReward);
            GameEventManager.Subscribe(GameEvents.ON_DUNGEON_CLEAR, OnDungeonClear);
        }

        private void UnsubscribeEvents()
        {
            GameEventManager.Unsubscribe<int>(GameEvents.OnEnemyDead, OnEnemyDead);
            GameEventManager.Unsubscribe<int>(GameEvents.OnStageCleared, OnStageCleared);
            GameEventManager.Unsubscribe<int>(GameEvents.ON_SUMMON_HERO, OnSummonHero);
            GameEventManager.Unsubscribe(GameEvents.ON_ENHANCE_GEAR, OnEnhanceGear);
            GameEventManager.Unsubscribe(GameEvents.ON_EQUIP_ITEM, OnEquipItem);
            GameEventManager.Unsubscribe(GameEvents.ON_HERO_LEVEL_UP, OnHeroLevelUp);
            GameEventManager.Unsubscribe<int>(GameEvents.ON_SKILL_UPGRADE, OnSkillUpgrade);
            GameEventManager.Unsubscribe(GameEvents.ON_AFK_REWARD_CLAIM_COUNT, OnClaimIdleReward);
            GameEventManager.Unsubscribe(GameEvents.ON_DUNGEON_CLEAR, OnDungeonClear);
        }

        private void OnEnemyDead(int count)
        {
            ChangeMissionProgress(EventKeys.EVENT_KILL_MONSTER, count > 0 ? 1 : 0);
        }

        private void OnStageCleared(int stage)
        {
            ChangeMissionProgress("CLEAR_STAGE_COUNT", 1);
        }

        private void OnSummonHero(int times)
        {
            ChangeMissionProgress(EventKeys.EVENT_HERO_SUMMON, times);
        }

        private void OnEnhanceGear()
        {
            ChangeMissionProgress(EventKeys.EVENT_ENHANCE_GEAR, 1);
        }

        private void OnEquipItem()
        {
            ChangeMissionProgress(EventKeys.EVENT_EQUIP_ITEM, 1);
        }

        private void OnHeroLevelUp()
        {
            ChangeMissionProgress(EventKeys.EVENT_HERO_LEVELUP, 1);
        }

        private void OnSkillUpgrade(int count)
        {
            ChangeMissionProgress(EventKeys.EVENT_SKILL_UPGRADE, count);
        }

        private void OnClaimIdleReward()
        {
            ChangeMissionProgress(EventKeys.EVENT_CLAIM_IDLE, 1);
        }

        private void OnDungeonClear()
        {
            ChangeMissionProgress(EventKeys.EVENT_DUNGEON_CLEAR, 1);
        }

        // ── helpers ──────────────────────────────────────────────────────────────────────

        private static List<ItemData> ToItemDataList(List<EventWheelItemDto> rewards)
        {
            var list = new List<ItemData>();

            if (rewards == null)
                return list;

            foreach (var reward in rewards)
            {
                list.Add(new ItemData(reward.ItemId, reward.Amount));
            }

            return list;
        }
    }
}
