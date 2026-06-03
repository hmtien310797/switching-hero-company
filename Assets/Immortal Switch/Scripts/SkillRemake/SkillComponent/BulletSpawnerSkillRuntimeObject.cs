using System.Collections;
using Common;
using Immortal_Switch.Scripts.Skill;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Immortal_Switch.Scripts.SkillRemake
{
    public class BulletSpawnerSkillRuntimeObject: SkillRuntimeObject
    {
        private BulletPatternConfig currentPattern;
        private float damage;
        
        [Header("Debug")] [SerializeField] private bool fireBySpace = true;

        private bool isFiring;

        protected override void OnRuntimeInitialized()
        {
            base.OnRuntimeInitialized();
            currentPattern = Context.SkillData.Levels[Context.SkillLevel - 1].BulletSpawnerConfig;
            Vector3 direction = GetDirectionToTarget(Context.Caster.transform, Context.MainTarget.Transform);
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            damage = currentPattern.damage;
            TryFire();
        }
        
        private Vector3 GetDirectionToTarget(Transform from, Transform target)
        {
            Vector3 direction = target.position - from.position;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.0001f)
                return from.forward;

            return direction.normalized;
        }

        [Button]
        private void TryFire()
        {
            StartCoroutine(ExecutePatternRoutine());
        }

        private IEnumerator ExecutePatternRoutine()
        {
            int totalWaves = Mathf.Max(1, currentPattern.totalWaves);

            for (int waveIndex = 0; waveIndex < totalWaves; waveIndex++)
            {
                if (currentPattern.bulletSpawnDelay > 0f)
                {
                    yield return SpawnCurrentWaveRoutine(waveIndex);
                }
                else
                {
                    SpawnCurrentWaveInstant(waveIndex);
                }

                if (currentPattern.timeBetweenWaves > 0f)
                {
                    yield return new WaitForSeconds(currentPattern.timeBetweenWaves);
                }
            }
            
            DespawnSelf();
        }

        private void SpawnCurrentWaveInstant(int waveIndex)
        {
            int count = Mathf.Max(1, currentPattern.bulletsPerWave);

            for (int i = 0; i < count; i++)
            {
                SpawnBulletByPattern(waveIndex, i, count);
            }
        }

        private IEnumerator SpawnCurrentWaveRoutine(int waveIndex)
        {
            int count = Mathf.Max(1, currentPattern.bulletsPerWave);

            for (int i = 0; i < count; i++)
            {
                SpawnBulletByPattern(waveIndex, i, count);
                yield return new WaitForSeconds(currentPattern.bulletSpawnDelay);
            }
        }

        private void SpawnBulletByPattern(int waveIndex, int bulletIndex, int bulletCount)
        {
            float angle = GetBulletAngle(waveIndex, bulletIndex, bulletCount);
            Vector3 direction = AngleToDirectionXZ(angle);

            Quaternion rotation = Quaternion.identity;
            if (direction.sqrMagnitude > 0.0001f)
            {
                rotation = Quaternion.LookRotation(direction, Vector3.up);
            }

            BulletProjectile bullet = PoolManager.Instance.Spawn(
                currentPattern.bulletPrefab,
                this.transform.position,
                rotation
            );

            if (bullet == null)
                return;

            bullet.Setup(
                Context.Caster,
                direction,
                currentPattern.bulletSpeed,
                currentPattern.bulletLifeTime, damage
            );
        }

        private float GetBulletAngle(int waveIndex, int bulletIndex, int bulletCount)
        {
            float centerAngle = GetFirePointCenterAngleXZ();

            int orderedBulletIndex = GetOrderedBulletIndex(waveIndex, bulletIndex, bulletCount);

            switch (currentPattern.patternType)
            {
                case PatternType.Single:
                    return centerAngle;

                case PatternType.Spread:
                    return GetSpreadAngle(centerAngle, orderedBulletIndex, bulletCount);

                case PatternType.Ring:
                    return GetRingAngle(centerAngle, orderedBulletIndex, bulletCount);

                case PatternType.Spiral:
                    return GetSpiralAngle(centerAngle, waveIndex, orderedBulletIndex, bulletCount);

                default:
                    return centerAngle;
            }
        }

        private int GetOrderedBulletIndex(int waveIndex, int bulletIndex, int bulletCount)
        {
            if (!currentPattern.alternateBulletOrderEachWave)
                return bulletIndex;

            bool isReverseWave = waveIndex % 2 == 1;

            if (!isReverseWave)
                return bulletIndex;

            return bulletCount - 1 - bulletIndex;
        }

        private float GetFirePointCenterAngleXZ()
        {
            Vector3 forward = transform.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude <= 0.0001f)
                return 0f;

            forward.Normalize();

            return Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        }

        private float GetSpreadAngle(float centerAngle, int bulletIndex, int bulletCount)
        {
            if (bulletCount <= 1)
                return centerAngle;

            float totalAngle = currentPattern.spreadAngle;
            float startAngle = centerAngle - totalAngle * 0.5f;
            float angleStep = totalAngle / (bulletCount - 1);

            return startAngle + angleStep * bulletIndex;
        }

        private float GetRingAngle(float centerAngle, int bulletIndex, int bulletCount)
        {
            float angleStep = 360f / bulletCount;
            return centerAngle + angleStep * bulletIndex;
        }

        private float GetSpiralAngle(float centerAngle, int waveIndex, int bulletIndex, int bulletCount)
        {
            float spiralOffset = waveIndex * currentPattern.rotationSpeedPerWave;

            if (bulletCount <= 1)
            {
                return centerAngle + spiralOffset;
            }

            return GetSpreadAngle(centerAngle + spiralOffset, bulletIndex, bulletCount);
        }

        private Vector3 AngleToDirectionXZ(float angle)
        {
            float rad = angle * Mathf.Deg2Rad;

            return new Vector3(
                Mathf.Sin(rad),
                0f,
                Mathf.Cos(rad)
            );
        }
    }
}