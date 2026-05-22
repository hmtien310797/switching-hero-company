using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Hero;
using UnityEngine;
using Battle;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.Skill
{
    [Serializable]
    public sealed class SkillCooldownDebugView
    {
        public string Label;
        public SkillDataSO Skill;
        public float Cooldown;
        public float Remaining;
        public bool Ready;
    }

    public sealed class HeroSkillController : MonoBehaviour
    {
        [Header("Skills")]
        [SerializeField] private List<SkillDataSO> classSkills = new();
        [SerializeField] private SkillDataSO ultimateSkill;
        [SerializeField] private SkillDataSO passiveSkill;

        [Header("Runtime")]
        [SerializeField] private bool autoBindOnAwake = true;

        [Header("Debug Cast - Play Mode")]
        [SerializeField] private bool enableDebugHotkeys = true;
        [SerializeField] private bool debugLogCastResult = true;
        [SerializeField] private KeyCode classSkill1Key = KeyCode.Alpha1;
        [SerializeField] private KeyCode classSkill2Key = KeyCode.Alpha2;
        [SerializeField] private KeyCode classSkill3Key = KeyCode.Alpha3;
        [SerializeField] private KeyCode classSkill4Key = KeyCode.Alpha4;
        [SerializeField] private KeyCode classSkill5Key = KeyCode.Alpha5;
        [SerializeField] private KeyCode ultimateKey = KeyCode.Alpha6;
        [SerializeField] private KeyCode passiveKey = KeyCode.Alpha7;
        [SerializeField] private List<SkillCooldownDebugView> cooldownDebugView = new();

        private PassiveSkillRuntime passiveRuntime;
        private readonly SkillTargetResolver targetResolver = new();
        private readonly Dictionary<SkillDataSO, float> cooldownRemainingBySkill = new();

        private HeroActor owner;
        private HeroAnimationDriver animationDriver;
        private PvEBattleController battleController;
        private ISkillLevelProvider levelProvider = new DefaultSkillLevelProvider();
        private ISkillObjectSpawner objectSpawner = new PoolSkillObjectSpawner();
        private SkillExecutor executor;
        private SkillDataSO currentSkill;
        private int currentSkillLevel;
        private SkillBehaviour currentCustomBehaviour;
        private bool isCasting;

        public HeroActor Owner => owner;
        public SkillDataSO UltimateSkill => ultimateSkill;
        public SkillDataSO PassiveSkill => passiveSkill;
        public bool IsCasting => isCasting;

        public float GetCooldownRemaining(SkillDataSO skillData)
        {
            if (skillData == null)
                return 0f;

            return cooldownRemainingBySkill.TryGetValue(skillData, out float remaining) ? Mathf.Max(0f, remaining) : 0f;
        }

        public bool IsCooldownReady(SkillDataSO skillData)
        {
            return GetCooldownRemaining(skillData) <= 0f;
        }

        private void Awake()
        {
            if (!autoBindOnAwake)
                return;

            owner = GetComponent<HeroActor>();
            animationDriver = GetComponent<HeroAnimationDriver>();
            executor = new SkillExecutor(targetResolver, objectSpawner);
        }

        private void Update()
        {
            TickCooldowns(Time.deltaTime);

            if (enableDebugHotkeys)
                HandleDebugHotkeys();

            RefreshCooldownDebugView();
        }

        private void OnEnable()
        {
            BindEvents();
        }

        private void OnDisable()
        {
            UnbindEvents();
            ResetRuntimeOnSwitchOut();
        }

        public void Init(HeroActor owner, PvEBattleController battleController, ISkillLevelProvider levelProvider = null)
        {
            this.owner = owner;
            this.battleController = battleController;

            if (owner != null)
                animationDriver = owner.Anim;

            if (levelProvider != null)
                this.levelProvider = levelProvider;

            executor = new SkillExecutor(targetResolver, objectSpawner);

            BuildPassiveRuntime();
            BindEvents();
        }

        public void SetSkills(List<SkillDataSO> classSkills, SkillDataSO ultimateSkill, SkillDataSO passiveSkill)
        {
            this.classSkills = classSkills ?? new List<SkillDataSO>();
            this.ultimateSkill = ultimateSkill;
            this.passiveSkill = passiveSkill;
            BuildPassiveRuntime();
        }

        public void SetHeroBoundSkills(SkillDataSO ultimateSkill, SkillDataSO passiveSkill)
        {
            this.ultimateSkill = ultimateSkill;
            this.passiveSkill = passiveSkill;
            BuildPassiveRuntime();
        }

        public void SetClassSkills(List<SkillDataSO> classSkills)
        {
            this.classSkills = classSkills ?? new List<SkillDataSO>();
        }

        // Kept temporarily so old integration code still compiles while migrating from passive list to single passive.
        public void SetSkills(List<SkillDataSO> classSkills, SkillDataSO ultimateSkill, List<SkillDataSO> passiveSkills)
        {
            SkillDataSO firstPassive = passiveSkills != null && passiveSkills.Count > 0 ? passiveSkills[0] : null;
            SetSkills(classSkills, ultimateSkill, firstPassive);
        }

        public bool TryCastClassSkill(int index)
        {
            if (classSkills == null || index < 0 || index >= classSkills.Count)
                return false;

            return TryCastSkill(classSkills[index]);
        }

        public bool TryCastUltimate()
        {
            return TryCastSkill(ultimateSkill);
        }

        public bool TryCastSkill(SkillDataSO skillData)
        {
            if (skillData == null || owner == null || owner.IsDead)
                return false;

            if (owner.Stats != null && !owner.Stats.CanCastSkill())
                return false;

            int level = levelProvider.GetSkillLevel(skillData, owner);

            if (!CanCastSkillByCooldown(skillData, level))
                return false;

            SkillCastConfig castConfig = skillData.GetCastConfig(level);
            ICombatUnit target = targetResolver.ResolveMainTarget(CreateContext(skillData, level, null), castConfig.TargetSelectType);

            if (castConfig.RequireTarget && (target == null || target.IsDead))
                return false;

            if (castConfig.MoveToCastRange && target != null && !IsTargetInRange(target, castConfig.CastRange))
            {
                owner.SetTarget(target);
                owner.MoveTowards(target.Position);
                return false;
            }

            CastSkillInternal(skillData, level, target, interrupt: true);
            return true;
        }

        public void CastSkillImmediately(SkillDataSO skillData)
        {
            if (skillData == null)
                return;

            int level = levelProvider.GetSkillLevel(skillData, owner);

            if (!CanCastSkillByCooldown(skillData, level))
                return;

            SkillCastConfig castConfig = skillData.GetCastConfig(level);
            ICombatUnit target = targetResolver.ResolveMainTarget(CreateContext(skillData, level, null), castConfig.TargetSelectType);
            CastSkillInternal(skillData, level, target, interrupt: true);
        }

        public void CastPassiveTriggeredSkill(SkillDataSO triggeredPassiveSkill)
        {
            SkillDataSO skillToCast = triggeredPassiveSkill != null ? triggeredPassiveSkill : passiveSkill;
            if (skillToCast == null)
                return;

            int level = levelProvider.GetSkillLevel(skillToCast, owner);

            if (!CanCastSkillByCooldown(skillToCast, level))
                return;

            ICombatUnit target = owner != null && owner.HasValidTarget()
                ? owner.CurrentTarget
                : battleController != null && owner != null
                    ? battleController.GetNearestEnemy(owner.Position)
                    : null;

            CastSkillInternal(skillToCast, level, target, interrupt: true);
        }

        public int CountEnemiesInRange(float range)
        {
            if (owner == null)
                return 0;

            ICombatUnit target = owner.HasValidTarget() ? owner.CurrentTarget : null;
            return targetResolver.CountEnemiesInRange(CreateContext(currentSkill, currentSkillLevel, target), owner.Position, range);
        }

        public bool TryDebugCastClassSkill(int oneBasedIndex)
        {
            return TryCastClassSkill(oneBasedIndex - 1);
        }

        public bool TryDebugCastUltimate()
        {
            return TryCastUltimate();
        }

        public bool TryDebugCastPassive()
        {
            if (passiveSkill == null)
                return false;

            return TryCastSkill(passiveSkill);
        }

        [ContextMenu("Debug Cast/Class Skill 1")]
        private void ContextDebugCastClassSkill1() => DebugCastAndLog("Class Skill 1", () => TryDebugCastClassSkill(1));

        [ContextMenu("Debug Cast/Class Skill 2")]
        private void ContextDebugCastClassSkill2() => DebugCastAndLog("Class Skill 2", () => TryDebugCastClassSkill(2));

        [ContextMenu("Debug Cast/Class Skill 3")]
        private void ContextDebugCastClassSkill3() => DebugCastAndLog("Class Skill 3", () => TryDebugCastClassSkill(3));

        [ContextMenu("Debug Cast/Class Skill 4")]
        private void ContextDebugCastClassSkill4() => DebugCastAndLog("Class Skill 4", () => TryDebugCastClassSkill(4));

        [ContextMenu("Debug Cast/Class Skill 5")]
        private void ContextDebugCastClassSkill5() => DebugCastAndLog("Class Skill 5", () => TryDebugCastClassSkill(5));

        [ContextMenu("Debug Cast/Ultimate")]
        private void ContextDebugCastUltimate() => DebugCastAndLog("Ultimate", TryDebugCastUltimate);

        [ContextMenu("Debug Cast/Passive")]
        private void ContextDebugCastPassive() => DebugCastAndLog("Passive", TryDebugCastPassive);

        public void ResetRuntimeOnSwitchOut()
        {
            CancelCurrentSkill();
            passiveRuntime?.Reset();
        }

        private bool CanCastSkillByCooldown(SkillDataSO skillData, int level)
        {
            if (skillData == null)
                return false;

            float remaining = GetCooldownRemaining(skillData);
            if (remaining > 0f)
            {
                if (debugLogCastResult)
                    Debug.Log($"[HeroSkillController] {skillData.SkillName} is on cooldown: {remaining:0.00}s", this);

                return false;
            }

            return true;
        }

        private void StartCooldown(SkillDataSO skillData, int level)
        {
            if (skillData == null)
                return;

            SkillCastConfig castConfig = skillData.GetCastConfig(level);
            float cooldown = castConfig != null ? Mathf.Max(0f, castConfig.Cooldown) : 0f;
            if (cooldown <= 0f)
            {
                cooldownRemainingBySkill.Remove(skillData);
                return;
            }

            cooldownRemainingBySkill[skillData] = cooldown;
        }

        private void TickCooldowns(float deltaTime)
        {
            if (cooldownRemainingBySkill.Count == 0)
                return;

            tempCooldownKeys.Clear();
            foreach (KeyValuePair<SkillDataSO, float> pair in cooldownRemainingBySkill)
                tempCooldownKeys.Add(pair.Key);

            for (int i = 0; i < tempCooldownKeys.Count; i++)
            {
                SkillDataSO skill = tempCooldownKeys[i];
                if (skill == null)
                {
                    cooldownRemainingBySkill.Remove(skill);
                    continue;
                }

                float remaining = cooldownRemainingBySkill[skill] - deltaTime;
                if (remaining <= 0f)
                    cooldownRemainingBySkill.Remove(skill);
                else
                    cooldownRemainingBySkill[skill] = remaining;
            }
        }

        private readonly List<SkillDataSO> tempCooldownKeys = new();

        private void HandleDebugHotkeys()
        {
            if (Input.GetKeyDown(classSkill1Key))
                DebugCastAndLog("Class Skill 1", () => TryDebugCastClassSkill(1));
            if (Input.GetKeyDown(classSkill2Key))
                DebugCastAndLog("Class Skill 2", () => TryDebugCastClassSkill(2));
            if (Input.GetKeyDown(classSkill3Key))
                DebugCastAndLog("Class Skill 3", () => TryDebugCastClassSkill(3));
            if (Input.GetKeyDown(classSkill4Key))
                DebugCastAndLog("Class Skill 4", () => TryDebugCastClassSkill(4));
            if (Input.GetKeyDown(classSkill5Key))
                DebugCastAndLog("Class Skill 5", () => TryDebugCastClassSkill(5));
            if (Input.GetKeyDown(ultimateKey))
                DebugCastAndLog("Ultimate", TryDebugCastUltimate);
            if (Input.GetKeyDown(passiveKey))
                DebugCastAndLog("Passive", TryDebugCastPassive);
        }

        private void DebugCastAndLog(string label, Func<bool> castFunc)
        {
            bool success = castFunc != null && castFunc.Invoke();
            if (!debugLogCastResult)
                return;

            Debug.Log($"[HeroSkillController Debug] {label}: {(success ? "CAST OK" : "CAST FAILED")}", this);
        }

        private void RefreshCooldownDebugView()
        {
            cooldownDebugView.Clear();

            if (classSkills != null)
            {
                for (int i = 0; i < classSkills.Count && i < 5; i++)
                    AddCooldownDebugEntry($"{i + 1}. Class", classSkills[i]);
            }

            AddCooldownDebugEntry("6. Ultimate", ultimateSkill);
            AddCooldownDebugEntry("7. Passive", passiveSkill);
        }

        private void AddCooldownDebugEntry(string label, SkillDataSO skillData)
        {
            if (skillData == null)
                return;

            int level = owner != null ? levelProvider.GetSkillLevel(skillData, owner) : 1;
            SkillCastConfig castConfig = skillData.GetCastConfig(level);
            float cooldown = castConfig != null ? Mathf.Max(0f, castConfig.Cooldown) : 0f;
            float remaining = GetCooldownRemaining(skillData);

            cooldownDebugView.Add(new SkillCooldownDebugView
            {
                Label = string.IsNullOrEmpty(skillData.SkillName) ? label : $"{label} - {skillData.SkillName}",
                Skill = skillData,
                Cooldown = cooldown,
                Remaining = remaining,
                Ready = remaining <= 0f
            });
        }

        private void CastSkillInternal(SkillDataSO skillData, int level, ICombatUnit target, bool interrupt)
        {
            if (interrupt)
                CancelCurrentSkill();

            currentSkill = skillData;
            currentSkillLevel = level;
            isCasting = true;

            StartCooldown(skillData, level);

            SkillRuntimeContext context = CreateContext(skillData, level, target);

            SkillEventBus.Raise(new SkillEventContext
            {
                EventType = GetCastEventType(skillData),
                Owner = owner,
                Source = owner,
                Target = target,
                Skill = skillData
            });

            if (skillData.CustomBehaviourPrefab != null)
            {
                currentCustomBehaviour = Instantiate(skillData.CustomBehaviourPrefab, transform);
                currentCustomBehaviour.Init(context);
                currentCustomBehaviour.Cast();
                return;
            }

            SkillCastConfig castConfig = skillData.GetCastConfig(level);
            if (owner != null && target != null)
            {
                owner.SetTarget(target);
                owner.Anim?.FaceTarget(owner.Position, target.Position);
            }

            SkillRuntimeObjectConfig runtimeConfig = skillData.GetRuntimeObjectConfig(level);
            SkillRuntimeVisualType runtimeType = runtimeConfig != null
                ? runtimeConfig.RuntimeVisualType
                : SkillRuntimeVisualType.HeroSpineAnimation;

            switch (runtimeType)
            {
                case SkillRuntimeVisualType.SpawnedSkillObject:
                    SpawnRuntimeObjectSkill(context, runtimeConfig);
                    break;

                case SkillRuntimeVisualType.ProjectileOnly:
                case SkillRuntimeVisualType.Instant:
                    ExecuteManualPhases(context);
                    FinishCurrentSkill();
                    break;

                case SkillRuntimeVisualType.HeroSpineAnimation:
                default:
                    CastHeroSpineSkill(context, castConfig);
                    break;
            }
        }

        private void SpawnRuntimeObjectSkill(SkillRuntimeContext context, SkillRuntimeObjectConfig runtimeConfig)
        {
            if (runtimeConfig == null || runtimeConfig.RuntimePrefab == null)
            {
                ExecuteManualPhases(context);
                FinishCurrentSkill();
                return;
            }

            if (runtimeConfig.LockCasterWhileAlive && owner != null)
                owner.SetActionLocked(true);

            Vector3 spawnPosition = GetRuntimeObjectSpawnPosition(context, runtimeConfig);
            SkillRuntimeObject runtimeObject = objectSpawner.Spawn(runtimeConfig.RuntimePrefab, spawnPosition, Quaternion.identity);

            if (runtimeObject == null)
            {
                FinishCurrentSkill();
                return;
            }

            SkillRuntimeContext runtimeContext = context.CloneForRuntimeObject(runtimeObject, spawnPosition);
            runtimeObject.Init(runtimeContext, runtimeConfig, executor, targetResolver, objectSpawner);

            // In phase 1, spawned skill objects are independent once created.
            // Projectile/AOE/skill object will live until its own lifetime/animation end.
            if (!runtimeConfig.LockCasterWhileAlive)
                FinishCurrentSkill();
        }

        private Vector3 GetRuntimeObjectSpawnPosition(SkillRuntimeContext context, SkillRuntimeObjectConfig runtimeConfig)
        {
            Vector3 basePosition;
            switch (runtimeConfig.SpawnPositionType)
            {
                case SkillSpawnPositionType.Target:
                    basePosition = context.MainTarget != null ? context.MainTarget.Position : context.TargetPosition;
                    break;

                case SkillSpawnPositionType.CastPosition:
                    basePosition = context.CastPosition;
                    break;

                case SkillSpawnPositionType.Self:
                case SkillSpawnPositionType.ProjectileSpawnPoint:
                case SkillSpawnPositionType.CustomSocket:
                default:
                    basePosition = owner != null ? owner.Position : transform.position;
                    break;
            }

            return basePosition + runtimeConfig.SpawnOffset;
        }

        private void CastHeroSpineSkill(SkillRuntimeContext context, SkillCastConfig castConfig)
        {
            if (owner != null)
                owner.SetActionLocked(true);

            if (!string.IsNullOrEmpty(castConfig.AnimationName))
            {
                animationDriver?.PlaySkill(castConfig.AnimationName);
            }
            else
            {
                ExecuteManualPhases(context);
                FinishCurrentSkill();
            }
        }

        private SkillTriggerEventType GetCastEventType(SkillDataSO skillData)
        {
            if (skillData == null)
                return SkillTriggerEventType.OnCastSkill;

            switch (skillData.OwnerType)
            {
                case SkillOwnerType.UltimateSkill:
                    return SkillTriggerEventType.OnCastUltimateSkill;
                case SkillOwnerType.ClassSkill:
                    return SkillTriggerEventType.OnCastClassSkill;
                default:
                    return SkillTriggerEventType.OnCastSkill;
            }
        }

        private void ExecuteManualPhases(SkillRuntimeContext context)
        {
            SkillLevelData levelData = context.SkillData.GetLevelData(context.SkillLevel);
            if (levelData == null || levelData.Phases == null)
                return;

            for (int i = 0; i < levelData.Phases.Count; i++)
            {
                SkillPhaseData phase = levelData.Phases[i];
                if (phase != null && phase.TriggerType == SkillPhaseTriggerType.Manual)
                    executor.ExecutePhase(context, phase);
            }
        }

        private SkillRuntimeContext CreateContext(SkillDataSO skillData, int level, ICombatUnit target)
        {
            Vector3 ownerPos = owner != null ? owner.Position : transform.position;
            return new SkillRuntimeContext
            {
                Caster = owner,
                MainTarget = target,
                SkillData = skillData,
                SkillLevel = level,
                CastPosition = ownerPos,
                TargetPosition = target != null ? target.Position : ownerPos,
                BattleController = battleController,
                SkillController = this
            };
        }

        private bool IsTargetInRange(ICombatUnit target, float range)
        {
            if (owner == null || target == null)
                return false;

            Vector3 self = owner.Position;
            Vector3 targetPos = target.Position;
            self.y = 0f;
            targetPos.y = 0f;
            return (targetPos - self).sqrMagnitude <= range * range;
        }

        private void OnSpineEvent(string eventName)
        {
            if (!isCasting || currentSkill == null)
                return;

            currentCustomBehaviour?.OnSpineEvent(eventName);

            SkillPhaseData phase = currentSkill.GetPhaseByEvent(currentSkillLevel, SkillPhaseTriggerType.SpineEvent, eventName);
            if (phase == null)
                return;

            ICombatUnit target = owner != null && owner.HasValidTarget() ? owner.CurrentTarget : null;
            SkillRuntimeContext context = CreateContext(currentSkill, currentSkillLevel, target);
            executor.ExecutePhase(context, phase);
        }

        private void OnAnimationComplete(string animationName)
        {
            currentCustomBehaviour?.OnAnimationComplete(animationName);
            FinishCurrentSkill();
        }

        private void FinishCurrentSkill()
        {
            isCasting = false;
            currentSkill = null;
            currentSkillLevel = 0;
            currentCustomBehaviour = null;

            if (owner != null)
                owner.SetActionLocked(false);
        }

        private void CancelCurrentSkill()
        {
            currentCustomBehaviour?.Cancel();
            if (currentCustomBehaviour != null)
                Destroy(currentCustomBehaviour.gameObject);

            isCasting = false;
            currentSkill = null;
            currentSkillLevel = 0;
            currentCustomBehaviour = null;

            if (owner != null)
                owner.SetActionLocked(false);
        }

        private void BuildPassiveRuntime()
        {
            passiveRuntime = null;

            if (passiveSkill == null)
                return;

            int level = levelProvider.GetSkillLevel(passiveSkill, owner);
            passiveRuntime = new PassiveSkillRuntime(this, passiveSkill, level);
        }

        private void OnSkillEvent(SkillEventContext context)
        {
            if (context == null || owner == null || owner.IsDead || !gameObject.activeInHierarchy)
                return;

            passiveRuntime?.HandleEvent(context);
        }

        private void BindEvents()
        {
            if (animationDriver != null)
            {
                animationDriver.SpineEventTriggered -= OnSpineEvent;
                animationDriver.AnimationCompleted -= OnAnimationComplete;
                animationDriver.SpineEventTriggered += OnSpineEvent;
                animationDriver.AnimationCompleted += OnAnimationComplete;
            }

            SkillEventBus.EventRaised -= OnSkillEvent;
            SkillEventBus.EventRaised += OnSkillEvent;
        }

        private void UnbindEvents()
        {
            if (animationDriver != null)
            {
                animationDriver.SpineEventTriggered -= OnSpineEvent;
                animationDriver.AnimationCompleted -= OnAnimationComplete;
            }

            SkillEventBus.EventRaised -= OnSkillEvent;
        }
    }
}
