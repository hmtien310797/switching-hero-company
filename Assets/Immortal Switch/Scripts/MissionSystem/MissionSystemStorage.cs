using System;
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

        public void ResetDaily()
        {
            Data.DailyTask = new MissionSystemTask
            {
                Tasks = _db.MissionConfig.rows
                    .FindAll(v => v.type == MissionSystemTypes.DAILY)
                    .Select(v => new MissionSystemEntry
                    {
                        Id = v.missionId,
                        Progress = 0,
                        EventKey = v.eventKey,
                        IsClaimed = false,
                    })
                    .ToArray(),
                Point = 0,
                PointsClaimed = Array.Empty<MissionSystemPoint>(),
            };
        }

        public void ResetWeekly()
        {
            Data.WeeklyTask = new MissionSystemTask
            {
                Tasks = _db.MissionConfig.rows
                    .FindAll(v => v.type == MissionSystemTypes.WEEKLY)
                    .Select(v => new MissionSystemEntry
                    {
                        Id = v.missionId,
                        Progress = 0,
                        EventKey = v.eventKey,
                        IsClaimed = false,
                    })
                    .ToArray(),
                Point = 0,
                PointsClaimed = Array.Empty<MissionSystemPoint>(),
            };
        }

        public void InitRepeat()
        {
            Data.RepeatTask = _db.MissionConfig.rows
                .FindAll(v => v.type == MissionSystemTypes.REPEAT)
                .Select(v => new MissionSystemEntry
                {
                    Id = v.missionId,
                    Progress = 0,
                    EventKey = v.eventKey,
                    IsClaimed = false,
                })
                .ToArray();
        }

        public void InitMain()
        {
            var main = _db.MissionConfig.rows.FirstOrDefault(v => v.type == MissionSystemTypes.MAIN);

            if (main != null)
            {
                Data.Main = new MissionSystemEntry
                {
                    EventKey = main.eventKey,
                    Id = main.missionId,
                    IsClaimed = false,
                    Progress = 0,
                };
            }
        }

        public void Initialize()
        {
            if (Data.Main == null ||
                string.IsNullOrWhiteSpace(Data.Main.Id))
            {
                InitMain();
                ResetDaily();
                ResetWeekly();
                InitRepeat();
                Save();
            }
        }
    }
}