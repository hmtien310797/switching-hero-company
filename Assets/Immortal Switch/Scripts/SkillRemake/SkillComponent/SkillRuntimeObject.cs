using System;
using System.Collections.Generic;
using Common;
using UnityEngine;

namespace Immortal_Switch.Scripts.Skill
{
    /// <summary>
    /// Generic spawned skill object. This object can represent a Spine VFX, particle VFX, aura,
    /// ground effect, slash object, etc. It is independent from the caster after spawning.
    /// </summary>
    public class SkillRuntimeObject : PoolableBehaviour
    {
        public event Action<SkillRuntimeObject> Despawned;

        private static readonly List<SkillPhaseData> PhaseBuffer = new();

        [Header("Debug Runtime Event")]
        [SerializeField] private bool debugCountRuntimeEvent;
        [SerializeField] private string debugCountEventName = "hit";
        [SerializeField] private bool debugLogRuntimeEventCount = true;
        [SerializeField] private bool debugLogRuntimeEventTotalOnDespawn = true;

        private int debugRuntimeEventCount;

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

            debugRuntimeEventCount = 0;

            OnRuntimeInitialized();
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
            DebugCountRuntimeEvent(eventName);

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
            DebugLogRuntimeEventTotal();

            Despawned?.Invoke(this);
            Despawned = null;

            initialized = false;
            Context = null;
            Config = null;
            Executor = null;
            TargetResolver = null;
            Spawner = null;

            debugRuntimeEventCount = 0;
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void DebugCountRuntimeEvent(string eventName)
        {
            if (!debugCountRuntimeEvent)
                return;

            if (string.IsNullOrEmpty(eventName))
                return;

            if (!string.Equals(eventName, debugCountEventName, StringComparison.OrdinalIgnoreCase))
                return;

            debugRuntimeEventCount++;

            if (!debugLogRuntimeEventCount)
                return;

            string skillName = Context != null && Context.SkillData != null
                ? Context.SkillData.SkillName
                : "NULL_SKILL";

            string casterName = Context != null && Context.Caster != null
                ? Context.Caster.name
                : "NULL_CASTER";

            Debug.Log(
                $"<color=#FFCA28>[Skill Event Count]</color> " +
                $"Skill: <b>{skillName}</b> | " +
                $"Caster: <b>{casterName}</b> | " +
                $"Object: <b>{name}</b> | " +
                $"Event: <color=#81C784>{eventName}</color> | " +
                $"Count: <color=#4FC3F7>{debugRuntimeEventCount}</color>",
                this
            );
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void DebugLogRuntimeEventTotal()
        {
            if (!debugCountRuntimeEvent || !debugLogRuntimeEventTotalOnDespawn)
                return;

            if (debugRuntimeEventCount <= 0)
                return;

            string skillName = Context != null && Context.SkillData != null
                ? Context.SkillData.SkillName
                : "NULL_SKILL";

            string casterName = Context != null && Context.Caster != null
                ? Context.Caster.name
                : "NULL_CASTER";

            Debug.Log(
                $"<color=#AB47BC>[Skill Event Total]</color> " +
                $"Skill: <b>{skillName}</b> | " +
                $"Caster: <b>{casterName}</b> | " +
                $"Object: <b>{name}</b> | " +
                $"Event: <color=#81C784>{debugCountEventName}</color> | " +
                $"Total Count: <color=#4FC3F7>{debugRuntimeEventCount}</color>",
                this
            );
        }
    }
}