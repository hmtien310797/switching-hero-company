using UnityEngine;
using Battle;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.Skill
{
    public sealed class SkillRuntimeContext
    {
        public HeroActor Caster;
        public ICombatUnit MainTarget;
        public SkillDataSO SkillData;
        public int SkillLevel;
        public Vector3 CastPosition;
        public Vector3 TargetPosition;
        public IHeroBattleContext BattleContext;
        public HeroSkillController SkillController;
        public SkillRuntimeObject RuntimeObject;

        public SkillRuntimeContext CloneForTarget(ICombatUnit target)
        {
            return new SkillRuntimeContext
            {
                Caster = Caster,
                MainTarget = target,
                SkillData = SkillData,
                SkillLevel = SkillLevel,
                CastPosition = CastPosition,
                TargetPosition = target != null ? target.Position : TargetPosition,
                BattleContext = BattleContext,
                SkillController = SkillController,
                RuntimeObject = RuntimeObject
            };
        }

        public SkillRuntimeContext CloneForRuntimeObject(SkillRuntimeObject runtimeObject, Vector3 runtimePosition)
        {
            var mainTarget = MainTarget;
            bool hasValidTarget = mainTarget.IsUnityAlive();
            return new SkillRuntimeContext
            {
                Caster = Caster,
                MainTarget = MainTarget,
                SkillData = SkillData,
                SkillLevel = SkillLevel,
                CastPosition = runtimePosition,
                TargetPosition = hasValidTarget
                    ? mainTarget.Position
                    : TargetPosition,
                BattleContext = BattleContext,
                SkillController = SkillController,
                RuntimeObject = runtimeObject
            };
        }
    }
}
