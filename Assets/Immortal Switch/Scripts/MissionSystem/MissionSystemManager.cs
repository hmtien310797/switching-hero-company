using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.ItemSystem.Models;
using Immortal_Switch.Scripts.MissionSystem.Interfaces;
using Immortal_Switch.Scripts.MissionSystem.Models;
using UnityEngine;

namespace Immortal_Switch.Scripts.MissionSystem
{
    public class MissionSystemManager : Singleton<MissionSystemManager>
    {
        [Header("Config")] [SerializeField] private MissionSystemDatabaseSO database;

        private IMissionSystemService Service { get; set; }
        private IMissionSystemStorage Storage { get; set; }
        public MissionSystemData Data => Storage.Data;

        /// <summary>
        /// event fire khi progress thay đổi
        /// 1: type nhiem vu
        /// 2: progress value
        /// 3: id nhiem vu
        /// </summary>
        public event Action<string, float, string> OnChangeProgress;

        // --- Private Field ---

        protected override void OnSingletonAwake()
        {
            GameEventManager.Subscribe<int>(GameEvents.OnEnemyDead, OnEnemyDead);
            GameEventManager.Subscribe<int>(GameEvents.OnStageCleared, OnStageCleared);
        }

        protected override void OnDestroy()
        {
            GameEventManager.Unsubscribe<int>(GameEvents.OnEnemyDead, OnEnemyDead);
            GameEventManager.Unsubscribe<int>(GameEvents.OnStageCleared, OnStageCleared);
            base.OnDestroy();
        }

        private void OnEnemyDead(int deadCnt)
        {
            if (deadCnt >= 1)
            {
                Service.ChangeProgress(MissionSystemEventKeys.EVENT_KILL_MONSTER, 1);
            }
        }

        private void OnStageCleared(int stage)
        {
            Service.ChangeProgress(MissionSystemEventKeys.EVENT_CLEAR_STAGE, stage);
        }

        public override UniTask InitializeAsync()
        {
            Load();
            return UniTask.CompletedTask;
        }

        public void Load()
        {
            Storage = new MissionSystemStorage(database);
            Service = new MissionSystemService(Storage);

            Storage.Load();
            Storage.Initialize();
        }

        public List<RewardEntry> ParseRewards(string rewards)
        {
            Debug.Log($"ParseRewards: {rewards}");
            var results = new List<RewardEntry>();
            var splitRewards = rewards.Split(';');

            foreach (var reward in splitRewards)
            {
                var splits = reward.Split(':');

                // có 2 key là item key va quantity
                if (splits.Length > 1)
                {
                    var itemKey = splits[0];
                    BigInteger.TryParse(splits[1], out var quantity);

                    results.Add(new RewardEntry
                    {
                        itemKey = itemKey,
                        quantity = quantity,
                    });
                }
                else
                {
                    Debug.LogError($"Reward {reward} wrong config");
                }
            }

            return results;
        }

        public DynamicHeroesGlobalSpecificationsMissionConfigRow GetMission(string id)
        {
            return database.MissionConfig.rows.Find(v => v.missionId == id);
        }

        public bool IsCompleted(DynamicHeroesGlobalSpecificationsMissionConfigRow cfg)
        {
            return Storage.Data.Missions
                .SelectMany(v => v.Value)
                .Where(v => v.Id == cfg.missionId)
                .Any(v => CheckComplete(cfg.target, cfg.eventKey) && !v.IsClaimed);
        }

        public void Claim(DynamicHeroesGlobalSpecificationsMissionConfigRow cfg)
        {
            var isCompleted = IsCompleted(cfg);

            if (!isCompleted)
            {
                Debug.LogError("MissionSystem Claim Failed");
                return;
            }

            // todo: phát thưởng và hiện phần thưởng cho user.
            Debug.Log($"rewards: {cfg.rewards}");

            // xử lý logic cho các type mission
            switch (cfg.type)
            {
                // tiep nhiem vu moi.
                case MissionSystemTypes.MAIN:
                    var nextCfg = database.MissionConfig.rows.Find(v => v.missionId == cfg.nextMission);

                    if (nextCfg != null)
                    {
                        Service.NextMission(nextCfg);
                    }
                    else
                    {
                        Service.SetIsClaimed(cfg.missionId, cfg.type, true);
                    }

                    DispatchProgressMainMission();
                    break;

                case MissionSystemTypes.DAILY:
                case MissionSystemTypes.WEEKLY:
                case MissionSystemTypes.ACHIEVEMENT:
                    Service.SetIsClaimed(cfg.missionId, cfg.type, true);
                    break;
            }
        }

        public void NotifyReady()
        {
            DispatchProgressMainMission();
        }

        private void DispatchProgressMainMission()
        {
            if (Storage.Data.Missions.TryGetValue(MissionSystemTypes.MAIN, out var list))
            {
                foreach (var entry in list)
                {
                    OnChangeProgress?.Invoke(MissionSystemTypes.MAIN, entry.Progress, entry.Id);
                }
            }
        }

        private bool CheckComplete(float target, string eventKey)
        {
            return true;
            /*switch (eventKey)
            {
                case MissionSystemEventKeys.EVENT_CLEAR_STAGE:
                    return PlayerSystemManager.Instance.Data.CurrentStage >= target;
            }

            return false;*/
        }
    }
}