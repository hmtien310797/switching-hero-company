using System;
using System.Collections.Generic;
using Common;
using Immortal_Switch.Scripts.Core;
using UnityEngine;

namespace Immortal_Switch.Scripts.Skill
{
    /// <summary>
    /// Generic spawned skill object. This object can represent a Spine VFX, particle VFX, aura,
    /// ground effect, slash object, etc. It is independent from the caster after spawning.
    /// </summary>
    public class SkillRuntimeObject : PoolableBehaviour
    {
        private static readonly List<SkillPhaseData> PhaseBuffer = new();

        protected SkillRuntimeContext Context;
        protected SkillRuntimeObjectConfig Config;
        protected SkillExecutor Executor;
        protected SkillTargetResolver TargetResolver;
        protected ISkillObjectSpawner Spawner;

        private float lifeTimer;
        private bool initialized;

        public virtual void Init(
            SkillRuntimeContext context,
            SkillRuntimeObjectConfig config,
            SkillExecutor executor,
            SkillTargetResolver targetResolver,
            ISkillObjectSpawner spawner)
        {
            Context = context;
            Config = config;
            Executor = executor;
            TargetResolver = targetResolver;
            Spawner = spawner;

            if (Context != null)
                Context.RuntimeObject = this;

            lifeTimer = config != null ? config.LifeTime : 0f;
            initialized = true;

            OnRuntimeInitialized();
        }

        private void Awake()
        {
            GameEventManager.Subscribe(GameEvents.OnStageCleared, ForceDespawn);
            GameEventManager.Subscribe(GameEvents.OnStageLost, ForceDespawn);
        }

        protected virtual void OnRuntimeInitialized()
        {
        }

        protected virtual void Update()
        {
            if (!initialized)
                return;

            UpdateFollow();
            UpdateLifeTime();
        }

        protected virtual void UpdateFollow()
        {
            if (Config == null || Context == null)
                return;

            Transform followTarget = null;

            switch (Config.FollowType)
            {
                case SkillFollowType.FollowSelf:
                    followTarget = Context.Caster != null ? Context.Caster.transform : null;
                    break;

                case SkillFollowType.FollowTarget:
                    followTarget = Context.MainTarget is Component component ? component.transform : null;
                    break;
            }

            if (followTarget == null)
                return;

            transform.position = followTarget.position + Config.SpawnOffset;
        }

        protected virtual void UpdateLifeTime()
        {
            if (Config == null || !Config.UseLifeTime)
                return;

            lifeTimer -= Time.deltaTime;
            if (lifeTimer <= 0f)
                ForceDespawn();
        }

        /// <summary>
        /// Fired by this runtime object. For Spine objects, call this from Spine event callback.
        /// For particle/timeline objects later, call this from animation event or script.
        /// </summary>
        public void EmitRuntimeEvent(string eventName)
        {
            if (Context == null || Context.SkillData == null || Executor == null)
                return;

            SkillRuntimeContext runtimeContext = Context.CloneForRuntimeObject(this, transform.position);
            Context.SkillData.GetPhasesByEvent(
                Context.SkillLevel,
                SkillPhaseTriggerType.RuntimeObjectEvent,
                eventName,
                PhaseBuffer
            );

            for (int i = 0; i < PhaseBuffer.Count; i++)
                Executor.ExecutePhase(runtimeContext, PhaseBuffer[i]);
        }

        public virtual void ForceDespawn()
        {
            DespawnSelf();
        }

        public override void OnDespawnedToPool()
        {
            initialized = false;
            Context = null;
            Config = null;
            Executor = null;
            TargetResolver = null;
            Spawner = null;
        }
    }
}
