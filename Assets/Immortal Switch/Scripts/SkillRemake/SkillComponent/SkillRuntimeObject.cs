using System;
using System.Collections.Generic;
using System.Threading;
using Battle;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Common;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Pooling;
using Immortal_Switch.Scripts.SkillRemake;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Immortal_Switch.Scripts.Skill
{
    /// <summary>
    /// Generic spawned skill object. This object can represent a Spine VFX, particle VFX, aura,
    /// ground effect, slash object, etc. It is independent from the caster after spawning.
    /// </summary>
    public class SkillRuntimeObject : MonoBehaviour
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
        private SkillRuntimeSpawnMode runtimeSpawnMode;
        
        private AddressablePoolHandle addressablePoolHandle;
        private bool hasAddressableInstanceHandle;
        protected CancellationTokenRegistration _endStageCancelRegistration;
        protected CancellationTokenSource cancellationTokenSource;

        private bool isDespawning;
        private int skillId;

        public virtual void Init(
            SkillRuntimeContext context,
            SkillRuntimeObjectConfig config,
            SkillExecutor executor,
            SkillTargetResolver targetResolver,
            ISkillObjectSpawner spawner, object arg = null)
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
            isDespawning = false;
            skillId = context.SkillData.SkillId;

            AddressableSkillSpawnService.OnSkillDespawned += OnDespawnSkill;

            OnRuntimeInitialized(arg);
        }

        private void OnDespawnSkill(int id)
        {
            if (id != skillId)
            {
                return;
            }

            if (runtimeSpawnMode == SkillRuntimeSpawnMode.AddressableInstance)
            {
                ForceDespawn();
            }
        }
        
        public void BindAddressableInstanceSpawn()
        {
            runtimeSpawnMode =
                SkillRuntimeSpawnMode.AddressableInstance;

            addressablePoolHandle = null;
            isDespawning = false;
        }

        public void BindAddressablePoolSpawn(
            AddressablePoolHandle handle)
        {
            runtimeSpawnMode =
                SkillRuntimeSpawnMode.AddressablePool;

            addressablePoolHandle = handle;
            isDespawning = false;
        }

        protected virtual void OnRuntimeInitialized(object arg)
        {
            if (BattleFlowController.Instance.endStageSessionCancellationTokenSource != null)
            {
                _endStageCancelRegistration =
                    BattleFlowController.Instance
                        .endStageSessionCancellationTokenSource
                        .Token
                        .Register(ForceDespawn);
                cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                    this.GetCancellationTokenOnDestroy(), BattleFlowController.Instance.endStageSessionCancellationTokenSource.Token
                );
                return;
            }
            cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                this.GetCancellationTokenOnDestroy()
            );
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
        protected virtual void EmitRuntimeEvent(string eventName)
        {
            DebugCountRuntimeEvent(eventName);

            if (Context == null || Context.SkillData == null || Executor == null)
                return;

            SkillRuntimeContext runtimeContext = Context.CloneForRuntimeObject(this, transform.position);
            Context.SkillData.GetPhasesByEvent(
                Context.SkillLevel,
                SkillPhaseTriggerType.RuntimeObjectSpineEvent,
                eventName,
                PhaseBuffer
            );

            for (int i = 0; i < PhaseBuffer.Count; i++)
                Executor.ExecutePhase(runtimeContext, PhaseBuffer[i]);
        }

        [Button]
        public virtual void ForceDespawn()
        {
            if (isDespawning)
                return;

            isDespawning = true;
            transform.localScale = Vector3.one;
            switch (runtimeSpawnMode)
            {
                case SkillRuntimeSpawnMode.AddressablePool:
                    DespawnAddressablePool();
                    break;

                case SkillRuntimeSpawnMode.AddressableInstance:
                    DespawnAddressableInstance();
                    break;

                default:
                    Debug.LogError(
                        $"[{nameof(SkillRuntimeObject)}] " +
                        $"Invalid SpawnMode on object={name}",
                        this
                    );

                    gameObject.SetActive(false);
                    break;
            }
            
            _endStageCancelRegistration.Dispose();
            CancelSpawnTask();

            if (runtimeSpawnMode == SkillRuntimeSpawnMode.AddressableInstance)
            {
                AddressableSkillSpawnService.OnSkillDespawned -= OnDespawnSkill;
            }

            OnDespawnedToPool();
        }
        
        private void CancelSpawnTask()
        {
            if (cancellationTokenSource == null)
                return;

            if (!cancellationTokenSource.IsCancellationRequested)
            {
                cancellationTokenSource.Cancel();
            }

            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;
        }
        

        protected void OnDestroy()
        {
            _endStageCancelRegistration.Dispose();
            
            if (runtimeSpawnMode == SkillRuntimeSpawnMode.AddressableInstance)
            {
                AddressableSkillSpawnService.OnSkillDespawned -= OnDespawnSkill;
            }
        }

        public void NotifyAddressablePoolDespawned()
        {
            ClearRuntimeState();

            addressablePoolHandle = null;
            isDespawning = false;
        }
        
        private void DespawnAddressablePool()
        {
            AddressablePoolHandle handle =
                addressablePoolHandle;

            addressablePoolHandle = null;

            if (handle == null)
            {
                Debug.LogError(
                    $"[{nameof(SkillRuntimeObject)}] " +
                    $"Missing AddressablePoolHandle. Object={name}",
                    this
                );
                
                ClearRuntimeState();
                gameObject.SetActive(false);
                return;
            }

            // AddressablePoolService.ReturnInternal()
            // sẽ gọi IAddressablePoolable.OnDespawned().
            handle.Despawn();
        }

        private void DespawnAddressableInstance()
        {
            ClearRuntimeState();

            bool released =
                AddressableSpawnService.ReleaseInstance(gameObject);

            if (!released)
            {
                Debug.LogError(
                    $"[{nameof(SkillRuntimeObject)}] " +
                    $"Failed to release Addressable instance. Object={name}",
                    this
                );
            }
        }
        
        private void ClearRuntimeState()
        {
            DebugLogRuntimeEventTotal();

            Despawned?.Invoke(this);
            Despawned = null;

            initialized = false;

            if (Context != null &&
                Context.RuntimeObject == this)
            {
                Context.RuntimeObject = null;
            }

            Context = null;
            Config = null;
            Executor = null;
            TargetResolver = null;
            Spawner = null;

            lifeTimer = 0f;
            debugRuntimeEventCount = 0;
        }

        protected virtual void OnDespawnedToPool()
        {
            
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