using System.Collections.Generic;
using System.Linq;
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
            if (!Data.Missions.TryGetValue(MissionSystemTypes.MAIN, out var list) ||
                list.Count == 0)
            {
                var firstMainMission = _db.MissionConfig.rows.FirstOrDefault(v => v.type == MissionSystemTypes.MAIN);

                if (firstMainMission != null)
                {
                    Data.Missions.TryAdd(MissionSystemTypes.MAIN, new List<MissionSystemEntry>
                    {
                        new()
                        {
                            EventKey = firstMainMission.eventKey,
                            Id = firstMainMission.missionId,
                            IsClaimed = false,
                            Progress = 0,
                        },
                    });

                    Save();
                }
            }
        }
    }
}