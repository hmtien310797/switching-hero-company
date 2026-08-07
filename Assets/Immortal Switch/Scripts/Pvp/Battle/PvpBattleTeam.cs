using System.Collections.Generic;
using Battle;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.Pvp.Battle
{
    /// <summary>
    /// 1 team PvP: 2 HeroActor + own <see cref="BattleTargetRegistry"/> (chứa hero của team đối thủ
    /// khi register chéo) + <see cref="PvpBattleContext"/> (allies = own heroes, enemies = đối thủ).
    /// </summary>
    public sealed class PvpBattleTeam
    {
        public readonly BattleTargetRegistry Registry = new();
        public readonly List<ICombatUnit> Heroes = new();   // own heroes (ref chia sẻ cho context.GetAllies)
        public readonly List<HeroActor> Actors = new();
        public PvpBattleContext Context;

        public void AddHero(HeroActor actor)
        {
            if (actor == null) return;
            Actors.Add(actor);
            Heroes.Add(actor);
        }

        public bool AllDead
        {
            get
            {
                // Empty team = chưa spawn (hoặc spawn fail) → KHÔNG phải "all dead". Trả false để
                // tránh EndBattle sớm khi Update check trong lúc await SpawnTeamAsync (team rỗng).
                if (Actors.Count == 0) return false;
                for (int i = 0; i < Actors.Count; i++)
                    if (Actors[i] != null && !Actors[i].IsDead) return false;
                return true;
            }
        }

        /// <summary>Số hero còn sống — dùng cho tie-break khi hết giờ (thắng/thua/hoà theo sống bằng nhau).</summary>
        public int AliveCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < Actors.Count; i++)
                    if (Actors[i] != null && !Actors[i].IsDead) n++;
                return n;
            }
        }

        public void Clear()
        {
            Registry.Clear();
            Heroes.Clear();
            Actors.Clear();
            Context = null;
        }
    }
}
