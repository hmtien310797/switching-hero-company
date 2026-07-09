using System.Collections;
using Immortal_Switch.Scripts.Pooling;
using Immortal_Switch.Scripts.Skill;
using Immortal_Switch.Scripts.Sound;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Immortal_Switch.Scripts.SkillRemake
{
    public class BulletSpawnerSkillRuntimeObject : SkillRuntimeObject
    {
        private BulletPatternConfig currentPattern;
        public BulletPatternConfig debugPattern;
        public bool EnableDebug;

        [Header("Debug")] [SerializeField] private bool fireBySpace = true;

        private bool isFiring;
        private Coroutine patternCoroutine;

        private void Awake()
        {
            if (EnableDebug)
            {
                currentPattern = debugPattern;
            }
        }

        protected override void OnRuntimeInitialized(object arg)
        {
            base.OnRuntimeInitialized(arg);

            if (Context.SkillData.OwnerType == SkillOwnerType.ClassSkill)
            {
                currentPattern = Context.SkillData.BasePhases[0].Actions[0].Projectile.BulletPatternConfig;
            }
            else
            {
                currentPattern = Context.SkillData.Levels[Context.SkillLevel - 1].Phases[0].Actions[0].Projectile
                    .BulletPatternConfig;
            }

            Vector3 direction = GetDirectionToTarget(Context.Caster.transform, Context.MainTarget.Transform);
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
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
            if (EnableDebug)
            {
                currentPattern = debugPattern;
            }

            StartCoroutine(ExecutePatternRoutine());
        }

        private IEnumerator ExecutePatternRoutine()
        {
            yield return new WaitForSeconds(currentPattern.delayWhenStartFiring);
            int totalWaves = Mathf.Max(1, currentPattern.totalWaves);

            for (int waveIndex = 0; waveIndex < totalWaves; waveIndex++)
            {
                if (currentPattern.bulletSpawnDelay > 0f)
                {
                    yield return SpawnCurrentWaveRoutine(waveIndex);
                }
                else
                {
                    SoundManager.Instance.PlaySfx(Config.soundDefinition.hitSound);
                    SpawnCurrentWaveInstant(waveIndex);
                }

                // Không cần chờ timeBetweenWaves sau wave cuối cùng.
                bool hasNextWave = waveIndex < totalWaves - 1;

                if (hasNextWave &&
                    currentPattern.timeBetweenWaves > 0f)
                {
                    yield return new WaitForSeconds(
                        currentPattern.timeBetweenWaves
                    );
                }
            }

            patternCoroutine = null;

            if (!EnableDebug)
            {
                ForceDespawn();
            }
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

        private void SpawnBulletByPattern(
            int waveIndex,
            int bulletIndex,
            int bulletCount)
        {

            if (string.IsNullOrWhiteSpace(
                    currentPattern.bulletAddressableKey))
            {
                Debug.LogError(
                    $"[{nameof(BulletSpawnerSkillRuntimeObject)}] " +
                    $"Bullet Addressable key is null or empty.",
                    this
                );

                return;
            }

            AddressablePoolService poolService =
                AddressablePoolService.Instance;

            if (poolService == null)
            {
                Debug.LogError(
                    $"[{nameof(BulletSpawnerSkillRuntimeObject)}] " +
                    $"{nameof(AddressablePoolService)}.Instance is null.",
                    this
                );

                return;
            }

            float angle =
                GetBulletAngle(
                    waveIndex,
                    bulletIndex,
                    bulletCount
                );

            Vector3 direction =
                AngleToDirectionXZ(angle);

            Quaternion rotation =
                Quaternion.identity;

            if (direction.sqrMagnitude > 0.0001f)
            {
                rotation =
                    Quaternion.LookRotation(
                        direction,
                        Vector3.up
                    );
            }

            BulletProjectile bullet =
                poolService.Spawn<BulletProjectile>(
                    currentPattern.bulletAddressableKey,
                    transform.position,
                    rotation
                );

            if (bullet == null)
                return;

            if (EnableDebug)
            {
                bullet.Setup(
                    null,
                    Config,
                    direction,
                    currentPattern.bulletSpeed,
                    currentPattern.bulletLifeTime,
                    currentPattern.damage
                );

                return;
            }

            if (Context == null ||
                Context.Caster == null)
            {
                AddressableProjectilePoolable poolable =
                    bullet.GetComponent<
                        AddressableProjectilePoolable>();

                poolable?.Despawn();
                return;
            }

            bullet.Setup(
                Context.Caster, Config,
                direction,
                currentPattern.bulletSpeed,
                currentPattern.bulletLifeTime,
                currentPattern.damage
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