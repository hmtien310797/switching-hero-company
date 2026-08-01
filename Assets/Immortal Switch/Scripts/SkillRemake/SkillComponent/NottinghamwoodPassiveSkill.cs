using Battle;
using Immortal_Switch.Scripts.Combat;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Pooling;
using Immortal_Switch.Scripts.StatSystem;
using Spine.Unity;
using UnityEngine;

namespace Immortal_Switch.Scripts.Skill
{
    /// <summary>
    /// Custom passive skill handler for hero Nottinghamwood.
    /// Unlike standard passives that apply stat buffs, Nottinghamwood's passive
    /// plays progressive stack animations on a separate Spine track and spawns
    /// projectiles at max stacks that target random enemies regardless of distance.
    /// </summary>
    public class NottinghamwoodPassiveSkill : MonoBehaviour
    {
        [Header("Projectile")]
        [SerializeField] private HeroProjectile projectilePrefab;
        [SerializeField] private string projectileAddressKey;
        [SerializeField] private float damageValue = 100f;

        [Header("Animation")]
        [SerializeField] private int animationTrack = 1;
        [SerializeField] private int maxStacks = 4;

        // Cached references
        private HeroSkillController skillController;
        private SkeletonAnimation skeletonAnimation;
        private HeroActor owner;
        private SkillDataSO passiveSkillData;
        private IHeroBattleContext battleContext;

        // State
        private int currentStack;
        private bool isInitialized;

        #region Unity Lifecycle

        private void Awake()
        {
            skillController = GetComponent<HeroSkillController>();
            owner = GetComponent<HeroActor>();
            skeletonAnimation = GetComponentInChildren<SkeletonAnimation>(true);
        }

        private void OnEnable()
        {
            GameEventManager.Subscribe<int>(GameEvents.OnStageCleared, OnStageCleared);
            GameEventManager.Subscribe(GameEvents.OnStageLost, OnStageLost);
        }

        private void OnDisable()
        {
            DespawnActiveProjectiles();
            GameEventManager.Unsubscribe<int>(GameEvents.OnStageCleared, OnStageCleared);
            GameEventManager.Unsubscribe(GameEvents.OnStageLost, OnStageLost);
            ClearPassiveAnimations();
            ResetState();
        }

        #endregion

        #region Public API (called by HeroSkillController)

        /// <summary>
        /// Initializes the passive handler with skill data and battle context.
        /// Called by HeroSkillController.BuildPassiveRuntime().
        /// </summary>
        public void Init(SkillDataSO skillData, IHeroBattleContext context)
        {
            passiveSkillData = skillData;
            battleContext = context;

            ResetState();
            isInitialized = true;
        }

        /// <summary>
        /// Handles skill events forwarded by HeroSkillController.
        /// Filters for basic attack hits on this owner to accumulate stacks.
        /// </summary>
        public void HandleEvent(SkillEventContext context)
        {
            if (!isInitialized || passiveSkillData == null)
                return;

            if (owner == null || owner.IsDead || !gameObject.activeInHierarchy)
                return;

            // Only process events sourced from this owner
            if (context.Source != owner)
                return;

            // Only process basic attack hits (Skill == null means basic attack)
            if (context.EventType != SkillTriggerEventType.OnHit)
                return;

            if (context.Skill != null)
                return;

            // Respect cooldown: don't accumulate stacks while on cooldown
            if (skillController != null && !skillController.IsCooldownReady(passiveSkillData))
                return;

            AddStack();
        }

        /// <summary>
        /// Resets all state: clears stacks and passive animations.
        /// Called when hero switches out or stage transitions.
        /// </summary>
        public void Reset()
        {
            ClearPassiveAnimations();
            ResetState();
        }

        /// <summary>
        /// Clears passive animations on the dedicated track.
        /// Safe to call even if skeleton is not available.
        /// </summary>
        public void ClearPassiveAnimations()
        {
            if (skeletonAnimation == null || skeletonAnimation.AnimationState == null)
                return;

            skeletonAnimation.AnimationState.SetEmptyAnimation(animationTrack, 0f);
            skeletonAnimation.AnimationState.Apply(skeletonAnimation.Skeleton);
            skeletonAnimation.AnimationState.ClearTrack(animationTrack);
            skeletonAnimation.Update(0f);
        }

        #endregion

        #region Internal Logic

        private void AddStack()
        {
            currentStack++;

            if (currentStack >= maxStacks)
            {
                // Trigger at max stacks: clear animations, fire projectiles, reset
                ClearPassiveAnimations();
                FireProjectiles();
                StartPassiveCooldown();
                currentStack = 0;
            }
            else
            {
                // Update progressive animation for current stack
                UpdatePassiveAnimation();
            }
        }

        private void UpdatePassiveAnimation()
        {
            if (skeletonAnimation == null || skeletonAnimation.AnimationState == null)
                return;

            if (currentStack <= 0 || currentStack > maxStacks)
                return;

            string animName = $"passive_{currentStack}";
            skeletonAnimation.AnimationState.SetAnimation(animationTrack, animName, true);
        }

        private void FireProjectiles()
        {
            if (owner == null || battleContext == null)
                return;

            for (int i = 0; i < maxStacks; i++)
            {
                ICombatUnit target = battleContext.GetRandomEnemyAlive();
                if (target == null || !target.IsUnityAlive())
                    continue;

                Vector3 spawnPosition = owner.Position + Vector3.up * 0.8f;

                HeroProjectile projectile = null;

                if (!string.IsNullOrEmpty(projectileAddressKey) &&
                    AddressablePoolService.Instance.HasPool(projectileAddressKey))
                {
                    projectile = AddressablePoolService.Instance.Spawn<HeroProjectile>(
                        projectileAddressKey,
                        spawnPosition,
                        Quaternion.identity
                    );
                }

                if (projectile == null && projectilePrefab != null)
                {
                    projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
                }

                if (projectile != null)
                {
                    projectile.Init(target, owner, damageValue);
                }
            }
        }

        private void StartPassiveCooldown()
        {
            if (skillController == null || passiveSkillData == null)
                return;

            skillController.StartCooldownForPassive(passiveSkillData);
        }

        private void ResetState()
        {
            currentStack = 0;
        }

        private void DespawnActiveProjectiles()
        {
            if (!string.IsNullOrEmpty(projectileAddressKey))
                AddressablePoolService.Instance.DespawnAllActive(projectileAddressKey);
        }

        #endregion

        #region Game Event Handlers

        private void OnStageCleared(int stage)
        {
            DespawnActiveProjectiles();
            Reset();
        }

        private void OnStageLost()
        {
            DespawnActiveProjectiles();
            Reset();
        }

        #endregion
    }
}
