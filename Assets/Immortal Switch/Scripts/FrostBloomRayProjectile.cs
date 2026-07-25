using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Combat;
using Immortal_Switch.Scripts.Skill;
using Immortal_Switch.Scripts.StatSystem;
using Spine.Unity;
using UnityEngine;

namespace Immortal_Switch.Scripts
{
    public class FrostBloomRayProjectile : BulletProjectile
    {
        [SerializeField] private SkeletonAnimation frostBloomProjectile;
        [SerializeField] private SkeletonAnimation frostBloomImpact;
        [SerializeField] private SkillAreaRuntime skillAreaRuntime;
        [SerializeField] private Collider collider;

        private bool stopMoving;
        protected override void Update()
        {
            if (!isInitialized)
                return;

            if (stopMoving)
                return;

            float deltaTime = Time.deltaTime;

            transform.position +=
                direction * speed * deltaTime;

            timer += deltaTime;

            if (timer >= lifeTime)
            {
                DespawnSelfAsync();
            }
        }

        public override void Setup(SkillRuntimeObject controller, ICombatUnit source, SkillRuntimeContext context, SkillRuntimeObjectConfig Config, SkillExecutor executor,
            SkillTargetResolver targetResolver, ISkillObjectSpawner skillObjectSpawner, Vector3 moveDirection,
            float bulletSpeed, float bulletLifeTime, float damage)
        {
            base.Setup(controller, source, context, Config, executor, targetResolver, skillObjectSpawner, moveDirection, bulletSpeed, bulletLifeTime, damage);
            frostBloomProjectile.gameObject.SetActive(true);
            frostBloomImpact.gameObject.SetActive(false);
            frostBloomProjectile.AnimationState.SetAnimation(0, "animation", true);
            stopMoving = false;
            collider.enabled = true;
        }

        protected override void OnTriggerEnter(Collider other)
        {
            if (!isInitialized)
                return;

            if (!IsInLayerMask(
                    other.gameObject.layer,
                    enemyLayer))
            {
                return;
            }

            ICombatUnit targetCombatUnit =
                other.GetComponent<ICombatUnit>();

            if (targetCombatUnit == null)
                return;

            collider.enabled = false;
            
            stopMoving = true;
            DamageResult damageResult =
                DamageCalculator.CalculateDamage(
                    sourceCombatUnit,
                    targetCombatUnit,
                    damage
                );

            targetCombatUnit.TakeDamage(damageResult);
            frostBloomProjectile.gameObject.SetActive(false);
            frostBloomImpact.gameObject.SetActive(true);
            frostBloomImpact.AnimationState.SetAnimation(0, "animation", false);
            
            if (Context == null || Context.SkillData == null || Executor == null)
                return;

            SkillRuntimeContext runtimeContext = Context.CloneForRuntimeObject(controller, transform.position);
            Context.SkillData.GetPhasesByEvent(
                Context.SkillLevel,
                SkillPhaseTriggerType.RuntimeObjectSpineEvent,
                "finalhit",
                controller.SkillPhaseData
            );

            for (int i = 0; i < controller.SkillPhaseData.Count; i++)
            {
                Executor.ExecutePhase(runtimeContext, controller.SkillPhaseData[i]);
            }
            
            DespawnSelfAsync(1f).Forget();
        }
    }
}