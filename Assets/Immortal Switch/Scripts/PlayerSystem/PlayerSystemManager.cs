using System;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Helper;
using Immortal_Switch.Scripts.PlayerSystem.Interfaces;
using Immortal_Switch.Scripts.PlayerSystem.Models;
using Immortal_Switch.Scripts.Shared.Helper;
using UnityEngine;

namespace Immortal_Switch.Scripts.PlayerSystem
{
    public class PlayerSystemManager : Singleton<PlayerSystemManager>
    {
        [Header("Config")]
        [SerializeField]
        private PlayerSystemDatabaseSO database;

        private IPlayerSystemService Service { get; set; }
        private IPlayerSystemStorage Storage { get; set; }

        /// <summary>
        /// event fire khi login vao ngay moi
        /// </summary>
        public event Action OnLoginNewDay;

        protected override void OnSingletonAwake()
        {
            //GameEventManager.Subscribe(GameEvents.OnStageCleared, OnStageCleared);
        }

        private void OnStageCleared()
        {
        }

        public override UniTask InitializeAsync()
        {
            Load();
            return UniTask.CompletedTask;
        }

        private void Load()
        {
            Storage = new PlayerSystemStorage();
            Service = new PlayerSystemService(Storage, database);

            database.Load();
            Storage.Load();
            CheckLoginNewDay();
        }

        private void CheckLoginNewDay()
        {
            if (Storage.Data.LastLogin == null ||
                DateTimeHelper.IsNewDay(Storage.Data.LastLogin.Value))
            {
                Storage.Data.LastLogin = DateTime.UtcNow;
                Storage.Save();
                OnLoginNewDay?.Invoke();
            }
        }
    }
}