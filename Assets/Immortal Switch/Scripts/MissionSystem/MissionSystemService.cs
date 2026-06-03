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
            foreach (var mission in _storage.Data.Missions
                         .SelectMany(pair => pair.Value.Where(mission => mission.EventKey == eventKey)))
            {
                mission.Progress += value;
            }

            _storage.Save();
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
        }
    }
}