using System.Collections.Generic;
using System.Linq;
using Immortal_Switch.Scripts.Tutorial.Interfaces;
using UnityEngine;

namespace Immortal_Switch.Scripts.Tutorial
{
    public class TutorialService : ITutorialService
    {
        private readonly ITutorialStorage _storage;

        public TutorialService(ITutorialStorage storage)
        {
            _storage = storage;
        }

        public void Complete(int tutorialGuideId)
        {
            if (tutorialGuideId < 1)
            {
                Debug.LogError($"[TutorialService] Tutorial start id must be greater than zero: {tutorialGuideId}");
                return;
            }

            var set = _storage.Data.CompletedIds.ToHashSet();
            set.Add(tutorialGuideId);

            _storage.Data.CompletedIds = new List<int>(set);
            _storage.Save();
        }
    }
}