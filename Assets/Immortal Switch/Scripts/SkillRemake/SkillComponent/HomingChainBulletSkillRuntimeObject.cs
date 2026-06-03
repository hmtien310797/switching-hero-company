using System.Collections;
using Common;
using Immortal_Switch.Scripts.Skill;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Immortal_Switch.Scripts.SkillRemake
{
    public class HomingChainBulletSkillRuntimeObject : SkillRuntimeObject
    {
        [SerializeField]
        [InlineEditor] private HomingChainBulletConfig debugConfig;
        
        private HomingChainBulletConfig config;
        private Coroutine spawnCoroutine;

        protected override void OnRuntimeInitialized()
        {
            base.OnRuntimeInitialized();
            config = debugConfig;

            TryFire();
        }

        [Button]
        private void TryFire()
        {
            if (spawnCoroutine != null)
                StopCoroutine(spawnCoroutine);

            spawnCoroutine = StartCoroutine(SpawnBulletRoutine());
        }

        private IEnumerator SpawnBulletRoutine()
        {
            int count = Mathf.Max(1, config.bulletCount);

            for (int i = 0; i < count; i++)
            {
                SpawnOneBullet(i);

                if (config.delayBetweenBullets > 0f && i < count - 1)
                    yield return new WaitForSeconds(config.delayBetweenBullets);
            }

            spawnCoroutine = null;

            // Runtime object này chỉ cần sống tới khi spawn đủ bullet.
            // Bullet đã tự quản lý chain target và tự despawn.
            // Nếu bạn có hàm Finish/Despawn runtime riêng thì gọi ở đây.
            // DespawnSelf();
        }

        private void SpawnOneBullet(int index)
        {
            Vector3 spawnPosition = transform.position;

            Vector3 initialDirection = transform.forward;
            initialDirection.y = 0f;

            if (initialDirection.sqrMagnitude <= 0.0001f)
                initialDirection = Vector3.forward;

            initialDirection.Normalize();

            Quaternion rotation = Quaternion.LookRotation(initialDirection, Vector3.up);

            HomingChainBulletProjectile bullet = PoolManager.Instance.Spawn(
                config.bulletPrefab,
                spawnPosition,
                rotation
            );

            if (bullet == null)
                return;

            bullet.Setup(
                Context.Caster,
                spawnPosition,
                initialDirection,
                config
            );
        }

        private void OnDisable()
        {
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }
        }
    }
}