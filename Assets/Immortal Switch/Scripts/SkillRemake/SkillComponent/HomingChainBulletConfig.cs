using System;
using UnityEngine;

[Serializable]
public class HomingChainBulletConfig
{
    [Header("Prefab")]
    public HomingChainBulletProjectile bulletPrefab;

    [Header("Spawn")]
    public int bulletCount = 1;
    public float delayBetweenBullets = 0.05f;

    [Header("Target")]
    public LayerMask enemyLayer;
    public float searchRadius = 20f;
    public int maxTargetsPerBullet = 4;

    [Header("Movement")]
    public float straightSpeed = 18f;
    public float curveSpeed = 22f;
    public float arriveDistance = 0.2f;
    public float curveTangentLength = 3f;

    [Header("Damage")]
    public float damage = 100f;

    [Header("Debug")]
    public bool drawDebugPath = false;
    
    [Header("Curve")]
    public float curveHeight = 4f;
    public bool alternateCurveSide = true;
    
}