using System.Collections;
using Common;
using UnityEngine;

public class BulletSpawner : MonoBehaviour
{
    [SerializeField] private BulletPatternConfig currentPattern;
    [SerializeField] private Transform firePoint;

    [Header("Debug")]
    [SerializeField] private bool fireBySpace = true;

    private bool isFiring;

    private void Update()
    {
        if (!fireBySpace)
            return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryFire();
        }
    }

    public void TryFire()
    {
        if (isFiring)
            return;

        if (!ValidatePattern())
            return;

        StartCoroutine(ExecutePatternRoutine());
    }

    public void SetPattern(BulletPatternConfig pattern)
    {
        currentPattern = pattern;
    }

    private bool ValidatePattern()
    {
        // if (currentPattern.bulletPrefab == null)
        // {
        //     Debug.LogError("[BulletSpawner] Bullet prefab is null.");
        //     return false;
        // }

        if (firePoint == null)
        {
            Debug.LogError("[BulletSpawner] Fire point is null.");
            return false;
        }

        if (PoolManager.Instance == null)
        {
            Debug.LogError("[BulletSpawner] PoolManager.Instance is null. Please add PoolManager to scene.");
            return false;
        }

        return true;
    }

    private IEnumerator ExecutePatternRoutine()
    {
        isFiring = true;

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

        isFiring = false;
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

        // BulletProjectile bullet = PoolManager.Instance.Spawn(
        //     currentPattern.bulletPrefab,
        //     firePoint.position,
        //     rotation
        // );

        // if (bullet == null)
        //     return;
    }

    private float GetBulletAngle(int waveIndex, int bulletIndex, int bulletCount)
    {
        float centerAngle = GetFirePointCenterAngleXZ();

        int orderedBulletIndex = GetOrderedBulletIndex(waveIndex, bulletIndex, bulletCount);

        // switch (currentPattern.patternType)
        // {
        //     case BulletPatternConfig.PatternType.Single:
        //         return centerAngle;
        //
        //     case BulletPatternConfig.PatternType.Spread:
        //         return GetSpreadAngle(centerAngle, orderedBulletIndex, bulletCount);
        //
        //     case BulletPatternConfig.PatternType.Ring:
        //         return GetRingAngle(centerAngle, orderedBulletIndex, bulletCount);
        //
        //     case BulletPatternConfig.PatternType.Spiral:
        //         return GetSpiralAngle(centerAngle, waveIndex, orderedBulletIndex, bulletCount);
        //
        //     default:
        //         return centerAngle;
        // }
        return 0;
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
        Vector3 forward = firePoint.forward;
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