using System.Collections.Generic;
using System.Linq;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.MissionSystem.Interfaces;
using Immortal_Switch.Scripts.MissionSystem.Models;
using Immortal_Switch.Scripts.Shared;

namespace Immortal_Switch.Scripts.MissionSystem
{
    internal class MissionSystemService : IMissionSystemService
    {
        private readonly IMissionSystemStorage _storage;

        public MissionSystemService(IMissionSystemStorage storage)
        {
            _storage = storage;
        }

        private MissionSystemEntry SetProgress(List<MissionSystemEntry> tasks, bool needSetProgress, string eventKey, int value)
        {
            foreach (var mission in tasks.Where(entry => entry.EventKey == eventKey))
            {
                if (!mission.IsClaimed)
                {
                    if (needSetProgress)
                    {
                        mission.Progress = value;
                    }
                    else
                    {
                        mission.Progress += value;
                    }

                    return mission;
                }
            }

            return null;
        }

        public Dictionary<string, MissionSystemEntry> ChangeProgress(string eventKey, int value)
        {
            var matches = new Dictionary<string, MissionSystemEntry>();
            var needSetProgress = NeedSetProgress(eventKey);

            if (!_storage.Data.Main.IsClaimed &&
                _storage.Data.Main.EventKey == eventKey)
            {
                if (needSetProgress)
                {
                    _storage.Data.Main.Progress = value;
                }
                else
                {
                    _storage.Data.Main.Progress += value;
                }

                matches.Add(MissionTypes.MAIN, _storage.Data.Main);
            }

            var mission = SetProgress(_storage.Data.DailyTask.Tasks, needSetProgress, eventKey, value);

            if (mission != null)
            {
                matches.Add(MissionTypes.DAILY, mission);
            }

            mission = SetProgress(_storage.Data.WeeklyTask.Tasks, needSetProgress, eventKey, value);

            if (mission != null)
            {
                matches.Add(MissionTypes.WEEKLY, mission);
            }

            mission = SetProgress(_storage.Data.RepeatTask, needSetProgress, eventKey, value);

            if (mission != null)
            {
                matches.Add(MissionTypes.REPEAT, mission);
            }

            if (matches.Count > 0)
            {
                _storage.Save();
            }

            //Debug.Log($"MatchesChangeProgress: {JsonConvert.SerializeObject(matches)} - {eventKey}");
            return matches;
        }

        public void NextMainMission(DynamicHeroesGlobalSpecificationsMissionConfigRow cfg)
        {
            _storage.Data.Main = new MissionSystemEntry
            {
                Id = cfg.missionId,
                IsClaimed = false,
                Progress = 0,
                EventKey = cfg.eventKey,
            };

            _storage.Save();
        }

        public bool SetIsClaimed(string missionId, string missionType, bool isClaimed)
        {
            var flag = false;

            switch (missionType)
            {
                case MissionTypes.MAIN:
                    if (_storage.Data.Main.Id == missionId)
                    {
                        _storage.Data.Main.IsClaimed = isClaimed;
                        flag = true;
                    }

                    break;

                case MissionTypes.DAILY:
                    foreach (var entry in _storage.Data.DailyTask.Tasks.Where(entry => entry.Id == missionId))
                    {
                        entry.IsClaimed = isClaimed;
                        flag = true;
                        break;
                    }

                    break;

                case MissionTypes.WEEKLY:
                    foreach (var entry in _storage.Data.WeeklyTask.Tasks.Where(entry => entry.Id == missionId))
                    {
                        entry.IsClaimed = isClaimed;
                        flag = true;
                        break;
                    }

                    break;

                case MissionTypes.REPEAT:
                case MissionTypes.ACHIEVEMENT:
                    foreach (var entry in _storage.Data.RepeatTask.Where(entry => entry.Id == missionId))
                    {
                        entry.IsClaimed = isClaimed;
                        flag = true;
                        break;
                    }

                    break;
            }

            if (flag)
            {
                _storage.Save();
                return true;
            }

            return false;
        }

        public void IncreasePoint(string missionType, int point)
        {
            switch (missionType)
            {
                case MissionTypes.DAILY:
                    _storage.Data.DailyTask.Point += point;
                    _storage.Save();
                    break;

                case MissionTypes.WEEKLY:
                    _storage.Data.WeeklyTask.Point += point;
                    _storage.Save();
                    break;
            }
        }

