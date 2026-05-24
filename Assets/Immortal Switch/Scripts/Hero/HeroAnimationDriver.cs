using System;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace Immortal_Switch.Scripts.Hero
{
    public class HeroAnimationDriver : MonoBehaviour
    {
        [Header("Spine")]
        [SerializeField] private SkeletonAnimation skeletonAnimation;

        [Header("Base Animation Names")]
        [SerializeField] private string spawnAnim = "spawn";
        [SerializeField] private string idleAnim = "idle";
        [SerializeField] private string runAnim = "run";
        [SerializeField] private string deadAnim = "dead";
        [SerializeField] private string winAnim = "win";

        [Header("Attack Combo Animation Names")]
        [SerializeField] private string[] attackAnims =
        {
            "attack1",
            "attack2",
            "attack3"
        };

        [Header("Skill Animation Names")]
        [SerializeField] private string ultimateAnim = "ultimate";
        [SerializeField] private string passiveAnim = "passive";

        [Header("Fallback Duration")]
        [SerializeField] private float defaultSpawnDuration = 1f;
        [SerializeField] private float defaultAttackDuration = 0.8f;
        [SerializeField] private float defaultUltimateDuration = 1.2f;
        [SerializeField] private float defaultPassiveDuration = 1f;
        [SerializeField] private float defaultDeadDuration = 1f;
        [SerializeField] private float defaultWinDuration = 1f;

        [Header("Mix")]
        [SerializeField] private float defaultMix = 0.08f;

        private string currentAnim;

        public event Action<string> SpineEventTriggered;
        public event Action<string> AnimationCompleted;

        private void Awake()
        {
            if (skeletonAnimation == null)
                skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();

            if (skeletonAnimation == null)
                return;

            skeletonAnimation.AnimationState.Data.DefaultMix = defaultMix;

            skeletonAnimation.AnimationState.Event += OnSpineEvent;
            skeletonAnimation.AnimationState.Complete += OnSpineComplete;
        }

        private void OnDestroy()
        {
            if (skeletonAnimation == null)
                return;

            skeletonAnimation.AnimationState.Event -= OnSpineEvent;
            skeletonAnimation.AnimationState.Complete -= OnSpineComplete;
        }

        private void OnSpineEvent(TrackEntry trackEntry, Spine.Event e)
        {
            if (e == null || e.Data == null)
                return;

            SpineEventTriggered?.Invoke(e.Data.Name);
        }

        private void OnSpineComplete(TrackEntry trackEntry)
        {
            if (trackEntry == null || trackEntry.Animation == null)
                return;

            AnimationCompleted?.Invoke(trackEntry.Animation.Name);
        }

        public void FaceDirection(Vector3 direction)
        {
            if (skeletonAnimation == null)
                return;

            direction.y = 0f;

            if (direction.x > 0.01f)
                skeletonAnimation.Skeleton.ScaleX = 1f;
            else if (direction.x < -0.01f)
                skeletonAnimation.Skeleton.ScaleX = -1f;
        }

        public void FaceTarget(Vector3 selfPosition, Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - selfPosition;
            FaceDirection(direction);
        }

        public void PlayIdle()
        {
            Play(idleAnim, true);
        }

        public void PlayRun()
        {
            Play(runAnim, true);
        }

        public float PlayAttack(int comboIndex)
        {
            string animName = GetAttackAnimationName(comboIndex);

            if (string.IsNullOrEmpty(animName))
                return defaultAttackDuration;

            return PlayTimed(
                animName,
                false,
                defaultAttackDuration,
                forceRestart: true
            );
        }

        public float PlaySpawn()
        {
            return PlayTimed(spawnAnim, false, defaultSpawnDuration, forceRestart: true);
        }

        public float PlayUltimate()
        {
            return PlayTimed(ultimateAnim, false, defaultUltimateDuration, forceRestart: true);
        }

        public float PlayPassive()
        {
            return PlayTimed(passiveAnim, false, defaultPassiveDuration, forceRestart: true);
        }

        public float PlaySkill(string animName, float fallbackDuration = 1f)
        {
            return PlayTimed(animName, false, fallbackDuration, forceRestart: true);
        }

        public float PlayDead()
        {
            return PlayTimed(deadAnim, false, defaultDeadDuration, forceRestart: true);
        }

        public float PlayWin()
        {
            return PlayTimed(winAnim, false, defaultWinDuration, forceRestart: true);
        }
        
        public void PauseHeroSpine()
        {
            if (skeletonAnimation == null)
                return;

            skeletonAnimation.AnimationState.TimeScale = 0f;
        }
        
        public void ResumeHeroSpine()
        {
            if (skeletonAnimation == null)
                return;

            skeletonAnimation.AnimationState.TimeScale = 1f;
        }

        public string GetAttackAnimationName(int comboIndex)
        {
            if (attackAnims == null || attackAnims.Length == 0)
                return string.Empty;

            int index = Mathf.Abs(comboIndex) % attackAnims.Length;
            return attackAnims[index];
        }

        private void Play(string animName, bool loop)
        {
            if (skeletonAnimation == null)
                return;

            if (string.IsNullOrEmpty(animName))
                return;

            if (currentAnim == animName)
                return;

            skeletonAnimation.AnimationState.SetAnimation(0, animName, loop);
            currentAnim = animName;
        }

        private float PlayTimed(
            string animName,
            bool loop,
            float fallbackDuration,
            bool forceRestart = false
        )
        {
            if (skeletonAnimation == null)
                return fallbackDuration;

            if (string.IsNullOrEmpty(animName))
                return fallbackDuration;

            Spine.Animation animation = skeletonAnimation.Skeleton.Data.FindAnimation(animName);

            if (animation == null)
            {
                Debug.LogWarning($"[{name}] Animation not found: {animName}", this);
                return fallbackDuration;
            }

            if (forceRestart || currentAnim != animName)
            {
                skeletonAnimation.AnimationState.SetAnimation(0, animName, loop);
                currentAnim = animName;
            }

            return Mathf.Max(0.01f, animation.Duration);
        }
    }
}