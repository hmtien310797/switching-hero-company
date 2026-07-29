using System;
using System.Threading;
using Battle;
using Immortal_Switch.Scripts.Pooling;
using Immortal_Switch.Scripts.StatSystem;
using Spine.Unity;
using UnityEngine;

namespace Immortal_Switch.Scripts.Boss
{
    /// <summary>
    /// Đạn parabol cho boss Sea Monster.
    /// Bay theo đường cong quadratic bezier từ vị trí boss đến mục tiêu.
    /// Khi tới đích: tự despawn về pool và gọi callback để xử lý damage + impact.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AddressableProjectilePoolable))]
    public class SeaMonsterBossBullet : MonoBehaviour, IAddressableProjectile
    {
        [Header("Spine")]
        [SerializeField] private SkeletonAnimation skeletonAnimation;
        [SerializeField] private string flyAnimationName = "animation";

        [Header("Flight")]
        [SerializeField]
        [Min(0.1f)]
        private float arcHeight = 3f;

        [SerializeField]
        [Min(0.1f)]
        private float flightDuration = 0.5f;

        private AddressableProjectilePoolable poolable;
        private ICombatUnit source;
        private ICombatUnit target;
        private Action<SeaMonsterBossBullet, ICombatUnit> onReachedTarget;

        private Vector3 startPosition;
        private Vector3 controlPoint;
        private Vector3 targetPosition;
        private float elapsed;
        private bool isFlying;
        private bool hasReachedTarget;
        private bool isDespawned;

        private CancellationTokenRegistration endStageCancelRegistration;

        private void Awake()
        {
            poolable = GetComponent<AddressableProjectilePoolable>();

            if (skeletonAnimation == null)
                skeletonAnimation = GetComponent<SkeletonAnimation>();
        }

        /// <summary>
        /// Khởi động đường bay parabol về phía mục tiêu.
        /// </summary>
        public void Launch(
            ICombatUnit sourceUnit,
            ICombatUnit targetUnit,
            Action<SeaMonsterBossBullet, ICombatUnit> onReachedTargetCallback)
        {
            source = sourceUnit;
            target = targetUnit;
            onReachedTarget = onReachedTargetCallback;

            startPosition = transform.position;
            targetPosition = target != null ? target.Position : startPosition;

            // Quadratic bezier control point: midpoint + arc height
            Vector3 midPoint = (startPosition + targetPosition) * 0.5f;
            controlPoint = midPoint + Vector3.up * arcHeight;

            elapsed = 0f;
            isFlying = true;
            hasReachedTarget = false;

            PlayFlyAnimation();
            RegisterEndStageCleanup();
        }

        private void PlayFlyAnimation()
        {
            if (skeletonAnimation == null)
                return;

            if (string.IsNullOrEmpty(flyAnimationName))
                return;

            skeletonAnimation.AnimationState.SetAnimation(0, flyAnimationName, true);
        }

        private void Update()
        {
            if (!isFlying)
                return;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flightDuration);

            // Quadratic bezier: B(t) = (1-t)²P0 + 2(1-t)tP1 + t²P2
            float u = 1f - t;
            transform.position =
                u * u * startPosition +
                2f * u * t * controlPoint +
                t * t * targetPosition;

            if (t >= 1f && !hasReachedTarget)
            {
                hasReachedTarget = true;
                isFlying = false;
                OnReachedTarget();
            }
        }

        private void OnReachedTarget()
        {
            ICombatUnit currentTarget = target;

            if (currentTarget != null && currentTarget.IsUnityAlive() && !currentTarget.IsDead)
            {
                onReachedTarget?.Invoke(this, currentTarget);
            }

            DespawnSelf();
        }

        public void DespawnSelf()
        {
            // Idempotent: cả DespawnAllActiveBullets (từ skill logic) lẫn
            // endStageCancelRegistration của từng bullet đều gọi hàm này khi
            // endStageSession CTS bị cancel. Pool handle đã được trả về pool ở
            // lần despawn đầu tiên (OnDespawned reset isDespawning & gán
            // PoolHandle = null), nên lần thứ hai phải chặn sớm để tránh
            // "Missing AddressablePoolHandle".
            if (isDespawned)
                return;

            isDespawned = true;
            isFlying = false;
            hasReachedTarget = true;
            ReturnToPool();
        }

        private void ReturnToPool()
        {
            endStageCancelRegistration.Dispose();

            CleanupSpineAnimation();

            if (poolable == null)
            {
                poolable = GetComponent<AddressableProjectilePoolable>();
            }

            if (poolable == null)
            {
                gameObject.SetActive(false);
                return;
            }

            poolable.Despawn();
        }

        private void CleanupSpineAnimation()
        {
            if (skeletonAnimation == null)
                return;

            skeletonAnimation.AnimationState.ClearTracks();
            skeletonAnimation.Skeleton.SetToSetupPose();
        }

        public void OnProjectileSpawnedFromPool()
        {
            isFlying = false;
            hasReachedTarget = false;
            isDespawned = false;
            elapsed = 0f;
            source = null;
            target = null;
            onReachedTarget = null;

            CleanupSpineAnimation();
        }

        public void OnProjectileDespawnedToPool()
        {
            isFlying = false;
            hasReachedTarget = false;
            elapsed = 0f;
            source = null;
            target = null;
            onReachedTarget = null;
        }

        private void RegisterEndStageCleanup()
        {
            BattleFlowController flowController = BattleFlowController.Instance;

            if (flowController == null)
                return;

            CancellationTokenSource cts =
                flowController.endStageSessionCancellationTokenSource;

            if (cts == null)
                return;

            endStageCancelRegistration = cts.Token.Register(
                static state =>
                {
                    var self = state as SeaMonsterBossBullet;
                    if (self == null || !self.IsUnityAlive())
                        return;

                    self.DespawnSelf();
                },
                this);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!isFlying)
                return;

            Gizmos.color = Color.cyan;
            Vector3 prev = startPosition;
            int segments = 20;

            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                float u = 1f - t;
                Vector3 point =
                    u * u * startPosition +
                    2f * u * t * controlPoint +
                    t * t * targetPosition;

                Gizmos.DrawLine(prev, point);
                prev = point;
            }
        }
#endif
    }
}
