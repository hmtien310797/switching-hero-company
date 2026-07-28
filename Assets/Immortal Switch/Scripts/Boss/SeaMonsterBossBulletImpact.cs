using System.Threading;
using Battle;
using Immortal_Switch.Scripts.Pooling;
using Spine.Unity;
using UnityEngine;

namespace Immortal_Switch.Scripts.Boss
{
    /// <summary>
    /// Hiệu ứng impact khi đạn Sea Monster trúng mục tiêu.
    /// Play Spine animation một lần (loop=false), tự động despawn
    /// khi animation kết thúc hoặc khi stage thay đổi.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AddressableProjectilePoolable))]
    public class SeaMonsterBossBulletImpact : MonoBehaviour, IAddressableProjectile
    {
        [Header("Spine")]
        [SerializeField] private SkeletonAnimation skeletonAnimation;
        [SerializeField] private string impactAnimationName = "animation";

        [Header("Fallback")]
        [SerializeField]
        [Min(0.1f)]
        private float fallbackDuration = 1f;

        private AddressableProjectilePoolable poolable;
        private CancellationTokenRegistration endStageCancelRegistration;
        private bool isPlaying;

        private void Awake()
        {
            poolable = GetComponent<AddressableProjectilePoolable>();

            if (skeletonAnimation == null)
                skeletonAnimation = GetComponent<SkeletonAnimation>();
        }

        /// <summary>
        /// Bắt đầu hiển thị hiệu ứng impact. Play Spine animation với loop=false.
        /// Tự động despawn sau khi animation kết thúc hoặc sau <see cref="fallbackDuration"/> giây.
        /// </summary>
        public void Play()
        {
            if (isPlaying)
                return;

            isPlaying = true;

            if (skeletonAnimation != null && !string.IsNullOrEmpty(impactAnimationName))
            {
                // Đăng ký sự kiện Complete để biết khi nào animation kết thúc
                skeletonAnimation.AnimationState.Complete -= OnImpactAnimationComplete;
                skeletonAnimation.AnimationState.Complete += OnImpactAnimationComplete;

                skeletonAnimation.AnimationState.SetAnimation(0, impactAnimationName, false);
            }
            else
            {
                // Fallback: không có SkeletonAnimation → despawn sau thời gian cố định
                DespawnAfterFallback();
            }

            RegisterEndStageCleanup();
        }

        private void OnImpactAnimationComplete(Spine.TrackEntry trackEntry)
        {
            // Chỉ despawn khi animation trên track 0 hoàn thành
            if (trackEntry == null || trackEntry.TrackIndex != 0)
                return;

            skeletonAnimation.AnimationState.Complete -= OnImpactAnimationComplete;

            DespawnSelf();
        }

        private async void DespawnAfterFallback()
        {
            await System.Threading.Tasks.Task.Delay(
                System.TimeSpan.FromSeconds(fallbackDuration));

            DespawnSelf();
        }

        private void DespawnSelf()
        {
            if (!isPlaying)
                return;

            isPlaying = false;
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

            skeletonAnimation.AnimationState.Complete -= OnImpactAnimationComplete;
            skeletonAnimation.AnimationState.ClearTracks();
            skeletonAnimation.Skeleton.SetToSetupPose();
        }

        public void OnProjectileSpawnedFromPool()
        {
            isPlaying = false;
            CleanupSpineAnimation();
        }

        public void OnProjectileDespawnedToPool()
        {
            isPlaying = false;
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
                    var self = state as SeaMonsterBossBulletImpact;
                    if (self == null || !self.IsUnityAlive())
                        return;

                    self.DespawnSelf();
                },
                this);
        }
    }
}
