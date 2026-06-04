using System.Collections.Generic;
using System.Linq;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.MissionSystem.Interfaces;
using Immortal_Switch.Scripts.MissionSystem.Models;

namespace Immortal_Switch.Scripts.MissionSystem
{
    internal class MissionSystemService : IMissionSystemService
    {
        private readonly IMissionSystemStorage _storage;

        public MissionSystemService(IMissionSystemStorage storage)
        {
            _storage = storage;
        }

        public void ChangeProgress(string eventKey, int value)
        {
            var flag = false;

            foreach (var mission in _storage.Data.Missions
                         .SelectMany(pair => pair.Value.Where(mission => mission.EventKey == eventKey)))
            {
                if (!mission.IsClaimed)
                {
                    if (mission.EventKey is MissionSystemEventKeys.EVENT_CLEAR_STAGE or MissionSystemEventKeys.EVENT_HERO_LEVELUP)
                    {
                        mission.Progress = value;
                    }
                    else
                    {
                        mission.Progress += value;
                    }

                    flag = true;
                    break;
                }
            }

            if (flag)
            {
                _storage.Save();
            }
        }

        public void NextMission(DynamicHeroesGlobalSpecificationsMissionConfigRow cfg)
        {
            _storage.Data.Missions[cfg.type] = new List<MissionSystemEntry>
            {
                new()
                {
                    Id = cfg.missionId,
                    IsClaimed = false,
                    Progress = 0,
                    EventKey = cfg.eventKey,
                },
            };

            _storage.Save();
        }

        public void SetIsClaimed(string missionId, string type, bool isClaimed)
        {
            if (_storage.Data.Missions.TryGetValue(type, out var missions))
            {
                var flag = false;

                foreach (var entry in missions.Where(entry => entry.Id == missionId))
                {
                    entry.IsClaimed = isClaimed;
                    flag = true;
                    break;
                }

                if (flag)
                {
                    _storage.Save();
                }
            }
        }
    }
}