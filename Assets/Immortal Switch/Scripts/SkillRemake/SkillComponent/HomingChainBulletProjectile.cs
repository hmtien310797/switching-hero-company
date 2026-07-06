using System;
using System.Collections.Generic;
using System.Threading;
using Battle;
using DG.Tweening;
using Immortal_Switch.Scripts.Combat;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Pooling;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

public class HomingChainBulletProjectile : MonoBehaviour,
    IAddressableProjectile
{
    private readonly List<ICombatUnit> visitedTargets = new();

    private HomingChainBulletConfig config;
    private ICombatUnit caster;

    private ICombatUnit currentTarget;
    private Tween moveTween;

    private Vector3 lastMoveDirection;
    private int hitTargetCount;
    private bool isInitialized;
    private bool isMovingToVirtualPoint;
    private IHeroBattleContext iHeroBattleContext;

    private AddressableProjectilePoolable addressablePoolable;
    private CancellationTokenRegistration _endStageCancelRegistration;
    private Transform pendingTarget;
    
    private void Awake()
    {
        addressablePoolable =
            GetComponent<AddressableProjectilePoolable>();

        if (addressablePoolable == null)
        {
            Debug.LogError(
                $"[{nameof(HomingChainBulletProjectile)}] " +
                $"Missing {nameof(AddressableProjectilePoolable)}.",
                this
            );
        }
        
        
    }
    

    public void Setup(
        ICombatUnit owner,
        Vector3 spawnPosition,
        Vector3 initialDirection,
        HomingChainBulletConfig bulletConfig, IHeroBattleContext battleContext)
    {
        KillMoveTween();

        caster = owner;
        config = bulletConfig;

        transform.position = spawnPosition;
        iHeroBattleContext = battleContext;

        visitedTargets.Clear();

        currentTarget = null;
        pendingTarget = null;

        hitTargetCount = 0;
        isInitialized = true;
        isMovingToVirtualPoint = false;

        previousPosition = spawnPosition;

        lastMoveDirection =
            initialDirection.sqrMagnitude > 0.0001f
                ? initialDirection.normalized
                : transform.forward;

        StartFirstTarget();
    }

    private void StartFirstTarget()
    {
        currentTarget = iHeroBattleContext.GetRandomEnemyAlive();

        if (currentTarget == null)
        {
            DespawnSelf();
            return;
        }

        Vector3 targetPosition = currentTarget.Transform.position;
        targetPosition.y = transform.position.y;

        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
        {
            lastMoveDirection = direction.normalized;
            FaceDirection(lastMoveDirection);
        }

        float distance = Vector3.Distance(transform.position, targetPosition);
        float duration = config.straightSpeed <= 0f
            ? 0.01f
            : distance / config.straightSpeed;

        KillMoveTween();

        moveTween = transform
            .DOMove(targetPosition, duration)
            .SetEase(Ease.Linear)
            .OnUpdate(UpdateFacingByVelocity)
            .OnComplete(() =>
            {
                OnReachCurrentTarget();
                StartNextTarget();
            });
    }

    private void StartNextTarget()
    {
        if (hitTargetCount >= config.maxTargetsPerBullet)
        {
            DespawnSelf();
            return;
        }

        currentTarget = iHeroBattleContext.GetRandomFromFarthestEnemies(transform.position,
            visitedTargets);

        if (currentTarget == null)
        {
            DespawnSelf();
            return;
        }

        Vector3 start = transform.position;

        Vector3 end = currentTarget.Transform.position;
        end.y = start.y;

        float flatDistanceSqr = GetFlatSqrDistance(start, end);
        float minDistance = config.minDetourDistance;
        float minDistanceSqr = minDistance * minDistance;

        if (config.UseShortDistanceDetour &&
            flatDistanceSqr < minDistanceSqr)
        {
            StartMoveToVirtualPoint();
            return;
        }

        StartMoveToCurrentTarget();
    }

    private void StartMoveToVirtualPoint()
    {
        if (currentTarget == null)
        {
            DespawnSelf();
            return;
        }

        Vector3 start = transform.position;
        Vector3 targetPosition = currentTarget.Transform.position;

        Vector3 virtualPoint = FindVirtualPoint(
            start,
            targetPosition);

        Vector3[] path = BuildCurvePath(
            start,
            virtualPoint);

        float distance = EstimatePathDistance(path);

        float duration = config.curveSpeed <= 0f
            ? 0.01f
            : distance / config.curveSpeed;

        KillMoveTween();

        moveTween = transform
            .DOPath(
                path,
                duration,
                PathType.CatmullRom,
                PathMode.Full3D,
                12)
            .SetEase(Ease.Linear)
            .OnUpdate(UpdateFacingByVelocity)
            .OnComplete(StartMoveToCurrentTarget);
    }

    private Vector3 FindVirtualPoint(
        Vector3 currentPosition,
        Vector3 targetPosition)
    {
        float minDistance =
            Mathf.Max(0.01f, config.minDetourDistance);

        float minDistanceSqr =
            minDistance * minDistance;

        int attempts =
            Mathf.Max(1, config.virtualPointFindAttempts);

        for (int i = 0; i < attempts; i++)
        {
            float xOffset = UnityEngine.Random.Range(
                config.virtualPointXRange.x,
                config.virtualPointXRange.y);

            float zOffset = UnityEngine.Random.Range(
                config.virtualPointZRange.x,
                config.virtualPointZRange.y);

            float yOffset = UnityEngine.Random.Range(
                config.virtualPointYRange.x,
                config.virtualPointYRange.y);

            float xSign =
                UnityEngine.Random.value < 0.5f ? -1f : 1f;

            float zSign =
                UnityEngine.Random.value < 0.5f ? -1f : 1f;

            Vector3 candidate =
                currentPosition +
                new Vector3(
                    xOffset * xSign,
                    yOffset,
                    zOffset * zSign);

            if (GetFlatSqrDistance(
                    candidate,
                    targetPosition) >= minDistanceSqr)
            {
                return candidate;
            }
        }

        Vector3 fallbackDirection =
            currentPosition - targetPosition;

        fallbackDirection.y = 0f;

        if (fallbackDirection.sqrMagnitude <= 0.0001f)
        {
            fallbackDirection =
                Quaternion.Euler(
                    0f,
                    UnityEngine.Random.Range(0f, 360f),
                    0f) *
                Vector3.forward;
        }

        fallbackDirection.Normalize();

        float fallbackDistance =
            minDistance +
            Mathf.Max(
                config.virtualPointXRange.y,
                config.virtualPointZRange.y);

        Vector3 fallbackPoint =
            targetPosition +
            fallbackDirection * fallbackDistance;

        fallbackPoint.y =
            currentPosition.y +
            UnityEngine.Random.Range(
                config.virtualPointYRange.x,
                config.virtualPointYRange.y);

        return fallbackPoint;
    }

    private void StartMoveToCurrentTarget()
    {
        if (currentTarget == null)
        {
            DespawnSelf();
            return;
        }

        Vector3 start = transform.position;

        // Lấy lại vị trí mới nhất của target sau khi đi qua virtual point.
        Vector3 end = currentTarget.Transform.position;
        end.y = start.y;

        Vector3[] path = BuildCurvePath(start, end);

        float distance = EstimatePathDistance(path);

        float duration = config.curveSpeed <= 0f
            ? 0.01f
            : distance / config.curveSpeed;

        KillMoveTween();

        moveTween = transform
            .DOPath(
                path,
                duration,
                PathType.CatmullRom,
                PathMode.Full3D,
                12)
            .SetEase(Ease.Linear)
            .OnUpdate(UpdateFacingByVelocity)
            .OnComplete(() =>
            {
                OnReachCurrentTarget();
                StartNextTarget();
            });
    }
    
    private void DespawnSelf()
    {
        KillMoveTween();

        if (addressablePoolable == null)
        {
            addressablePoolable =
                GetComponent<AddressableProjectilePoolable>();
        }

        if (addressablePoolable == null)
        {
            Debug.LogError(
                $"[{nameof(HomingChainBulletProjectile)}] " +
                $"Cannot return bullet to pool because " +
                $"{nameof(AddressableProjectilePoolable)} is missing.",
                this
            );

            gameObject.SetActive(false);
            return;
        }

        addressablePoolable.Despawn();
    }

    private Vector3[] BuildCurvePath(
        Vector3 start,
        Vector3 end)
    {
        // Target vẫn nằm ở cùng mặt phẳng gốc với bullet.
        end.y = start.y;

        Vector3 toEnd = end - start;
        toEnd.y = 0f;

        Vector3 direction = toEnd.sqrMagnitude > 0.0001f
            ? toEnd.normalized
            : lastMoveDirection;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = Vector3.forward;
        }

        direction.Normalize();

        Vector3 side =
            Vector3.Cross(Vector3.up, direction).normalized;

        float flatDistance =
            Vector3.Distance(
                new Vector3(start.x, 0f, start.z),
                new Vector3(end.x, 0f, end.z));

        float sideSign = 1f;

        if (config.alternateCurveSide)
        {
            sideSign =
                hitTargetCount % 2 == 0
                    ? 1f
                    : -1f;
        }

        float horizontalCurveOffset =
            Mathf.Max(
                config.curveHeight,
                flatDistance * 0.35f);

        Vector3 middle =
            Vector3.Lerp(start, end, 0.5f);

        Vector3 control =
            middle +
            side * horizontalCurveOffset * sideSign;

        if (ShouldUseVerticalArc())
        {
            float minHeight =
                Mathf.Min(
                    config.verticalArcHeightRange.x,
                    config.verticalArcHeightRange.y);

            float maxHeight =
                Mathf.Max(
                    config.verticalArcHeightRange.x,
                    config.verticalArcHeightRange.y);

            float randomVerticalHeight =
                UnityEngine.Random.Range(
                    minHeight,
                    maxHeight);

            control.y =
                start.y + randomVerticalHeight;
        }
        else
        {
            // Lượt này chỉ cong trên mặt phẳng XZ như logic cũ.
            control.y = start.y;
        }

        return new[]
        {
            start,
            control,
            end
        };
    }

    private void OnReachCurrentTarget()
    {
        if (currentTarget == null)
            return;

        if (!visitedTargets.Contains(currentTarget))
            visitedTargets.Add(currentTarget);

        hitTargetCount++;
        Debug.Log($"[HomingChainBullet] Hit target: {currentTarget.Transform.name}, damage: {config.damage}");
    }

    private bool ShouldUseVerticalArc()
    {
        if (config == null ||
            !config.useVerticalArc)
        {
            return false;
        }

        if (!config.alternateVerticalArc)
        {
            return true;
        }

        /*
         * Sau khi trúng target đầu tiên:
         * hitTargetCount = 1.
         *
         * StartNextTarget() được gọi và tạo chain segment đầu tiên.
         * Vì vậy chainMoveIndex đầu tiên phải là 0.
         */
        int chainMoveIndex =
            Mathf.Max(0, hitTargetCount - 1);

        bool isEvenMove =
            chainMoveIndex % 2 == 0;

        return config.verticalArcOnFirstChainMove
            ? isEvenMove
            : !isEvenMove;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent(out ICombatUnit combatUnit))
            return;
        HitEffectManager.Instance.Play(combatUnit);
        DamageResult damageResult = DamageCalculator.CalculateDamage(caster, combatUnit, config.damage);
        combatUnit.TakeDamage(damageResult);
    }

    private float GetFlatSqrDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;

        return dx * dx + dz * dz;
    }

    private float EstimatePathDistance(Vector3[] path)
    {
        float distance = 0f;

        for (int i = 1; i < path.Length; i++)
        {
            distance += Vector3.Distance(path[i - 1], path[i]);
        }

        return distance;
    }

    private Vector3 previousPosition;

    private void UpdateFacingByVelocity()
    {
        Vector3 currentPosition =
            transform.position;

        Vector3 direction =
            currentPosition - previousPosition;

        if (direction.sqrMagnitude > 0.0001f)
        {
            lastMoveDirection = direction.normalized;

            transform.rotation =
                Quaternion.LookRotation(
                    direction.normalized,
                    Vector3.up);
        }

        previousPosition = currentPosition;
    }

    private void FaceDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private void KillMoveTween()
    {
        if (moveTween != null && moveTween.IsActive())
        {
            moveTween.Kill();
            moveTween = null;
        }

        previousPosition = transform.position;
    }
    
    public void OnProjectileSpawnedFromPool()
    {
        // Không gọi Setup ở đây.
        // Caster và config được truyền sau khi Spawn() hoàn tất.

        KillMoveTween();

        isInitialized = false;
        isMovingToVirtualPoint = false;

        currentTarget = null;
        pendingTarget = null;

        hitTargetCount = 0;

        visitedTargets.Clear();

        lastMoveDirection = Vector3.zero;
        previousPosition = transform.position;
    }

    public void OnProjectileDespawnedToPool()
    {
        KillMoveTween();

        config = null;
        caster = null;

        currentTarget = null;
        pendingTarget = null;

        visitedTargets.Clear();

        hitTargetCount = 0;

        isInitialized = false;
        isMovingToVirtualPoint = false;

        lastMoveDirection = Vector3.zero;
        previousPosition = Vector3.zero;
    }

    protected void Start()
    {
        _endStageCancelRegistration =
            BattleFlowController.Instance
                .endStageSessionCancellationTokenSource
                .Token
                .Register(DespawnSelf);
    }

    private void OnDestroy()
    {
        _endStageCancelRegistration.Dispose();
    }
}