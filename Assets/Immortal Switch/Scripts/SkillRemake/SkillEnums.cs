namespace Immortal_Switch.Scripts.Skill
{
    public enum SkillOwnerType
    {
        ClassSkill,
        UltimateSkill,
        PassiveSkill
    }

    public enum SkillRuntimeVisualType
    {
        Instant,
        HeroSpineAnimation,
        SpawnedSkillObject,
        ProjectileOnly,
        HeroSpineAndSpawnedSkillObject,
        HeroSpineObjectAndProjectile,
        SpawnProjectilePatternBehavior,
        SpawnHomingProjectile
    }
    
    public enum SkillPhaseTriggerType
    {
        // Legacy name kept so existing data still compiles.
        SpineEvent,

        // New generic event fired by spawned skill objects.
        RuntimeObjectSpineEvent,
        
        Delay,
        NormalizedTime,
        OnProjectileHit,
        OnAreaTick,
        Manual
    }

    public enum SkillActionType
    {
        DealDamage,
        ApplyDot,
        ApplyBuff,
        ApplyDebuff,
        SpawnProjectile,
        SpawnArea,
        AddStack,
        TriggerSkill
    }

    public enum SkillTargetType
    {
        CurrentTarget,
        Self,
        NearestEnemy,
        AllEnemies,
        AllAllies,
        RandomEnemy,
        LowestHpEnemy,
        HighestHpEnemy,
        AreaAroundTarget,
        AreaAroundSelf,

        // Useful for spawned skill objects: damage around the runtime object position.
        AreaAroundCastPosition
    }

    public enum SkillTargetSelectType
    {
        CurrentTarget,
        NearestEnemy,
        Self
    }

    public enum SkillScalingStat
    {
        Attack,
        MaxHp,
        Defense
    }

    public enum SkillAreaShape
    {
        Circle,
        Box
    }

    public enum SkillAreaPositionType
    {
        Self,
        Target,
        CastPosition,
        ForwardOffset
    }

    public enum SkillProjectileMoveType
    {
        Straight,
        Homing
    }

    public enum SkillProjectileHitDetectionType
    {
        DistanceToTarget,
        Collider
    }

    public enum SkillSpawnPositionType
    {
        Self,
        Target,
        ProjectileSpawnPoint,
        CustomSocket,
        CastPosition
    }

    public enum SkillFollowType
    {
        None,
        FollowSelf,
        FollowTarget
    }

    public enum SkillTriggerEventType
    {
        None,
        OnSpineEvent,
        OnHit,
        OnDamageDealt,
        OnCriticalHit,
        OnKill,
        OnTakeDamage,
        OnCastSkill,
        OnCastClassSkill,
        OnCastUltimateSkill,
        OnSkillHit,
        OnBattleStart,
        OnBattleEnd,
        OnHpBelowPercent,
        OnStackReached
    }

    public enum SkillEventSourceFilter
    {
        Owner,
        Ally,
        OwnerAndAlly
    }
}
