using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.ItemSystem.Models;
using UnityEngine;

namespace Immortal_Switch.Scripts.ItemSystem
{
    public class ItemSystemManager : Singleton<ItemSystemManager>
    {
        /// <summary>
        /// database cfg
        /// </summary>
        [field: SerializeField]
        public ItemSystemDatabaseSO Database { get; private set; }

        public override UniTask InitializeAsync()
        {
            return UniTask.CompletedTask;
        }
    }
}