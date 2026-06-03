using Immortal_Switch.Scripts.MissionSystem.Interfaces;
using Immortal_Switch.Scripts.MissionSystem.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Immortal_Switch.Scripts.MissionSystem
{
    internal class MissionSystemStorage : IMissionSystemStorage
    {
        private readonly MissionSystemDatabaseSO _db;
        public MissionSystemData Data { get; private set; }

        /// <summary>
        /// save key localstorage.
        /// </summary>
        private const string SAVE_KEY = nameof(MissionSystem);

        public MissionSystemStorage(MissionSystemDatabaseSO db)
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
            Data = ES3.KeyExists(SAVE_KEY) ? ES3.Load<MissionSystemData>(SAVE_KEY) : new MissionSystemData();
            Debug.Log($"{SAVE_KEY}: Load {JsonConvert.SerializeObject(Data)}");
        }

        public void Initialize()
        {
            // todo: add mission type vào dữ liệu của users.
        }
    }
}