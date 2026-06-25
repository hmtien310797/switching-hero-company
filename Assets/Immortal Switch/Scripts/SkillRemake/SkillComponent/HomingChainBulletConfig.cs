using System;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class HomingChainBulletConfig
{
    [Header("Addressable Bullet")]
    [Tooltip("Addressable key của prefab HomingChainBulletProjectile.")]
    public string bulletAddressableKey;

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
    
    [Header("Vertical Arc")]
    public bool useVerticalArc = true;

    [Tooltip("Các lượt chain sẽ luân phiên: vồng lên rồi bay phẳng.")]
    public bool alternateVerticalArc = true;

    [Tooltip("Lượt chain đầu tiên có bay vồng lên hay không.")]
    public bool verticalArcOnFirstChainMove = true;

    public Vector2 verticalArcHeightRange = new Vector2(0.5f, 3f);
    
    [Header("Short Distance Detour")]
    public bool UseShortDistanceDetour = true;
    
    [Min(0f)]
    public float minDetourDistance = 5f;

    public Vector2 virtualPointXRange = new Vector2(8f, 12f);
    public Vector2 virtualPointZRange = new Vector2(5f, 9f);
    public Vector2 virtualPointYRange = new Vector2(2f, 3f);

    [Min(1)]
    public int virtualPointFindAttempts = 12;
    
}