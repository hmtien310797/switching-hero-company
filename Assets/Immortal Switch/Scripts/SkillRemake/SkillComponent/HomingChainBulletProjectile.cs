using System;
using System.Collections.Generic;
using Battle;
using Common;
using DG.Tweening;
using Immortal_Switch.Scripts.Combat;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

public class HomingChainBulletProjectile : PoolableBehaviour
{
    private readonly List<ICombatUnit> visitedTargets = new();

    private HomingChainBulletConfig config;
    private ICombatUnit caster;

    private ICombatUnit currentTarget;
    private Tween moveTween;

    private Vector3 lastMoveDirection;
    private int hitTargetCount;
    private bool isInitialized;

    public void Setup(
        ICombatUnit owner,
        Vector3 spawnPosition,
        Vector3 initialDirection,
        HomingChainBulletConfig bulletConfig)
    {
        caster = owner;
        config = bulletConfig;

        transform.position = spawnPosition;

        visitedTargets.Clear();
        hitTargetCount = 0;
        isInitialized = true;

        lastMoveDirection = initialDirection.sqrMagnitude > 0.0001f
            ? initialDirection.normalized
            : transform.forward;

        StartFirstTarget();
    }

    private void StartFirstTarget()
    {
        currentTarget = PvEBattleController.Instance.GetRandomEnemyAlive();

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

        currentTarget = PvEBattleController.Instance.GetRandomFromFarthestEnemies(
            transform.position,
            visitedTargets,
            5
        );

        if (currentTarget == null)
        {
            DespawnSelf();
            return;
        }

        Vector3 start = transform.position;

        // Snapshot vị trí target đúng 1 lần.
        Vector3 end = currentTarget.Transform.position;
        end.y = start.y;

        Vector3[] path = BuildCurvePath(start, end);

        float distance = EstimatePathDistance(path);
        float duration = config.curveSpeed <= 0f
            ? 0.01f
            : distance / config.curveSpeed;

        KillMoveTween();

        moveTween = transform
            .DOPath(path, duration, PathType.CatmullRom, PathMode.Full3D, 12)
            .SetEase(Ease.Linear)
            .OnUpdate(UpdateFacingByVelocity)
            .OnComplete(() =>
            {
                OnReachCurrentTarget();
                StartNextTarget();
            });
    }

    private Vector3[] BuildCurvePath(Vector3 start, Vector3 end)
    {
        // Giữ bullet nằm trên cùng mặt phẳng XZ
        end.y = start.y;

        Vector3 toEnd = end - start;
        toEnd.y = 0f;

        Vector3 dir = toEnd.sqrMagnitude > 0.0001f
            ? toEnd.normalized
            : lastMoveDirection;

        dir.y = 0f;
        dir.Normalize();
        
        Vector3 side = Vector3.Cross(Vector3.up, dir).normalized;

        float distance = Vector3.Distance(start, end);
        float sideSign = 1f;

        if (config.alternateCurveSide)
        {
            sideSign = hitTargetCount % 2 == 0 ? 1f : -1f;
        }

        float curveOffset = Mathf.Max(config.curveHeight, distance * 0.35f);

        Vector3 middle = (start + end) * 0.5f;

        Vector3 control = middle + side * curveOffset * sideSign;
        control.y = start.y;

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
        Vector3 currentPosition = transform.position;
        Vector3 direction = currentPosition - previousPosition;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
        {
            lastMoveDirection = direction.normalized;
            FaceDirection(lastMoveDirection);
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

    public override void OnDespawnedToPool()
    {
        KillMoveTween();

        config = null;
        caster = null;
        currentTarget = null;

        visitedTargets.Clear();

        hitTargetCount = 0;
        isInitialized = false;
        lastMoveDirection = Vector3.zero;

        base.OnDespawnedToPool();
    }

    private void OnDisable()
    {
        KillMoveTween();
    }
}