        public bool IsCompleted(DynamicHeroesGlobalSpecificationsMissionConfigRow cfg)
        {
            switch (cfg.type)
            {
                case MissionTypes.MAIN:
                    return _storage.Data.Main.Id == cfg.missionId &&
                           _storage.Data.Main.Progress >= cfg.target &&
                           !_storage.Data.Main.IsClaimed;

                case MissionTypes.DAILY:
                    foreach (var entry in _storage.Data.DailyTask.Tasks.Where(entry => entry.Id == cfg.missionId))
                    {
                        return entry.Progress >= cfg.target && !entry.IsClaimed;
                    }

                    break;

                case MissionTypes.WEEKLY:
                    foreach (var entry in _storage.Data.WeeklyTask.Tasks.Where(entry => entry.Id == cfg.missionId))
                    {
                        return entry.Progress >= cfg.target && !entry.IsClaimed;
                    }

                    break;

                case MissionTypes.REPEAT:
                    foreach (var entry in _storage.Data.RepeatTask.Where(entry => entry.Id == cfg.missionId))
                    {
                        return entry.Progress >= cfg.target && !entry.IsClaimed;
                    }

                    break;
            }

            return false;
        }

        public List<ItemRewardData> RewardGroupClaim(DynamicHeroesGlobalSpecificationsMissionPointMilesStoneRow row, bool isAdsX2)
        {
            var rewards = new List<ItemRewardData>();

            switch (row.scope)
            {
                case MissionTypes.DAILY:
                    if (_storage.Data.DailyTask.Point >= row.pointThreshold &&
                        _storage.Data.DailyTask.PointsClaimed.All(v => v.Target != row.pointThreshold))
                    {
                        // todo: nhan thuong
                        _storage.Data.DailyTask.PointsClaimed.Add(new MissionSystemPoint
                        {
                            Target = row.pointThreshold,
                            X2Claimed = false,
                        });

                        rewards.AddRange(DatabaseManager.Instance.GetRewards(row.rewards));
                        _storage.Save();
                    }
                    else if (isAdsX2)
                    {
                        var idx = _storage.Data.DailyTask.PointsClaimed.FindIndex(v => v.Target == row.pointThreshold);

                        if (idx >= 0)
                        {
                            var point = _storage.Data.DailyTask.PointsClaimed[idx];

                            // neu da nhan thuong roi moi cho nhan x2.
                            if (point != null)
                            {
                                rewards.AddRange(DatabaseManager.Instance.GetRewards(row.rewards));
                            }

                            _storage.Data.DailyTask.PointsClaimed[idx].X2Claimed = true;
                            _storage.Save();
                        }
                    }

                    break;

                case MissionTypes.WEEKLY:
                    if (_storage.Data.WeeklyTask.Point >= row.pointThreshold &&
                        _storage.Data.WeeklyTask.PointsClaimed.All(v => v.Target != row.pointThreshold))
                    {
                        // todo: nhan thuong
                        _storage.Data.WeeklyTask.PointsClaimed.Add(new MissionSystemPoint
                        {
                            Target = row.pointThreshold,
                            X2Claimed = false,
                        });

                        rewards.AddRange(DatabaseManager.Instance.GetRewards(row.rewards));
                        _storage.Save();
                    }
                    else if (isAdsX2)
                    {
                        var idx = _storage.Data.WeeklyTask.PointsClaimed.FindIndex(v => v.Target == row.pointThreshold);

                        if (idx >= 0)
                        {
                            var point = _storage.Data.WeeklyTask.PointsClaimed[idx];

                            // neu da nhan thuong roi moi cho nhan x2.
                            if (point != null)
                            {
                                rewards.AddRange(DatabaseManager.Instance.GetRewards(row.rewards));
                            }

                            _storage.Data.WeeklyTask.PointsClaimed[idx].X2Claimed = true;
                            _storage.Save();
                        }
                    }

                    break;
            }

            return rewards;
        }

        private bool NeedSetProgress(string eventKey)
        {
            return eventKey is MissionEventKeys.EVENT_CLEAR_STAGE or MissionEventKeys.EVENT_HERO_LEVELUP;
        }
    }
}