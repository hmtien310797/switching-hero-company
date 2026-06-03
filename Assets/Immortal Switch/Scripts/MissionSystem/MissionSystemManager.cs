using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
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
        private volatile bool _isInitialized;

        protected override void OnSingletonAwake()
        {
            //GameEventManager.Subscribe<int>(GameEvents.OnEnemyDead, OnEnemyDead);
        }

        protected override void OnDestroy()
        {
            //GameEventManager.Unsubscribe<int>(GameEvents.OnEnemyDead, OnEnemyDead);
            base.OnDestroy();
        }

        private void OnEnemyDead(int deadCnt)
        {
            if (deadCnt >= 1)
            {
                Service.ChangeProgress(MissionSystemEventKeys.EVENT_KILL_MONSTER, 1);
            }
        }

        public override UniTask InitializeAsync()
        {
            Load();
            return UniTask.CompletedTask;
        }

        public void Load()
        {
            if (_isInitialized)
            {
                return;
            }

            Storage = new MissionSystemStorage(database);
            Service = new MissionSystemService(Storage);
            _isInitialized = true;

            Storage.Load();
            Storage.Initialize();
            _DispatchProgressMainMission();
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
                .Any(v => v.Progress >= cfg.target && !v.IsClaimed);
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

            // xử lý logic cho các type mission
            switch (cfg.type)
            {
                // tiep nhiem vu moi.
                case MissionSystemTypes.MAIN:
                    Service.NextMission(cfg);
                    break;

                case MissionSystemTypes.DAILY:
                    break;

                case MissionSystemTypes.WEEKLY:
                    break;

                case MissionSystemTypes.ACHIEVEMENT:
                    break;
            }
        }

        private void _DispatchProgressMainMission()
        {
            if (Storage.Data.Missions.TryGetValue(MissionSystemTypes.MAIN, out var list))
            {
                foreach (var entry in list)
                {
                    OnChangeProgress?.Invoke(MissionSystemTypes.MAIN, entry.Progress, entry.Id);
                }
            }
        }
    }
}