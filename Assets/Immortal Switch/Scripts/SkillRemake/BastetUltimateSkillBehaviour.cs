using System.Collections;
using System.Collections.Generic;
using Battle;
using DG.Tweening;
using Immortal_Switch.Scripts.Boss;
using Immortal_Switch.Scripts.Enemy;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

namespace Immortal_Switch.Scripts.Skill
{
    /// <summary>
    /// Custom behaviour for Bastet Ultimate.
    /// Rule:
    /// - Uses animation on Bastet herself.
    /// - Before each jump, find one random alive enemy once.
    /// - Tween Bastet toward that cached target position while the ultimate animation plays.
    /// - Spine event "hit" executes the configured skill phase on the cached target.
    /// - If no enemy is found before a jump, the ultimate ends.
    /// - Bastet does not return to the original position; she ends at the last landing position.
    /// </summary>
    public sealed class BastetUltimateSkillBehaviour : SkillBehaviour
    {
        [Header("Bastet Ultimate")]
        [SerializeField, Min(1)] private int maxJumpCount = 4;
        [SerializeField] private string animationName = "ultimate";
        [SerializeField] private string hitEventName = "hit";

        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveDuration = 0.35f;
        [SerializeField] private Ease moveEase = Ease.OutQuad;
        [SerializeField] private Vector3 landingOffset;
        [SerializeField] private bool keepCasterY = true;
        [SerializeField] private bool faceTargetBeforeJump = true;

        [Header("Timing")]
        [Tooltip("If true, the next jump starts only after the current ultimate animation completes.")]
        [SerializeField] private bool waitAnimationCompleteBeforeNextJump = true;

        [Tooltip("Fallback wait if animation complete does not fire for some reason.")]
        [SerializeField, Min(0.01f)] private float fallbackJumpDuration = 1f;

        [Header("Debug")]
        [SerializeField] private bool debugLog;

        private readonly List<EnemyActor> candidates = new();
        private Coroutine routine;
        private Tween moveTween;
        private ICombatUnit currentTarget;
        private bool waitingForHit;
        private bool hitReceived;
        private bool animationCompleted;
        private bool cancelled;

        public override void Cast()
        {
            cancelled = false;
            Context.Caster.CastingUltimate(true);
            routine = StartCoroutine(CastRoutine());
        }

        public override void Cancel()
        {
            cancelled = true;
            currentTarget = null;
            waitingForHit = false;
            hitReceived = false;
            animationCompleted = false;

            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }

            if (moveTween != null && moveTween.IsActive())
            {
                moveTween.Kill(false);
                moveTween = null;
            }
            
            Context.Caster.CastingUltimate(false);
        }

        public override void OnSpineEvent(string eventName)
        {
            if (cancelled || !waitingForHit)
                return;

            if (!string.Equals(eventName, hitEventName, System.StringComparison.Ordinal))
                return;

            hitReceived = true;
            waitingForHit = false;

            if (currentTarget == null || currentTarget.IsDead)
            {
                if (debugLog)
                    Debug.Log("[BastetUltimate] Hit skipped because cached target is dead/null.", this);

                return;
            }

            SkillRuntimeContext hitContext = Context.CloneForTarget(currentTarget);
            hitContext.CastPosition = currentTarget.Position;
            hitContext.TargetPosition = currentTarget.Position;

            Context.SkillController.ExecuteCustomBehaviourEvent(
                this,
                hitContext,
                SkillPhaseTriggerType.SpineEvent,
                hitEventName
            );
        }

        public override void OnAnimationComplete(string completedAnimationName)
        {
            if (cancelled)
                return;

            if (string.IsNullOrEmpty(animationName) ||
                string.Equals(completedAnimationName, animationName, System.StringComparison.Ordinal))
            {
                animationCompleted = true;
            }
        }

        private IEnumerator CastRoutine()
        {
            if (Context == null || Context.Caster == null || Context.SkillController == null)
                yield break;

            Context.Caster.SetActionLocked(true);

            for (int jumpIndex = 0; jumpIndex < maxJumpCount; jumpIndex++)
            {
                if (cancelled || Context.Caster == null || Context.Caster.IsDead)
                    break;

                currentTarget = PvEBattleController.Instance.GetRandomEnemyAlive();
                if (currentTarget == null)
                {
                    Debug.Log($"[BastetUltimate] Stop at jump {jumpIndex + 1}. No alive enemy found.", this);
                    break;
                }

                Context.Caster.SetTarget(currentTarget);

                Vector3 landingPosition = GetLandingPosition(currentTarget);

                if (faceTargetBeforeJump)
                    Context.Caster.Anim?.FaceTarget(Context.Caster.Position, landingPosition);

                hitReceived = false;
                waitingForHit = true;
                animationCompleted = false;

                float animationDuration = PlayUltimateAnimation();
                yield return new WaitForSeconds(0.3f);
                yield return MoveTweenRoutine(landingPosition);

                if (waitAnimationCompleteBeforeNextJump)
                    yield return WaitForAnimationOrFallback(Mathf.Max(animationDuration, fallbackJumpDuration));
                else
                    yield return WaitForHitOrFallback(Mathf.Max(animationDuration, fallbackJumpDuration));

                waitingForHit = false;
                GameCameraController.Instance.ShakeCamera();
            }
            
            routine = null;
            Context.SkillController.FinishCustomBehaviour(this, true);
        }

        private float PlayUltimateAnimation()
        {
            if (Context.Caster == null || Context.Caster.Anim == null)
                return fallbackJumpDuration;

            if (!string.IsNullOrEmpty(animationName))
                return Context.Caster.Anim.PlaySkill(animationName, fallbackJumpDuration);

            return Context.Caster.Anim.PlayUltimate();
        }

        private IEnumerator MoveTweenRoutine(Vector3 landingPosition)
        {
            if (Context.Caster == null)

                yield break;

            if (moveTween != null && moveTween.IsActive())

                moveTween.Kill(false);

            Transform casterTransform = Context.Caster.transform;

            bool completed = false;

            moveTween = casterTransform

                .DOMove(landingPosition, moveDuration)

                .SetEase(moveEase)

                .OnComplete(() => completed = true);

            while (!completed && moveTween != null && moveTween.IsActive())

                yield return null;

        }

        private IEnumerator WaitForAnimationOrFallback(float fallbackDuration)
        {
            float elapsed = 0f;
            while (!cancelled && !animationCompleted && elapsed < fallbackDuration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator WaitForHitOrFallback(float fallbackDuration)
        {
            float elapsed = 0f;
            while (!cancelled && !hitReceived && elapsed < fallbackDuration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private Vector3 GetLandingPosition(ICombatUnit target)
        {
            Vector3 position = target != null ? target.Position : Context.Caster.Position;
            position += landingOffset;

            if (keepCasterY && Context.Caster != null)
                position.y = Context.Caster.Position.y;

            return position;
        }
    }
}
