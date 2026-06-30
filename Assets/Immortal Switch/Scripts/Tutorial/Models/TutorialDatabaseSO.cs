using System.Collections.Generic;
using System.Linq;
using Game.Configs.Generated;
using UnityEngine;

namespace Immortal_Switch.Scripts.Tutorial.Models
{
    [CreateAssetMenu(fileName = "TutorialDatabase", menuName = "ScriptableObjects/Tutorial/Database")]
    public class TutorialDatabaseSO : ScriptableObject
    {
        /// <summary>
        /// tut database
        /// </summary>
        [field: SerializeField]
        public DynamicHeroesGlobalSpecificationsTutConfigDatabase TutDb { get; private set; }

        public List<DynamicHeroesGlobalSpecificationsTutConfigRow> GetTutorials(int stepId)
        {
            var list = new List<DynamicHeroesGlobalSpecificationsTutConfigRow>();

            foreach (var entry in TutDb.rows
                         .Where(entry => entry.stepId == stepId))
            {
                list.Add(entry);

                if (entry.nextStepId != 0)
                {
                    list.AddRange(GetTutorials(entry.nextStepId));
                }

                break;
            }

            return list;
        }
    }
}