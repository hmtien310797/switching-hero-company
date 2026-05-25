using Battle;

namespace Immortal_Switch.Scripts.Boss
{
    public interface IBossSkillLogic
    {
        void Initialize(BossActor boss);
        void OnBattleStart();
        void OnNormalAttack();
        void OnSkillCast();
        void OnHitTaken(float damageTaken);
        void OnHpChanged();
    }
}