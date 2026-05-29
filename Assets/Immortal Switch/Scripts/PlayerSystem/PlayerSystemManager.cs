using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.PlayerSystem.Interfaces;
using Immortal_Switch.Scripts.PlayerSystem.Models;
using UnityEngine;

namespace Immortal_Switch.Scripts.PlayerSystem
{
    public class PlayerSystemManager : Singleton<PlayerSystemManager>
    {
        [Header("Config")] [SerializeField] private PlayerSystemDatabaseSO database;

        private IPlayerSystemService Service { get; set; }
        private IPlayerSystemStorage Storage { get; set; }
        public PlayerSystemData Data => Storage.Data;

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
        }
    }
}