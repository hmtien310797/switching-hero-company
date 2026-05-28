using System;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Hero;
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

    public virtual async UniTask Enter() { }
    public virtual void Tick(float deltaTime) { }
    public virtual void Exit() { }
}

public class HeroIdleState : HeroStateBase
{
    public override HeroStateId Id => HeroStateId.Idle;

    public HeroIdleState(HeroActor owner, HeroStateMachine stateMachine) : base(owner, stateMachine) { }

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

    public HeroRunState(HeroActor owner, HeroStateMachine stateMachine) : base(owner, stateMachine) { }

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

    private float duration;
    private float timer;
    private bool hasTriggered;

    public HeroUltimateState(HeroActor owner, HeroStateMachine stateMachine) : base(owner, stateMachine) { }

    public override async UniTask Enter()
    {
        owner.SetActionLocked(true);

        duration = owner.Anim.PlayUltimate();
        timer = 0f;
        hasTriggered = false;
    }

    public override void Tick(float deltaTime)
    {
        timer += deltaTime;

        float triggerTime = duration * owner.UltimateHitNormalizedTime;

        if (!hasTriggered && timer >= triggerTime)
        {
            hasTriggered = true;

            // TODO:
            // Ultimate thật nên gọi HeroUltimateAbility / AbilityExecutor ở đây.
            // Hiện tại showcase tạm damage current target.
            // if (owner.HasValidTarget())
            //     owner.CurrentTarget.ReceiveDamage(3f);
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

public class HeroPassiveState : HeroStateBase
{
    public override HeroStateId Id => HeroStateId.Passive;

    private float duration;
    private float timer;
    private bool hasTriggered;

    public HeroPassiveState(HeroActor owner, HeroStateMachine stateMachine) : base(owner, stateMachine) { }

    public override async UniTask Enter()
    {
        owner.SetActionLocked(true);

        duration = owner.Anim.PlayPassive();
        timer = 0f;
        hasTriggered = false;
    }

    public override void Tick(float deltaTime)
    {
        timer += deltaTime;

        float triggerTime = duration * owner.PassiveHitNormalizedTime;

        if (!hasTriggered && timer >= triggerTime)
        {
            hasTriggered = true;

            // TODO:
            // Passive thật nên tách thành HeroPassiveAbility.
            // Có passive là aura/stat modifier, có passive là active proc animation.
            // Bản showcase tạm gây damage nhỏ.
            // if (owner.HasValidTarget())
            //     owner.CurrentTarget.ReceiveDamage(1.5f);
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

    public HeroDeadState(HeroActor owner, HeroStateMachine stateMachine) : base(owner, stateMachine) { }

    public override async UniTask Enter()
    {
        owner.SetActionLocked(false);
        owner.Anim.PlayDead();
    }

    public override void Tick(float deltaTime)
    {
        // Dead là terminal state.
        // Không cho thực hiện hành động khác.
    }
}

public class HeroBossSpawnState : HeroStateBase
{
    public override HeroStateId Id => HeroStateId.BossSpawn;

    public HeroBossSpawnState(HeroActor owner, HeroStateMachine stateMachine) : base(owner, stateMachine) { }

    public override UniTask Enter()
    {
        if (owner.IsCastingUltimateSkill)
        {
            return UniTask.CompletedTask;
        }
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

    public HeroManualMoveState(HeroActor owner, HeroStateMachine stateMachine) : base(owner, stateMachine) { }
    
}

public class HeroSpawnState : HeroStateBase
{
    public override HeroStateId Id => HeroStateId.Spawn;

    public HeroSpawnState(HeroActor owner, HeroStateMachine stateMachine) : base(owner, stateMachine) { }

    public override async UniTask Enter()
    {
        owner.SetActionLocked(false);
        owner.Anim.PlaySpawn();
        await UniTask.Delay(TimeSpan.FromSeconds(2f));
        owner.ActiveHealthBar(true);
        stateMachine.ChangeState(HeroStateId.Idle);
    }
}

public class HeroWinState : HeroStateBase
{
    public override HeroStateId Id => HeroStateId.Win;

    public HeroWinState(HeroActor owner, HeroStateMachine stateMachine) : base(owner, stateMachine) { }

    public override async UniTask Enter()
    {
        float duration = owner.Anim.PlayWin();
        await UniTask.Delay(TimeSpan.FromSeconds(duration));
        owner.EnableWinFx(true);
        Vector3[] path = new Vector3[] 
        {
            owner.transform.position - Vector3.forward * 20 - Vector3.right * 10,
            owner.transform.position - Vector3.forward * 35 + Vector3.up *5,
            owner.transform.position - Vector3.forward * 20 + Vector3.right * 10,
            owner.transform.position + Vector3.forward * 0 + Vector3.right * 15,
            PvEBattleController.Instance.GetMapEndPoint(),
        };

        transform.DOPath(path, 3f, PathType.CatmullRom).SetEase(Ease.InQuart).OnComplete(() =>
        {
            winFx.gameObject.SetActive(false);
            endAct?.Invoke();
        });
        
    }
}