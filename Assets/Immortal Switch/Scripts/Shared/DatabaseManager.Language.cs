using System.Collections.Generic;
using System.Linq;
using Game.Configs.Generated;
using UnityEngine;

namespace Immortal_Switch.Scripts.Shared
{
    public partial class DatabaseManager
    {
        [SerializeField]
        private DynamicHeroesGlobalSpecificationsConfigLanguageDatabase languageDb;

        public IReadOnlyList<DynamicHeroesGlobalSpecificationsConfigLanguageRow> GetLanguagesReleased()
        {
            return languageDb.rows.Where(v => v.status == "released").ToList();
        }
    }
}