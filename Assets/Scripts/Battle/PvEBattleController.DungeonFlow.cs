using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Hero;

namespace Battle
{
    public partial class PvEBattleController
    {
        /// <summary>
        /// Dừng Chapter session để chuyển sang Dungeon trong cùng scene.
        /// Không thay đổi CurrentStage hay serverFrontierStage.
        /// </summary>
        public async UniTask SuspendForDungeonAsync()
        {
            SetState(BattleState.Ended);
            isReadyBattle = false;

            DespawnCreepAndBoss();
            DespawnChapterHeroesForDungeon();
            pvEMapController?.ReleaseCurrentMap();

            heroTeamController?.SetHeroes(null, null);
            RefreshHeroSlotCache();

            await UniTask.Yield();
        }

        /// <summary>
        /// Khởi tạo lại đúng Chapter stage đang chơi sau khi Dungeon có kết quả.
        /// </summary>
        public async UniTask ResumeAfterDungeonAsync()
        {
            if (State != BattleState.Ended && State != BattleState.None)
                return;

            await StartAsync();
        }

        private void DespawnChapterHeroesForDungeon()
        {
            if (userDataCache == null)
                return;

            for (int slotIndex = 0; slotIndex < userDataCache.BattleHeroSlotCount; slotIndex++)
            {
                HeroActor hero = userDataCache.GetInBattleHeroActorAt(slotIndex);
                if (hero != null)
                {
                    hero.HeroSkillController?.DespawnAllInstanceOfUltimateSkillAndClassSkill();
                    heroSpawnService.Despawn(hero, OnHeroDead);
                }

                userDataCache.TrySetInBattleHeroActor(slotIndex, null);
            }

            inBattleHeroA = null;
            inBattleHeroB = null;
            heroDeadCount = 0;
        }
    }
}
