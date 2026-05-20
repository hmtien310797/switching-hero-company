using System.Collections.Generic;
using Immortal_Switch.Scripts.Hero;

public interface IHeroState
{
    HeroStateId Id { get; }
    void Enter();
    void Tick(float deltaTime);
    void Exit();
}

public class HeroStateMachine
{
    private readonly HeroActor owner;
    private readonly Dictionary<HeroStateId, IHeroState> states = new();

    private IHeroState currentState;

    public HeroStateId CurrentStateId => currentState != null ? currentState.Id : HeroStateId.Idle;

    public HeroStateMachine(HeroActor owner)
    {
        this.owner = owner;

        states[HeroStateId.Idle] = new HeroIdleState(owner, this);
        states[HeroStateId.Run] = new HeroRunState(owner, this);
        states[HeroStateId.Attack] = new HeroAttackState(owner, this);
        states[HeroStateId.Ultimate] = new HeroUltimateState(owner, this);
        states[HeroStateId.Passive] = new HeroPassiveState(owner, this);
        states[HeroStateId.Dead] = new HeroDeadState(owner, this);
        states[HeroStateId.Win] = new HeroWinState(owner, this);
    }

    public void Tick(float deltaTime)
    {
        currentState?.Tick(deltaTime);
    }

    public void ChangeState(HeroStateId id, bool force = false)
    {
        if (currentState != null && currentState.Id == id && !force)
            return;

        if (owner.IsDead && id != HeroStateId.Dead)
            return;

        currentState?.Exit();

        currentState = states[id];
        currentState.Enter();
    }
}