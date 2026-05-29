using Immortal_Switch.Scripts.PlayerSystem.Interfaces;
using Immortal_Switch.Scripts.PlayerSystem.Models;
using UnityEngine;

namespace Immortal_Switch.Scripts.PlayerSystem
{
    internal class PlayerSystemService : IPlayerSystemService
    {
        private readonly IPlayerSystemStorage _storage;
        private readonly PlayerSystemDatabaseSO _database;

        public PlayerSystemService(IPlayerSystemStorage storage, PlayerSystemDatabaseSO database)
        {
            _storage = storage;
            _database = database;
        }

        public void AddExp(int quantity)
        {
            UpdateExp(quantity + _storage.Data.Exp);
        }

        public void LevelUp()
        {
            _storage.Data.UpdateLevel(Mathf.Min(_storage.Data.Level + 1, _database.Level.LevelCount));
            _storage.Save();
        }

        public void UpdateExp(int quantity)
        {
            _storage.Data.UpdateExp(quantity);
            _storage.Save();
        }
    }
}