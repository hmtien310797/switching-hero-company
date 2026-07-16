using System.Linq;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
using UnityEngine;

namespace Immortal_Switch.Scripts.Shared.Badword
{
    public class BadwordManager : Singleton<BadwordManager>
    {
        [SerializeField]
        private DynamicHeroesGlobalSpecificationsBadWordDatabase badwordDb;

        protected override void OnSingletonAwake()
        {
            base.OnSingletonAwake();
            InitBadwords();
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