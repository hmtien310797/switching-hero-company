using System.Linq;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Shared.Database;
using UnityEngine;

namespace Immortal_Switch.Scripts.Shared
{
    public class DatabaseManager : Singleton<DatabaseManager>
    {
        [Header("Database config")]
        [field: SerializeField]
        public EquipmentTierDatabaseSO EquipmentTierDatabase { get; private set; }

        [SerializeField] private DynamicHeroesGlobalSpecificationsBadWordDatabase badwordDb;

        protected override void OnSingletonAwake()
        {
            InitBadwords();
            base.OnSingletonAwake();
        }

        public override UniTask InitializeAsync()
        {
            return UniTask.CompletedTask;
        }

        private void InitBadwords()
        {
            var badwords = badwordDb.rows.Select(v => v.vi).ToArray();
            IllegalWordDetection.Init(badwords);
        }
    }
}