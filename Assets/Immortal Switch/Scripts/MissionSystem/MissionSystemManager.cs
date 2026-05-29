using System;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.MissionSystem.Interfaces;
using Immortal_Switch.Scripts.MissionSystem.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Immortal_Switch.Scripts.MissionSystem
{
    public class MissionSystemManager : Singleton<MissionSystemManager>
    {
        [Header("Config")] [SerializeField] private MissionSystemDatabaseSO database;

        private IMissionSystemService Service { get; set; }
        private IMissionSystemStorage Storage { get; set; }
        public MissionSystemData Data => Storage.Data;

        public MissionEntry? Entry => database.GetEntry(Data.Id);

        /// <summary>
        /// event fire khi progress thay đổi
        /// 1: type nhiem vu
        /// 2: progress
        /// </summary>
        public event Action<EMissionSystemType, int> OnUpdateProgress;

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
            if (deadCnt < 1)
            {
                return;
            }

            var entry = Entry;

            if (entry is { type: EMissionSystemType.KillMonsters, } &&
                entry.Value.target != Storage.Data.Progress)
            {
                UpdateProgress(entry.Value.type, Storage.Data.Progress + 1);
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
            Service = new MissionSystemService(Storage, database);
            _isInitialized = true;

            database.Load();
            Storage.Load();
            Storage.Initialize();
            _DispatchProgress();
        }

        public void UpdateProgress(EMissionSystemType type, int progress)
        {
            var isUpdate = Service.UpdateProgress(type, progress);

            if (isUpdate)
            {
                _DispatchProgress();
            }
        }

        public bool IsComplete => Service.IsComplete();

        public void Complete()
        {
            if (IsComplete)
            {
                Storage.Data.SetIsClaimed(true);
                Service.Complete();
                _DispatchProgress();
            }
        }

        private void _DispatchProgress()
        {
            var entry = Entry;

            if (entry != null)
            {
                // fire event
                OnUpdateProgress?.Invoke(entry.Value.type, Data.Progress);
            }
        }
    }
}