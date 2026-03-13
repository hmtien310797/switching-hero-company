using Scripts.Battle;

public interface IBossSkillLogic
{
    void Initialize(MonsterBossController boss);
    void OnBattleStart();
    void OnNormalAttack();
    void OnSkillCast();
    void OnHitTaken(float damageTaken);
    void OnHpChanged();
}