using System;
using System.Collections.Generic;
using System.Linq;
using Immortal_Switch.Scripts.MissionSystem.Interfaces;
using Immortal_Switch.Scripts.MissionSystem.Models;

namespace Immortal_Switch.Scripts.MissionSystem
{
    internal class MissionSystemStorage : IMissionSystemStorage
    {
        private readonly MissionSystemDatabaseSO _db;
        public MissionSystemData Data { get; private set; }

        /// <summary>
        /// Callback fired after every Save(). Manager sets this to fire-and-forget server sync.
        /// </summary>
        public Action OnAfterSave { get; set; }

        public MissionSystemStorage(MissionSystemDatabaseSO db)
        {
            _db = db;
        }

        public void Save()
        {
            //Debug.Log($"{SAVE_KEY}: Save {JsonConvert.SerializeObject(Data)}");
            OnAfterSave?.Invoke();
        }

        public void Load()
        {
            Data = new MissionSystemData();

            //Debug.Log($"{SAVE_KEY}: Load {JsonConvert.SerializeObject(Data)}");
        }

        /// <summary>
        /// Overrides Data with server-loaded state without writing to ES3.
        /// The next Save() call will persist it locally.
        /// </summary>
        public void LoadFromData(MissionSystemData data)
        {
            Data = data;
        }

        public void ResetDaily()
        {
            Data.DailyTask = new MissionSystemTask
            {
                Tasks = _db.MissionConfig.rows
                    .FindAll(v => v.type == MissionTypes.DAILY)
                    .Select(v => new MissionSystemEntry
                    {
                        Id = v.missionId,
                        Progress = 0,
                        EventKey = v.eventKey,
                        IsClaimed = false,
                    })
                    .ToList(),
                Point = 0,
                PointsClaimed = new List<MissionSystemPoint>(),
            };
        }

        public void ResetWeekly()
        {
            Data.WeeklyTask = new MissionSystemTask
            {
                Tasks = _db.MissionConfig.rows
                    .FindAll(v => v.type == MissionTypes.WEEKLY)
                    .Select(v => new MissionSystemEntry
                    {
                        Id = v.missionId,
                        Progress = 0,
                        EventKey = v.eventKey,
                        IsClaimed = false,
                    })
                    .ToList(),
                Point = 0,
                PointsClaimed = new List<MissionSystemPoint>(),
            };
        }

        public void InitRepeat()
        {
            Data.RepeatTask = _db.MissionConfig.rows
                .FindAll(v => v.type == MissionTypes.REPEAT)
                .Select(v => new MissionSystemEntry
                {
                    Id = v.missionId,
                    Progress = 0,
                    EventKey = v.eventKey,
                    IsClaimed = false,
                })
                .ToList();
        }

        public void InitMain()
        {
            var main = _db.MissionConfig.rows.FirstOrDefault(v => v.type == MissionTypes.MAIN);

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
            var changed = false;

            if (Data == null)
            {
                Data = new MissionSystemData();
                changed = true;
            }

            if (Data.Main == null ||
                string.IsNullOrWhiteSpace(Data.Main.Id))
            {
                InitMain();
                changed = true;
            }

            if (Data.DailyTask?.Tasks == null)
            {
                ResetDaily();
                changed = true;
            }
            else
            {
                changed |= ReconcileTaskEntries(Data.DailyTask.Tasks, MissionTypes.DAILY);

                if (Data.DailyTask.PointsClaimed == null)
                {
                    Data.DailyTask.PointsClaimed = new List<MissionSystemPoint>();
                    changed = true;
                }
            }

            if (Data.WeeklyTask?.Tasks == null)
            {
                ResetWeekly();
                changed = true;
            }
            else
            {
                changed |= ReconcileTaskEntries(Data.WeeklyTask.Tasks, MissionTypes.WEEKLY);

                if (Data.WeeklyTask.PointsClaimed == null)
                {
                    Data.WeeklyTask.PointsClaimed = new List<MissionSystemPoint>();
                    changed = true;
                }
            }

            if (Data.RepeatTask == null)
            {
                InitRepeat();
                changed = true;
            }
            else
            {
                changed |= ReconcileTaskEntries(Data.RepeatTask, MissionTypes.REPEAT);
            }

            if (changed)
            {
                Save();
            }
        }

        private bool ReconcileTaskEntries(List<MissionSystemEntry> tasks, string missionType)
        {
            var changed = false;
            var configs = _db.MissionConfig.rows.FindAll(v => v.type == missionType);

            foreach (var cfg in configs)
            {
                var entry = tasks.Find(v => v.Id == cfg.missionId);

                if (entry == null)
                {
                    tasks.Add(new MissionSystemEntry
                    {
                        Id = cfg.missionId,
                        Progress = 0,
                        EventKey = cfg.eventKey,
                        IsClaimed = false,
                    });

                    changed = true;
                    continue;
                }
            }

            return changed;
        }
    }
}