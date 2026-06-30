using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

namespace Battle
{
    /// <summary>
    /// API tối thiểu mà Hero và Skill cần từ battle mode hiện tại.
    /// Không chứa dữ liệu Chapter/Stage để Dungeon có thể implement độc lập.
    /// </summary>
    public interface IHeroBattleContext
    {
        IBattleTargetRegistry TargetRegistry { get; }

        ICombatUnit GetNearestEnemy(Vector3 position);

        void OnSelectedHeroCastUltimateSkill();
    }
}
