using System.Collections.Generic;
using System.Threading;
using Battle;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Combat;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Pooling;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

namespace Immortal_Switch.Scripts.Boss
{
    /// <summary>
    /// Skill logic cho boss Sea Monster (ID 5001) — boss đánh xa.
    /// Mỗi đòn đánh thường bắn một viên đạn parabol về phía mục tiêu.
    /// Khi đạn tới đích: gây damage và spawn hiệu ứng impact.
    /// </summary>
    public class BossSeaMonsterSkillLogic : BossSkillLogicBase
    {
        private const string BulletAddressableKey = "sea_monster_bullet";
        private const string ImpactAddressableKey = "sea_monster_bullet_impact";
        private const int PoolSize = 3;

        private readonly List<SeaMonsterBossBullet> activeBullets = new();
        private CancellationTokenRegistration endStageCancelRegistration;
        private bool poolsReady;

        public override void Initialize(BossActor boss)
        {
            base.Initialize(boss);
            InitializeAsync().Forget();
        }

        private async UniTaskVoid InitializeAsync()
        {
            await CreatePoolsAsync();
            RegisterEndStageCleanup();
            poolsReady = true;
        }

        private async UniTask CreatePoolsAsync()
        {
            AddressablePoolService poolService = AddressablePoolService.Instance;

            if (poolService == null)
            {
                Debug.LogError(
                    $"[{nameof(BossSeaMonsterSkillLogic)}] " +
                    "AddressablePoolService.Instance is null.");
                return;
            }

            await poolService.CreatePoolAsync(BulletAddressableKey, PoolSize);
            await poolService.CreatePoolAsync(ImpactAddressableKey, PoolSize);

            Debug.Log(
                $"[{nameof(BossSeaMonsterSkillLogic)}] " +
                $"Pools created: '{BulletAddressableKey}' x{PoolSize}, " +
                $"'{ImpactAddressableKey}' x{PoolSize}.");
        }

        public override void OnNormalAttack()
        {
            if (!poolsReady)
                return;

            ICombatUnit target = boss.Target;

            if (target == null || target.IsDead)
                return;

            SpawnBullet(target);
        }

        private void SpawnBullet(ICombatUnit target)
        {
            AddressablePoolService poolService = AddressablePoolService.Instance;

            if (poolService == null)
                return;

            SeaMonsterBossBullet bullet = poolService.Spawn<SeaMonsterBossBullet>(
                BulletAddressableKey,
                boss.FirePointPosition,
                Quaternion.identity);

            if (bullet == null)
            {
                Debug.LogWarning(
                    $"[{nameof(BossSeaMonsterSkillLogic)}] " +
                    $"Failed to spawn bullet from pool '{BulletAddressableKey}'.");
                return;
            }

            activeBullets.Add(bullet);

            bullet.Launch(
                boss,
                target,
                OnBulletReachedTarget);
        }

        private void OnBulletReachedTarget(
            SeaMonsterBossBullet bullet,
            ICombatUnit target)
        {
            activeBullets.Remove(bullet);

            if (target == null || target.IsDead)
                return;

            // Gây damage
            DamageResult damageResult = DamageCalculator.CalculateDamage(boss, target);
            target.TakeDamage(damageResult);

            // Spawn hiệu ứng impact
            SpawnImpact(target.Position);
        }

        private void SpawnImpact(Vector3 position)
        {
            AddressablePoolService poolService = AddressablePoolService.Instance;

            if (poolService == null)
                return;

            SeaMonsterBossBulletImpact impact = poolService.Spawn<SeaMonsterBossBulletImpact>(
                ImpactAddressableKey,
                position,
                Quaternion.identity);

            if (impact != null)
            {
                impact.Play();
            }
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

            endStageCancelRegistration = cts.Token.Register(DespawnAllActiveBullets);
        }

        private void DespawnAllActiveBullets()
        {
            // Snapshot để tránh modified collection khi bullet tự remove callback
            var snapshot = new List<SeaMonsterBossBullet>(activeBullets);
            activeBullets.Clear();

            for (int i = 0; i < snapshot.Count; i++)
            {
                SeaMonsterBossBullet bullet = snapshot[i];

                if (bullet != null && bullet.IsUnityAlive())
                {
                    bullet.DespawnSelf();
                }
            }

            endStageCancelRegistration.Dispose();
        }
    }
}
