using Spine;
using Spine.Unity;
using UnityEngine;
using AnimationState = Spine.AnimationState;

namespace Immortal_Switch.Scripts.Skill
{
    /// <summary>
    /// Default runtime for skill objects rendered by Spine.
    /// Spine events are treated as generic RuntimeObjectEvent and execute phases from SkillDataSO.
    /// </summary>
    public class SpineSkillRuntimeObject : SkillRuntimeObject
    {
        [SerializeField] protected SkeletonAnimation skeletonAnimation;
        [SerializeField] protected string fallbackAnimationName;
        [SerializeField] protected bool fallbackLoop;

        protected override void OnRuntimeInitialized(object arg)
        {
            base.OnRuntimeInitialized(arg);
            
            if (skeletonAnimation == null)
            {
                Debug.LogError($"[{nameof(SpineSkillRuntimeObject)}] Missing SkeletonAnimation on {name}.");
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

            if (e.Data.Name == "finalhit")
            {
                GameCameraController.Instance.ShakeCamera();
            }
        }

        protected void OnSpineComplete(TrackEntry trackEntry)
        {
            if (Config != null && Config.DespawnOnAnimationComplete && !Config.LoopAnimation)
                ForceDespawn();
        }

        protected override void OnDespawnedToPool()
        {
            if (skeletonAnimation != null && skeletonAnimation.AnimationState != null)
            {
                skeletonAnimation.AnimationState.Event -= OnSpineEvent;
                skeletonAnimation.AnimationState.Complete -= OnSpineComplete;
                
                AnimationState animationState = skeletonAnimation.AnimationState;
                Skeleton skeleton = skeletonAnimation.Skeleton;

                animationState.ClearTracks();
                skeleton.SetToSetupPose();
            }

            base.OnDespawnedToPool();
        }
    }
}
