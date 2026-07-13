using System;
using System.Threading;
using Battle;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public abstract class HeroStateBase : IHeroState
{
    protected readonly HeroActor owner;
    protected readonly HeroStateMachine stateMachine;

    public abstract HeroStateId Id { get; }

    protected HeroStateBase(HeroActor owner, HeroStateMachine stateMachine)
    {
        this.owner = owner;
        this.stateMachine = stateMachine;
    }

    public virtual async UniTask Enter()
    {
    }

    public virtual void Tick(float deltaTime)
    {
    }

    public virtual void Exit()
    {
    }
}

public class HeroIdleState : HeroStateBase
{
    public override HeroStateId Id => HeroStateId.Idle;

    public HeroIdleState(HeroActor owner, HeroStateMachine stateMachine) : base(owner, stateMachine)
    {
    }

    public override async UniTask Enter()
    {
        owner.Anim.PlayIdle();
    }

    public override void Tick(float deltaTime)
    {
        if (owner.IsUnderPlayerControl)
            return;

        if (owner.MoveMode != HeroMoveMode.Auto)
            return;

        if (!owner.HasValidTarget())
            owner.SearchTarget();

        if (!owner.HasValidTarget())
            return;

        if (owner.IsTargetInAttackRange())
            stateMachine.ChangeState(HeroStateId.Attack);
        else
            stateMachine.ChangeState(HeroStateId.Run);
    }
}

public class HeroRunState : HeroStateBase
{
    public override HeroStateId Id => HeroStateId.Run;

    public HeroRunState(HeroActor owner, HeroStateMachine stateMachine) : base(owner, stateMachine)
    {
    }

    public override async UniTask Enter()
    {
        owner.Anim.PlayRun();
    }

    public override void Tick(float deltaTime)
    {
        if (owner.IsUnderPlayerControl)
            return;

        if (!owner.HasValidTarget())
        {
            owner.SearchTarget();

            if (!owner.HasValidTarget())
            {
                stateMachine.ChangeState(HeroStateId.Idle);
                return;
            }
        }

        if (owner.IsTargetInAttackRange())
        {
            stateMachine.ChangeState(HeroStateId.Attack);
            return;
        }

        owner.MoveTowards(owner.CurrentTarget.Position);
    }
}

public class HeroAttackState : HeroStateBase
{
    public override HeroStateId Id => HeroStateId.Attack;

    private const string HitEventName = "hit";

    private float duration;
    private float timer;
    private bool hasHit;
    private string currentAttackAnim;

    public HeroAttackState(HeroActor owner, HeroStateMachine stateMachine)
        : base(owner, stateMachine)
    {
    }

    public override async UniTask Enter()
    {
        if (owner.IsDead)
        {
            stateMachine.ChangeState(HeroStateId.Dead);
            return;
        }

        if (owner.Stats != null && !owner.Stats.CanAttack())
        {
            stateMachine.ChangeState(HeroStateId.Idle);
            return;
        }

        if (!owner.HasValidTarget())
        {
            owner.ResetAttackCombo();
            stateMachine.ChangeState(HeroStateId.Idle);
            return;
        }

        owner.Anim.FaceTarget(
            owner.transform.position,
            owner.CurrentTarget.Position
        );

        int comboIndex = owner.GetCurrentAttackComboIndex();

        currentAttackAnim = owner.Anim.GetAttackAnimationName(comboIndex);
        duration = owner.Anim.PlayAttack(comboIndex);

        timer = 0f;
        hasHit = false;

        owner.Anim.SpineEventTriggered += OnSpineEvent;
        owner.Anim.AnimationCompleted += OnAnimationCompleted;
    }

    public override void Tick(float deltaTime)
    {
        if (owner.IsDead)
        {
            stateMachine.ChangeState(HeroStateId.Dead);
            return;
        }

        if (owner.Stats != null && !owner.Stats.CanAttack())
        {
            owner.ResetAttackCombo();
            stateMachine.ChangeState(HeroStateId.Idle);
            return;
        }

        if (!owner.HasValidTarget())
        {
            owner.ResetAttackCombo();
            stateMachine.ChangeState(HeroStateId.Idle);
            return;
        }

        timer += deltaTime;

        // Fallback nếu animation không bắn complete event.
        if (timer >= duration)
        {
            FinishAttack();
        }
    }

    public override void Exit()
    {
        if (owner.Anim == null)
            return;

        owner.Anim.SpineEventTriggered -= OnSpineEvent;
        owner.Anim.AnimationCompleted -= OnAnimationCompleted;
    }

    private void OnSpineEvent(string eventName)
    {
        if (hasHit)
            return;

        if (eventName != HitEventName)
            return;

        hasHit = true;

        owner.DealAttackDamage();
    }

    private void OnAnimationCompleted(string animationName)
    {
        if (animationName != currentAttackAnim)
            return;

        FinishAttack();
    }

    private void FinishAttack()
    {
        owner.AdvanceAttackCombo();
        stateMachine.ChangeState(HeroStateId.Idle);
    }
}

public class HeroUltimateState : HeroStateBase
{
    public override HeroStateId Id => HeroStateId.Ultimate;

