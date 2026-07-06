using System.Linq;
using Immortal_Switch.Scripts.TransmutationSystem.Interfaces;
using Immortal_Switch.Scripts.TransmutationSystem.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Immortal_Switch.Scripts.TransmutationSystem
{
    internal class TransmutationSystemStorage : ITransmutationSystemStorage
    {
        /// <summary>
        /// save key localstorage.
        /// </summary>
        private const string SAVE_KEY = nameof(TransmutationSystem);

        public TransmutationSystemData Data { get; private set; }
        private readonly TransmutationSystemDatabaseSO _db;

        public TransmutationSystemStorage(TransmutationSystemDatabaseSO db)
        {
            _db = db;
        }

        public void Save()
        {
            ES3.Save(SAVE_KEY, Data);
            Debug.Log($"{SAVE_KEY}: Save {JsonConvert.SerializeObject(Data)}");
        }

        public void Load()
        {
            var first = _db.LevelConfig.rows.FirstOrDefault();
            Data = ES3.KeyExists(SAVE_KEY) ? ES3.Load<TransmutationSystemData>(SAVE_KEY) : new TransmutationSystemData();
            Debug.Log($"{SAVE_KEY}: Load {JsonConvert.SerializeObject(Data)}");

            if (first != null &&
                Data.Level < first.level)
            {
                Data.UpdateLevel(first.level);
                Data.UpdateEnergy(0);
                Data.UpdateExp(0);
                Save();
            }
        }
    }
}