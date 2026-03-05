using UnityEngine;

namespace Scripts.Battle
{
    public class MonsterBossController : MonsterScrepController
    {
        
        public override void InitMonster(int hid, PlayerHeroController etarget, PvEBattleController pBc, bool isBoss = true)
        {
            InitMonsterData();
            base.InitMonster(hid, etarget, pBc, isBoss);
        }

        private void InitMonsterData()
        {
            MonsterData.Health = 1000;
            MonsterData.RemainHealth = MonsterData.Health;
            MonsterData.RangeAttack = 4f;
            MonsterData.IdleIntervalTime = 3f;
            MonsterData.IdleStateTime = MonsterData.IdleIntervalTime / 2;
        }
    }
}
