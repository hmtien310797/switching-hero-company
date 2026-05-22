using Spine;
using Spine.Unity;
using UnityEngine;

namespace Immortal_Switch.Scripts.Skill
{
    /// <summary>
    /// Default runtime for skill objects rendered by Spine.
    /// Spine events are treated as generic RuntimeObjectEvent and execute phases from SkillDataSO.
    /// </summary>
    public sealed class SpineSkillRuntimeObject : SkillRuntimeObject
    {
        [SerializeField] private SkeletonAnimation skeletonAnimation;
        [SerializeField] private string fallbackAnimationName;
        [SerializeField] private bool fallbackLoop;

        protected override void OnRuntimeInitialized()
        {
            if (skeletonAnimation == null)
                skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();

            if (skeletonAnimation == null)
            {
                Debug.LogWarning($"[{nameof(SpineSkillRuntimeObject)}] Missing SkeletonAnimation on {name}.");
                return;
            }

            skeletonAnimation.AnimationState.Event -= OnSpineEvent;
            skeletonAnimation.AnimationState.Complete -= OnSpineComplete;
            skeletonAnimation.AnimationState.Event += OnSpineEvent;
            skeletonAnimation.AnimationState.Complete += OnSpineComplete;

            string animationName = Config != null && !string.IsNullOrEmpty(Config.AnimationName)
                ? Config.AnimationName
                : fallbackAnimationName;

            bool loop = Config != null ? Config.LoopAnimation : fallbackLoop;

            if (!string.IsNullOrEmpty(animationName))
                skeletonAnimation.AnimationState.SetAnimation(0, animationName, loop);
        }

        private void OnSpineEvent(TrackEntry trackEntry, Spine.Event e)
        {
            if (e == null || e.Data == null)
                return;

            EmitRuntimeEvent(e.Data.Name);
        }

        private void OnSpineComplete(TrackEntry trackEntry)
        {
            if (Config != null && Config.DespawnOnAnimationComplete && !Config.LoopAnimation)
                ForceDespawn();
        }

        public override void OnDespawnedToPool()
        {
            if (skeletonAnimation != null && skeletonAnimation.AnimationState != null)
            {
                skeletonAnimation.AnimationState.Event -= OnSpineEvent;
                skeletonAnimation.AnimationState.Complete -= OnSpineComplete;
            }

            base.OnDespawnedToPool();
        }
    }
}
