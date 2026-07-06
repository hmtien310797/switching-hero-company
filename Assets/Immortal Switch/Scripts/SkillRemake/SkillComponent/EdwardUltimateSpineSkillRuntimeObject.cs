using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Immortal_Switch.Scripts.Combat;
using Immortal_Switch.Scripts.Skill;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

namespace Immortal_Switch.Scripts.SkillRemake.SkillComponent
{
    public class EdwardUltimateSpineSkillRuntimeObject : SpineSkillRuntimeObject
    {
        [SerializeField, Min(1)] private int zigzagCount = 6;
        [SerializeField, Min(0f)] private float zigzagWidth = 1.5f;
        [SerializeField, Min(0.01f)] private float moveDuration = 1.5f;
        [Tooltip("Khoảng cách tiến về phía trước của mỗi đoạn zigzag.")]
        [SerializeField, Min(0.01f)]
        private float forwardDistancePerZigzag = 2f;
        [Tooltip("Tốc độ di chuyển của skill object.")]
        [SerializeField, Min(0.01f)]
        private float moveSpeed = 8f;

        private Tween moveTween;
        private Vector3[] pathPoints;
        private float damage;
        private string animationName;
        private bool loop;
        private CancellationTokenSource cts;

        protected override void OnRuntimeInitialized(object arg)
        {
            base.OnRuntimeInitialized(arg);
            if (Context.SkillData.OwnerType == SkillOwnerType.ClassSkill)
            {
                damage = Context.SkillData.BasePhases[0].Actions[0].Damage.SkillDamageBonusPercent;
            }
            else
            {
                damage = Context.SkillData.Levels[Context.SkillLevel - 1].Phases[0].Actions[0].Damage.SkillDamageBonusPercent;
            }

            animationName = Config != null && !string.IsNullOrEmpty(Config.AnimationName)
                ? Config.AnimationName
                : fallbackAnimationName;
            
            loop = Config != null ? Config.LoopAnimation : fallbackLoop;

            CreateEmitRuntimeCancellationToken();
            Vector3 direction = GetDirectionToTarget(Context.Caster.transform, Context.MainTarget.Transform);
            
            skeletonAnimation.Initialize(true);
            skeletonAnimation.AnimationState.ClearTracks();
            skeletonAnimation.Skeleton.SetToSetupPose();
            skeletonAnimation.gameObject.SetActive(false);
            
            Play(direction).Forget();
        }

        protected override void OnDespawnedToPool()
        {
            StopMovement();
            CancelEmitRuntimeEvent();
            skeletonAnimation.gameObject.SetActive(false);
        }

        public async UniTask Play(Vector3 direction, Action onCompleted = null)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(1f));
            skeletonAnimation.gameObject.SetActive(true);
            if (!string.IsNullOrEmpty(animationName))
                skeletonAnimation.AnimationState.SetAnimation(0, animationName, loop);
            
            StopMovement();
            
            BuildZigZagPath(
                Context.Caster.Position,
                direction);

            float totalDistance = CalculatePathDistance(Context.Caster.Position);

            float duration = totalDistance / moveSpeed;
            ExecuteSkillAction(cts.Token).Forget();
            moveTween = transform
                .DOPath(
                    pathPoints,
                    duration,
                    PathType.Linear,
                    PathMode.Full3D)
                .SetEase(Ease.Linear)
                .SetSpeedBased(false)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(() =>
                {
                    moveTween = null;
                    onCompleted?.Invoke();
                    ForceDespawn();
                });
        }

        private async UniTask ExecuteSkillAction(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    ExecuteAction();

                    await UniTask.Delay(
                        500,
                        cancellationToken: cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Bình thường khi token bị cancel.
            }
        }
        
        private void ExecuteAction()
        {
            EmitRuntimeEvent("hit");
        }
        

        private void BuildZigZagPath(
            Vector3 startPoint,
            Vector3 forwardDirection)
        {
            int actualZigzagCount = Mathf.Max(1, zigzagCount);

            pathPoints = new Vector3[actualZigzagCount];

            // Vector vuông góc với hướng forward trên mặt phẳng XZ.
            Vector3 sideDirection = new Vector3(
                -forwardDirection.z,
                0f,
                forwardDirection.x);

            for (int i = 0; i < actualZigzagCount; i++)
            {
                float forwardDistance =
                    forwardDistancePerZigzag * (i + 1);

                float sideSign =
                    i % 2 == 0
                        ? 1f
                        : -1f;

                pathPoints[i] =
                    startPoint +
                    forwardDirection * forwardDistance +
                    sideDirection * zigzagWidth * sideSign;
            }
        }

        private float CalculatePathDistance(Vector3 startPoint)
        {
            if (pathPoints == null || pathPoints.Length == 0)
                return 0f;

            float totalDistance = 0f;
            Vector3 previousPoint = startPoint;

            for (int i = 0; i < pathPoints.Length; i++)
            {
                totalDistance += Vector3.Distance(
                    previousPoint,
                    pathPoints[i]);

                previousPoint = pathPoints[i];
            }

            return totalDistance;
        }
        
        private Vector3 GetDirectionToTarget(Transform from, Transform target)
        {
            Vector3 direction = target.position - from.position;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.0001f)
                return from.forward;

            return direction.normalized;
        }

        public void StopMovement()
        {
            if (moveTween == null)
                return;

            moveTween.Kill();
            moveTween = null;
        }

        private void OnDisable()
        {
            StopMovement();
        }
        
        private void CreateEmitRuntimeCancellationToken()
        {
            CancelEmitRuntimeEvent();
            cts = CancellationTokenSource.CreateLinkedTokenSource(
                this.GetCancellationTokenOnDestroy()
            );
        }

        private void CancelEmitRuntimeEvent()
        {
            if (cts == null)
                return;

            cts.Cancel();
            cts.Dispose();
            cts = null;
        }
    }
}