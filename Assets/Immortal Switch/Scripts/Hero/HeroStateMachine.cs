using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

public interface IHeroState
{
    HeroStateId Id { get; }
    UniTask Enter();
    void Tick(float deltaTime);
    void Exit();
}
[Serializable]
public class HeroStateMachine
{
    private readonly HeroActor owner;
    private readonly Dictionary<HeroStateId, IHeroState> states = new();
    private IHeroState currentState;

    [ShowInInspector]
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
        states[HeroStateId.Spawn] = new HeroSpawnState(owner, this);
        states[HeroStateId.BossSpawn] = new HeroBossSpawnState(owner, this);
        states[HeroStateId.ManualMove] = new HeroManualMoveState(owner, this);
    }

    public void Tick(float deltaTime)
    {
        currentState?.Tick(deltaTime);
    }

    public void ChangeState(HeroStateId id, bool force = false)
    {
        HeroStateId fromId = currentState != null ? currentState.Id : HeroStateId.Idle;
        if (currentState != null && currentState.Id == id && !force)
            return;

        if (owner.IsDead && id != HeroStateId.Dead)
            return;
        
        if (owner.IsDead && id != HeroStateId.Dead)
        {
            LogBlockedStateChange(fromId, id, "Owner is dead");
            return;
        }

        if (!states.TryGetValue(id, out IHeroState nextState))
        {
            LogBlockedStateChange(fromId, id, "State not registered");
            return;
        }

        LogStateChange(fromId, id, force);

        currentState?.Exit();
        currentState = nextState;
        currentState.Enter().Forget();
    }
    
    [System.Diagnostics.Conditional("UNITY_EDITOR")]

    private void LogStateChange(HeroStateId from, HeroStateId to, bool force)
    {
        string heroName = owner != null ? owner.name : "NULL";
        Debug.Log(
            $"<color=#4FC3F7>[HeroState]</color> " +
            $"<b>{heroName}</b>: " +
            $"<color=#FFD54F>{from}</color> → <color=#81C784>{to}</color>" +
            $"{(force ? " <color=#FF8A65>[FORCE]</color>" : "")}",
            owner
        );
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]

    private void LogBlockedStateChange(HeroStateId from, HeroStateId to, string reason)
    {
        string heroName = owner != null ? owner.name : "NULL";

        Debug.LogWarning(

            $"<color=#EF5350>[HeroState BLOCKED]</color> " +
            $"<b>{heroName}</b>: " +
            $"<color=#FFD54F>{from}</color> → <color=#E57373>{to}</color> | Reason: {reason}",
            owner
        );
    }
}