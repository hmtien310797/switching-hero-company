using Cysharp.Threading.Tasks;

namespace Battle
{
    public partial class PvEBattleController
    {
        /// <summary>
        /// Dừng Chapter session để chuyển sang Dungeon trong cùng scene.
        /// Không thay đổi CurrentStage hay serverFrontierStage.
        /// </summary>
        public void SuspendForDungeon()
        {
            SetState(BattleState.Ended);
            isReadyBattle = false;
            DespawnCreepAndBoss();
            // if (battleHeroSessionController != null)
            //     battleHeroSessionController.DespawnAllHeroes();
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

    }
}
