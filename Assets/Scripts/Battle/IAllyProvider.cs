using System.Collections.Generic;
using Immortal_Switch.Scripts.StatSystem;

namespace Battle
{
    /// <summary>
    /// Allies provider cho 1 team trong combat. <b>FLAGGED (PvP thêm):</b> gỡ coupling
    /// <c>SkillTargetResolver.ResolveAllAllies</c> + <c>SkillExecutor.ResolveAllyTargets</c> →
    /// <c>UserDataCache.inBattleHeroes</c> (chỉ đúng cho player). PvE: provider null → code fallback
    /// UserDataCache (behavior cũ, không đổi). PvP: provider trả hero của team đó.
    /// </summary>
    public interface IAllyProvider
    {
        IReadOnlyList<ICombatUnit> GetAllies();
    }
}
