using System;
using UnityEngine;

[Serializable]
public struct BulletPatternConfig
{
    [Header("Base Settings")]
    public PatternType patternType;
    [Header("Addressable Bullet")]
    [Tooltip("Addressable key của prefab BulletProjectile.")]
    public string bulletAddressableKey;
    public float bulletSpeed;
    public float bulletLifeTime;

    [Header("Firing Rate")]
    public int totalWaves;
    public float timeBetweenWaves;

    [Header("Shape Settings")]
    public int bulletsPerWave;
    public float spreadAngle;

    [Header("Spiral Settings")]
    public float rotationSpeedPerWave;

    [Header("Delay Per Bullet")]
    public float bulletSpawnDelay;
    
    [Header("Wave Order")]
    public bool alternateBulletOrderEachWave;

    public float damage;
    public float delayWhenStartFiring;
}

public enum PatternType
{
    Single,
    Spread,
    Ring,
    Spiral
}