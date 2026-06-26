using System;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.DungeonSystem.Models;
using UnityEngine;

namespace Immortal_Switch.Scripts.DungeonSystem
{
    public class DungeonSystemManager : Singleton<DungeonSystemManager>
    {
        [Header("Database")] [SerializeField] private DynamicHeroesGlobalSpecificationsBadWordDatabase db;

        /// <summary>
        /// event fire khi chọn 1 dungeon bất kỳ để challenge
        /// </summary>
        public event Action<EDungeonType> OnSelectedChallenge;

        public override UniTask InitializeAsync()
        {
            return UniTask.CompletedTask;
        }

        public void NotifySelectedChallenge(EDungeonType challenge)
        {
            OnSelectedChallenge?.Invoke(challenge);
        }
    }
}