    public HeroUltimateState(HeroActor owner, HeroStateMachine stateMachine) : base(owner, stateMachine)
    {
    }

    public override UniTask Enter()
    {
        owner.SetActionLocked(true);
        return UniTask.CompletedTask;
    }

    public override void Exit()
    {
        owner.SetActionLocked(false);
    }
}

public class HeroPassiveState : HeroStateBase
{
    public override HeroStateId Id => HeroStateId.Passive;

    private float duration;
    private float timer;
    private bool hasTriggered;

    public HeroPassiveState(HeroActor owner, HeroStateMachine stateMachine) : base(owner, stateMachine)
    {
    }

    public override async UniTask Enter()
    {
        duration = 2.2f;
        owner.SetActionLocked(true);
        timer = 0f;
        hasTriggered = false;
    }

    public override void Tick(float deltaTime)
    {
        timer += deltaTime;

        float triggerTime = duration;

        if (!hasTriggered && timer >= triggerTime)
        {
            hasTriggered = true;
        }

        if (timer >= duration)
        {
            owner.SetActionLocked(false);
            stateMachine.ChangeState(HeroStateId.Idle);
        }
    }

    public override void Exit()
    {
        owner.SetActionLocked(false);
    }
}

public class HeroDeadState : HeroStateBase
{
    public override HeroStateId Id => HeroStateId.Dead;

    public HeroDeadState(HeroActor owner, HeroStateMachine stateMachine) : base(owner, stateMachine)
    {
    }

    public override async UniTask Enter()
    {
        owner.SetActionLocked(true);
        owner.Anim.PlayDead();
    }

    public override void Exit()
    {
        owner.SetActionLocked(false);
    }
}

public class HeroBossSpawnState : HeroStateBase
{
    public override HeroStateId Id => HeroStateId.BossSpawn;

    public HeroBossSpawnState(HeroActor owner, HeroStateMachine stateMachine) : base(owner, stateMachine)
    {
    }

    public override UniTask Enter()
    {
        owner.SetActionLocked(true);
        owner.Anim.PlayIdle();
        return UniTask.CompletedTask;
    }

    public override void Exit()
    {
        owner.SetActionLocked(true);
    }
}

public class HeroManualMoveState : HeroStateBase
{
    public override HeroStateId Id => HeroStateId.ManualMove;

    public HeroManualMoveState(HeroActor owner, HeroStateMachine stateMachine) : base(owner, stateMachine)
    {
    }

    public override async UniTask Enter()
    {
        owner.Anim.PlayRun();
    }

    public override void Tick(float deltaTime)
    {
        if (owner.IsUnderPlayerControl)
            return;

        owner.MoveTowards(owner.CurrentTarget.Position);
    }
}

public class HeroSpawnState : HeroStateBase
{
    public override HeroStateId Id => HeroStateId.Spawn;

    public HeroSpawnState(HeroActor owner, HeroStateMachine stateMachine) : base(owner, stateMachine)
    {
    }

    public override async UniTask Enter()
    {
        owner.SetActionLocked(true);
        owner.Anim.PlaySpawn();
        await UniTask.Delay(TimeSpan.FromSeconds(2f));
        owner.ActiveHealthBar(true);
        stateMachine.ChangeState(HeroStateId.Idle);
    }
}

public class HeroWinState : HeroStateBase
{
    public override HeroStateId Id => HeroStateId.Win;

    public HeroWinState(HeroActor owner, HeroStateMachine stateMachine) : base(owner, stateMachine)
    {
    }

    public override async UniTask Enter()
    {
        if (owner == null)
            return;

        CancellationToken cancellationToken = BattleFlowController.Instance.stageFlowCancellationTokenSource.Token;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            owner.Anim.PlayIdle();

            await UniTask.Delay(
                TimeSpan.FromSeconds(1.5f),
                cancellationToken: cancellationToken
            );

            cancellationToken.ThrowIfCancellationRequested();

            float duration = Mathf.Max(
                0f,
                owner.Anim.PlayWin() - 0.5f
            );

            await UniTask.Delay(
                TimeSpan.FromSeconds(duration),
                cancellationToken: cancellationToken
            );

            cancellationToken.ThrowIfCancellationRequested();

            owner.EnableWinFx(true);

            Vector3 startPosition = owner.transform.position;

            Vector3[] path =
            {
                startPosition
                - Vector3.forward * 20f
                - Vector3.right * 10f,

                startPosition
                - Vector3.forward * 35f
                + Vector3.up * 5f,

                startPosition
                - Vector3.forward * 20f
                + Vector3.right * 10f,

                startPosition
                + Vector3.right * 15f,

                PvEBattleController.Instance.GetEndMapPoint()
            };

            await owner.transform
                .DOPath(path, 3f, PathType.CatmullRom)
                .SetEase(Ease.InQuart)
                .ToUniTask(
                    TweenCancelBehaviour.Kill,
                    cancellationToken
                );
        }
        catch (OperationCanceledException)
        {
            // Đây là cancellation hợp lệ:
            // owner bị destroy hoặc flow bị hủy.
        }
        finally
        {
            if (owner != null)
            {
                owner.EnableWinFx(false);
            }
        }
    }
}