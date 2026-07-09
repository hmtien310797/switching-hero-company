using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Hero;
using UnityEngine;
using Battle;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.SkillRemake;
using Immortal_Switch.Scripts.Sound;
using Immortal_Switch.Scripts.StatSystem;
using Sirenix.OdinInspector;
using Spine.Unity;

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
        [SerializeField] private HeroAutoSkillController autoSkillController;
        [SerializeField] private bool autoCreateAutoSkillController = true;

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
        private SkeletonAnimation ownerSkeletonAnimation;
        private int activePassiveAuraTrack = -1;
        private readonly SkillTargetResolver targetResolver = new();
        private readonly Dictionary<SkillDataSO, float> cooldownRemainingBySkill = new();

        private HeroActor owner;
        private HeroAnimationDriver animationDriver;
        private IHeroBattleContext battleContext;
        private ISkillLevelProvider levelProvider = new DefaultSkillLevelProvider();
        private ISkillObjectSpawner objectSpawner = new PoolSkillObjectSpawner();
        private SkillExecutor executor;
        private SkillDataSO currentSkill;
        private int currentSkillLevel;
        private SkillBehaviour currentCustomBehaviour;
        //ra chieu dong loat nen ko can
        //private bool isCasting;

        public const int ClassSkillSlotCount = 5;
        public event Action<HeroSkillController> SkillsChanged;

        public HeroActor Owner => owner;

        public async UniTask InitializeUltimateSkillDataAndClassSkillData()
        {
            var prewarmTasks = new List<UniTask>();

            for (int i = 0; i < classSkills.Count; i++)
            {
                var classSkill = classSkills[i];

                if (classSkill == null)
                    continue;

                prewarmTasks.Add(
                    AddressableSkillSpawnService.PrewarmSkillRuntimeAssetsAsync(classSkill));
                
                prewarmTasks.Add(
                    SoundManager.Instance.PreloadSfxAsync(classSkill.GetAllNeedSound())
                );
            }

            prewarmTasks.Add(
                AddressableSkillSpawnService.PrewarmSkillRuntimeAssetsAsync(ultimateSkill)
            );
            
            prewarmTasks.Add(
                SoundManager.Instance.PreloadSfxAsync(ultimateSkill.GetAllNeedSound())
            );

            await UniTask.WhenAll(prewarmTasks);
        }

        public void DespawnAllInstanceOfUltimateSkillAndClassSkill()
        {
            AddressableSkillSpawnService.DisposeSkillComponent(ultimateSkill);
            SoundManager.Instance.ReleaseCachedSfxCollection(ultimateSkill.GetAllNeedSound());

            for (int i = 0; i < classSkills.Count; i++)
            {
                SkillDataSO currentSkill = classSkills[i];
                if (currentSkill == null)
                {
                    continue;
                }

                SoundManager.Instance.ReleaseCachedSfxCollection(currentSkill.GetAllNeedSound());
                AddressableSkillSpawnService.DisposeSkillComponent(currentSkill);
            }
        }
        
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

        public float GetCooldownDuration(SkillDataSO skillData)
        {
            if (skillData == null || owner == null)
                return 0f;

            int level = levelProvider.GetSkillLevel(skillData, owner);
            SkillCastConfig castConfig = skillData.GetCastConfig(level);
            return castConfig != null ? Mathf.Max(0f, castConfig.Cooldown) : 0f;
        }

        public SkillDataSO GetClassSkillAt(int slotIndex)
        {
            EnsureClassSkillSlotCapacity();

            if (slotIndex < 0 || slotIndex >= ClassSkillSlotCount)
                return null;

            return classSkills[slotIndex];
        }

        public List<SkillDataSO> GetAllEquippedClassSkills()
        {
            return classSkills;
        }
        
        public SkillDataSO GetUltimateSkill()
        {
            return ultimateSkill;
        }

        public void SetClassSkillAt(int slotIndex, SkillDataSO skillData)
        {
            EnsureClassSkillSlotCapacity();

            if (slotIndex < 0 || slotIndex >= ClassSkillSlotCount)
            {
                Debug.LogWarning($"[HeroSkillController] Invalid class skill slot index: {slotIndex}", this);
                return;
            }

            classSkills[slotIndex] = skillData;
            SkillsChanged?.Invoke(this);
        }
        
        public bool EquipSkill(SkillDataSO skillData)
        {
            int emptySlotIndex = GetFirstEmptyClassSkillSlot();
            classSkills[emptySlotIndex] = skillData;
            SkillsChanged?.Invoke(this);
            return true;
        }
        
        public bool UnequipSkill(SkillDataSO skillData, bool resetCooldown = false)
        {
            if (skillData == null)
                return false;

            int slotIndex = GetEquippedClassSkillSlot(skillData);
            if (slotIndex < 0)
                return false;

            return UnequipSkillAt(slotIndex, resetCooldown);
        }
        
        public bool UnequipSkillAt(int slotIndex, bool resetCooldown = false)
        {
            EnsureClassSkillSlotCapacity();

            if (!IsValidClassSkillSlot(slotIndex))
                return false;

            SkillDataSO equippedSkill = classSkills[slotIndex];
            if (equippedSkill == null)
                return false;

            classSkills[slotIndex] = null;

            if (resetCooldown)
                ResetCooldown(equippedSkill);

            SkillsChanged?.Invoke(this);
            return true;
        }
        
        public bool ReplaceSkillAt(
            int slotIndex,
            SkillDataSO newSkill,
            bool resetOldSkillCooldown = false)
        {
            EnsureClassSkillSlotCapacity();

            SkillDataSO oldSkill = classSkills[slotIndex];
            
            classSkills[slotIndex] = newSkill;

            ResetCooldown(oldSkill);

            SkillsChanged?.Invoke(this);
            return true;
        }

        private bool CanReplaceSkill(int slotIndex)
        {
            return IsValidClassSkillSlot(slotIndex);
        }
        
        public int GetEquippedClassSkillSlot(SkillDataSO skillData)
        {
            if (skillData == null)
                return -1;

            EnsureClassSkillSlotCapacity();

            for (int i = 0; i < ClassSkillSlotCount; i++)
            {
                if (classSkills[i] == skillData)
                    return i;
            }

            return -1;
        }
        
        public int GetFirstEmptyClassSkillSlot()
        {
            EnsureClassSkillSlotCapacity();

            for (int i = 0; i < ClassSkillSlotCount; i++)
            {
                if (classSkills[i] == null)
                    return i;
            }

            return -1;
        }

        public bool CanCastClassSkillAt(int slotIndex)
        {
            SkillDataSO skillData = GetClassSkillAt(slotIndex);
            return skillData != null && IsCooldownReady(skillData);
        }
        
        public bool CanEquipClassSkill(SkillDataSO skillData, out int slotIndex)
        {
            if (skillData == null)
            {
                slotIndex = -1;
                return false;
            }

            if (IsClassSkillEquipped(skillData))
            {
                slotIndex = -1;
                return false;
            }
            
            int emptySlotIndex = GetFirstEmptyClassSkillSlot();
            if (emptySlotIndex < 0)
            {
                slotIndex = -1;
                return false;
            }
            
            slotIndex = emptySlotIndex;
            return true;
        }
        
        public bool IsClassSkillEquipped(SkillDataSO skillData)
        {
            return GetEquippedClassSkillSlot(skillData) >= 0;
        }


        public bool CanCastUltimate()
        {
            return ultimateSkill != null && IsCooldownReady(ultimateSkill);
        }

        public float GetClassSkillCooldownRemaining(int slotIndex)
        {
            return GetCooldownRemaining(GetClassSkillAt(slotIndex));
        }

        public float GetClassSkillCooldownDuration(int slotIndex)
        {
            return GetCooldownDuration(GetClassSkillAt(slotIndex));
        }

        public float GetUltimateCooldownRemaining()
        {
            return GetCooldownRemaining(ultimateSkill);
        }

        public float GetUltimateCooldownDuration()
        {
            return GetCooldownDuration(ultimateSkill);
        }

        public async UniTask<bool> TryCastClassSkillAtAsync(int slotIndex)
        {
             return await TryCastClassSkillAsync(slotIndex);
        }

        private void Awake()
        {
            if (!autoBindOnAwake)
                return;

            owner = GetComponent<HeroActor>();
            animationDriver = GetComponent<HeroAnimationDriver>();
            ownerSkeletonAnimation =
                GetComponentInChildren<SkeletonAnimation>(true);
            executor = new SkillExecutor(targetResolver, objectSpawner);
            EnsureClassSkillSlotCapacity();
            autoSkillController.Init(this);
        }

        private void Update()
        {
            TickCooldowns(Time.deltaTime);

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
        
        private static bool IsValidClassSkillSlot(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < ClassSkillSlotCount;
        }

        public void Init(HeroActor owner, IHeroBattleContext battleContext, ISkillLevelProvider levelProvider = null)
        {
            this.owner = owner;
            this.battleContext = battleContext;

            if (owner != null)
            {
                animationDriver = owner.Anim;
                ownerSkeletonAnimation =
                    owner.GetComponentInChildren<SkeletonAnimation>(true);
            }

            if (levelProvider != null)
                this.levelProvider = levelProvider;

            executor = new SkillExecutor(targetResolver, objectSpawner);
            EnsureClassSkillSlotCapacity();
            autoSkillController.Init(this);
            BuildPassiveRuntime();
            BindEvents();
        }

        public void SetSkills(List<SkillDataSO> classSkills, SkillDataSO ultimateSkill, SkillDataSO passiveSkill)
        {
            this.classSkills = classSkills ?? new List<SkillDataSO>();
            EnsureClassSkillSlotCapacity();
            this.ultimateSkill = ultimateSkill;
            this.passiveSkill = passiveSkill;
            BuildPassiveRuntime();
            SkillsChanged?.Invoke(this);
        }

        public void SetHeroBoundSkills(SkillDataSO ultimateSkill, SkillDataSO passiveSkill)
        {
            this.ultimateSkill = ultimateSkill;
            this.passiveSkill = passiveSkill;
            BuildPassiveRuntime();
            SkillsChanged?.Invoke(this);
        }

        public void SetClassSkills(List<SkillDataSO> classSkills)
        {
            this.classSkills = classSkills ?? new List<SkillDataSO>();
            EnsureClassSkillSlotCapacity();
            SkillsChanged?.Invoke(this);
        }

        // Kept temporarily so old integration code still compiles while migrating from passive list to single passive.
        public void SetSkills(List<SkillDataSO> classSkills, SkillDataSO ultimateSkill, List<SkillDataSO> passiveSkills)
        {
            SkillDataSO firstPassive = passiveSkills != null && passiveSkills.Count > 0 ? passiveSkills[0] : null;
            SetSkills(classSkills, ultimateSkill, firstPassive);
        }

        private void EnsureClassSkillSlotCapacity()
        {
            if (classSkills == null)
                classSkills = new List<SkillDataSO>(ClassSkillSlotCount);

            while (classSkills.Count < ClassSkillSlotCount)
                classSkills.Add(null);

            if (classSkills.Count > ClassSkillSlotCount)
                classSkills.RemoveRange(ClassSkillSlotCount, classSkills.Count - ClassSkillSlotCount);
        }

        public async UniTask <bool> TryCastClassSkillAsync(int index)
        {
            EnsureClassSkillSlotCapacity();

            if (index < 0 || index >= ClassSkillSlotCount)
                return false;

            return await TryCastSkillAsync(classSkills[index]);
        }

        // ultimate skill cast first input
        public async UniTask<bool> TryCastUltimateAsync()
        {
            bool result = await TryCastSkillAsync(ultimateSkill, true);
            if (result)
            {
                owner.CastingUltimate(true);
            }
            return result;
        }

        public async UniTask<bool> TryCastSkillAsync(SkillDataSO skillData, bool isUltimate = false)
        {
            if (owner.StateMachine.CurrentStateId == HeroStateId.BossSpawn)
                return false;
            
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

            bool result = await CastSkillInternalAsync(skillData, level, target, interrupt: true, isUltimate);
            if (owner.IsChosen && result && isUltimate)
            {
                battleContext?.OnSelectedHeroCastUltimateSkill();
            }
            owner.SetActionLocked(false); 
            return true;
        }

        public void CastSkillImmediately(SkillDataSO skillData, bool isUltimate)
        {
            if (skillData == null)
                return;

            int level = levelProvider.GetSkillLevel(skillData, owner);

            if (!CanCastSkillByCooldown(skillData, level))
                return;

            SkillCastConfig castConfig = skillData.GetCastConfig(level);
            ICombatUnit target = targetResolver.ResolveMainTarget(CreateContext(skillData, level, null), castConfig.TargetSelectType);
            CastSkillInternalAsync(skillData, level, target, interrupt: true, isUltimate);
        }

        public bool TryActivatePassive(
            SkillDataSO triggeredPassiveSkill,
            BuffData buffData)
        {
            SkillDataSO skillToActivate =
                triggeredPassiveSkill != null
                    ? triggeredPassiveSkill
                    : passiveSkill;

            if (skillToActivate == null ||
                owner == null ||
                owner.IsDead)
            {
                return false;
            }

            int level =
                levelProvider.GetSkillLevel(
                    skillToActivate,
                    owner);

            if (!CanCastSkillByCooldown(
                    skillToActivate,
                    level))
            {
                return false;
            }

            if (owner.Stats == null ||
                owner.Stats.BuffModule == null)
            {
                return false;
            }

            // Cooldown chỉ bắt đầu khi buff thực sự có thể apply.
            StartCooldown(skillToActivate, level);

            if (buffData != null)
                owner.Stats.BuffModule.ApplyBuff(buffData);

            PlayPassiveTriggerAnimation(
                skillToActivate,
                level);

            SkillEventBus.Raise(new SkillEventContext
            {
                EventType = SkillTriggerEventType.OnCastSkill,
                Owner = owner,
                Source = owner,
                Target = owner,
                Skill = skillToActivate
            });

            return true;
        }
        
        private void PlayPassiveTriggerAnimation(
            SkillDataSO skillData,
            int level)
        {
            if (skillData == null ||
                animationDriver == null)
            {
                return;
            }

            SkillPassiveConfig passiveConfig =
                skillData.GetPassiveConfig(level);

            if (passiveConfig == null ||
                !passiveConfig.PlayTriggerAnimation ||
                string.IsNullOrEmpty(
                    passiveConfig.TriggerAnimation))
            {
                return;
            }

            animationDriver.PlaySkill(
                passiveConfig.TriggerAnimation);
        }

        public int CountEnemiesInRange(float range)
        {
            if (owner == null)
                return 0;

            ICombatUnit target = owner.HasValidTarget() ? owner.CurrentTarget : null;
            return targetResolver.CountEnemiesInRange(CreateContext(currentSkill, currentSkillLevel, target), owner.Position, range);
        }

        public async UniTask<bool> TryDebugCastClassSkillAsync(int oneBasedIndex)
        {
            return await TryCastClassSkillAsync(oneBasedIndex - 1);
        }

        public async UniTask<bool> TryDebugCastUltimate()
        {
            return await TryCastUltimateAsync();
        }

        public UniTask<bool> TryDebugCastPassive()
        {
            if (passiveSkill == null ||
                passiveRuntime == null)
            {
                return UniTask.FromResult(false);
            }

            int level =
                levelProvider.GetSkillLevel(
                    passiveSkill,
                    owner);

            ResolvedPassiveConfig resolvedConfig =
                passiveSkill.GetResolvedPassiveConfig(level);

            SkillPassiveConfig config =
                resolvedConfig?.BaseConfig;

            if (config == null)
                return UniTask.FromResult(false);

            BuffData debugBuff = new BuffData
            {
                Id = $"passive_{passiveSkill.SkillKey}_{passiveSkill.SkillId}",
                Name = passiveSkill.SkillName,
                Kind = BuffKind.Buff,
                Duration = config.BuffDuration,
                MaxStacks = 1,
                StackRule = BuffStackRule.Replace,
                Modifiers = new List<StatModifier>()
            };

            if (resolvedConfig.Modifiers != null)
            {
                for (int i = 0;
                     i < resolvedConfig.Modifiers.Count;
                     i++)
                {
                    StatModifier modifier =
                        resolvedConfig.Modifiers[i];

                    if (modifier != null)
                        debugBuff.Modifiers.Add(modifier.Clone());
                }
            }

            bool result =
                TryActivatePassive(
                    passiveSkill,
                    debugBuff);

            return UniTask.FromResult(result);
        }


        private void Start()
        {
            GameEventManager.Subscribe<int>(GameEvents.OnStageCleared, OnStageCleared);
            GameEventManager.Subscribe(GameEvents.OnStageLost, OnStageLost);
        }

        private void OnDestroy()
        {
            GameEventManager.Unsubscribe<int>(GameEvents.OnStageCleared, OnStageCleared);
            GameEventManager.Unsubscribe(GameEvents.OnStageLost, OnStageLost);
        }

        public void ResetRuntimeOnSwitchOut()
        {
            passiveRuntime?.Reset();
            ClearPassiveAura();

            CancelCurrentSkill();
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

        private async UniTask<bool> CastSkillInternalAsync(SkillDataSO skillData, int level, ICombatUnit target, bool interrupt, bool isUltimate)
        {
            if (interrupt)
                CancelCurrentSkill();
            
            currentSkill = skillData;
            currentSkillLevel = level;

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
                return true;
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

            bool result = true;
            switch (runtimeType)
            {
                case SkillRuntimeVisualType.SpawnedSkillObject:
                case SkillRuntimeVisualType.SpawnHomingProjectile:
                case SkillRuntimeVisualType.SpawnProjectilePatternBehavior:    
                    result = await SpawnRuntimeObjectSkill(context, runtimeConfig, isUltimate);
                    break;

                case SkillRuntimeVisualType.HeroSpineAndSpawnedSkillObject:
                    result = await CastHeroSpineAndSpawnedSkillObject(context, castConfig, runtimeConfig, isUltimate);
                    break;
                
                case SkillRuntimeVisualType.HeroSpineObjectAndProjectile:
                    result = await CastHeroSpineAndSpawnedProjectile(context, castConfig, runtimeConfig, isUltimate);
                    break;

                case SkillRuntimeVisualType.ProjectileOnly:
                case SkillRuntimeVisualType.Instant:
                    ExecuteManualPhases(context);
                    FinishCurrentSkill(isUltimate);
                    break;

                case SkillRuntimeVisualType.HeroSpineAnimation:
                default:
                    CastHeroSpineSkill(context, castConfig, isUltimate);
                    break;
            }
            return result;
        }

        private async UniTask<bool> SpawnRuntimeObjectSkill(SkillRuntimeContext context, SkillRuntimeObjectConfig runtimeConfig, bool isUltimate)
        {
            bool finishImmediatelyIfIndependent = true;
            var result = await SpawnRuntimeObjectInternal(context, runtimeConfig, finishImmediatelyIfIndependent, isUltimate);
            return result != null;
        }

        private async UniTask<SkillRuntimeObject> SpawnRuntimeObjectInternal(
            SkillRuntimeContext context,
            SkillRuntimeObjectConfig runtimeConfig,
            bool finishImmediatelyIfIndependent, bool isUltimate)
        {
            if (runtimeConfig == null)
            {
                if (finishImmediatelyIfIndependent)
                {
                    ExecuteManualPhases(context);
                    FinishCurrentSkill(isUltimate);
                }

                return null;
            }

            if (runtimeConfig.LockCasterWhileAlive && owner != null)
                owner.SetActionLocked(true);

            Vector3 spawnPosition = GetRuntimeObjectSpawnPosition(context, runtimeConfig);

            SkillRuntimeObject runtimeObject = await objectSpawner.SpawnRuntimeAsync(runtimeConfig, spawnPosition, Quaternion.identity);
            if (runtimeObject == null)
            {
                if (finishImmediatelyIfIndependent)
                    FinishCurrentSkill(isUltimate);

                return null;
            }

            SkillRuntimeContext runtimeContext = context.CloneForRuntimeObject(runtimeObject, spawnPosition);
            runtimeObject.Init(runtimeContext, runtimeConfig, executor, targetResolver, objectSpawner);

            // In phase 1, spawned skill objects are independent once created.
            // Projectile/AOE/skill object will live until its own lifetime/animation end.
            if (finishImmediatelyIfIndependent && !runtimeConfig.LockCasterWhileAlive)
                FinishCurrentSkill(isUltimate);

            return runtimeObject;
        }
        

        private async UniTask<bool> CastHeroSpineAndSpawnedSkillObject(
            SkillRuntimeContext context,
            SkillCastConfig castConfig,
            SkillRuntimeObjectConfig runtimeConfig, bool isUltimate)
        {
            if (context == null)
            {
                FinishCurrentSkill(isUltimate);
                return false;
            }

            // The spawned object is independent. Do not finish the skill here, because the hero
            // must stay locked until the hero ultimate animation completes.
            await SpawnRuntimeObjectInternal(context, runtimeConfig, finishImmediatelyIfIndependent: false, isUltimate);

            bool lockCasterDuringHeroAnimation = runtimeConfig == null || runtimeConfig.LockCasterDuringHeroAnimation;
            if (lockCasterDuringHeroAnimation && owner != null)
            {
                owner.SetActionLocked(true);
                owner.Locomotion?.Stop();
            }

            if (castConfig == null || string.IsNullOrEmpty(castConfig.HeroAnimationName))
            {
                // No hero animation configured. The spawned object has already been created,
                // so release the caster immediately.
                FinishCurrentSkill(isUltimate);
                return false;
            }

            float delay = animationDriver.PlaySkill(castConfig.HeroAnimationName);
            DelayFinishCurrentSkill(delay, isUltimate).Forget();
            return true;
            // FinishCurrentSkill() will be called by OnAnimationComplete() when the hero animation ends.
        }

        private async UniTask DelayFinishCurrentSkill(float delay, bool isUltimate)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delay));
            FinishCurrentSkill(isUltimate);
        }
        
        private async UniTask<bool> CastHeroSpineAndSpawnedProjectile(
            SkillRuntimeContext context,
            SkillCastConfig castConfig,
            SkillRuntimeObjectConfig runtimeConfig, bool isUltimate)
        {
            if (context == null)
            {
                FinishCurrentSkill(isUltimate);
                return false;
            }

            // The spawned object is independent. Do not finish the skill here, because the hero
            // must stay locked until the hero ultimate animation completes.
            await SpawnRuntimeObjectInternal(context, runtimeConfig, finishImmediatelyIfIndependent: false, isUltimate);

            bool lockCasterDuringHeroAnimation = runtimeConfig == null || runtimeConfig.LockCasterDuringHeroAnimation;
            if (lockCasterDuringHeroAnimation && owner != null)
            {
                owner.SetActionLocked(true);
                owner.Locomotion?.Stop();
            }

            if (castConfig == null || string.IsNullOrEmpty(castConfig.HeroAnimationName))
            {
                // No hero animation configured. The spawned object has already been created,
                // so release the caster immediately.
                FinishCurrentSkill(isUltimate);
                return false;
            }

            float delay = animationDriver.PlaySkill(castConfig.HeroAnimationName);
            DelayFinishCurrentSkill(delay, isUltimate).Forget();
            return true;
            // FinishCurrentSkill() will be called by OnAnimationComplete() when the hero animation ends.
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

        private void CastHeroSpineSkill(SkillRuntimeContext context, SkillCastConfig castConfig, bool isUltimate)
        {
            if (owner != null)
            {
                owner.SetActionLocked(true);
            }

            if (!string.IsNullOrEmpty(castConfig.HeroAnimationName))
            {
                float delay = animationDriver.PlaySkill(castConfig.HeroAnimationName);
                DelayFinishCurrentSkill(delay, isUltimate).Forget();
            }
            else
            {
                ExecuteManualPhases(context);
                FinishCurrentSkill(isUltimate);
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
                BattleContext = battleContext,
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
            if (currentSkill == null)
                return;

            // Custom behaviours own their event flow. Do not also execute generic phases here,
            // otherwise special skills such as Bastet Ultimate can double-trigger damage.
            if (currentCustomBehaviour != null)
            {
                currentCustomBehaviour.OnSpineEvent(eventName);
                return;
            }

            SkillPhaseData phase = currentSkill.GetPhaseByEvent(currentSkillLevel, SkillPhaseTriggerType.SpineEvent, eventName);
            if (phase == null)
                return;

            ICombatUnit target = owner != null && owner.HasValidTarget() ? owner.CurrentTarget : null;
            SkillRuntimeContext context = CreateContext(currentSkill, currentSkillLevel, target);
            executor.ExecutePhase(context, phase);
        }

        private void OnAnimationComplete(string animationName)
        {
            // Custom behaviours decide when the skill is actually finished.
            // Example: Bastet Ultimate replays the same animation up to N jumps.
            if (currentCustomBehaviour != null)
            {
                currentCustomBehaviour.OnAnimationComplete(animationName);
                return;
            }

            FinishCurrentSkill(false);
        }

        public bool IsCurrentCustomBehaviour(SkillBehaviour behaviour)
        {
            return behaviour != null && currentCustomBehaviour == behaviour;
        }

        public void ExecuteCustomBehaviourEvent(
            SkillBehaviour behaviour,
            SkillRuntimeContext context,
            SkillPhaseTriggerType triggerType,
            string eventName)
        {
            if (!IsCurrentCustomBehaviour(behaviour))
                return;

            if (context == null || context.SkillData == null)
                return;

            SkillPhaseData phase = context.SkillData.GetPhaseByEvent(context.SkillLevel, triggerType, eventName);
            if (phase == null)
                return;

            executor.ExecutePhase(context, phase);
        }

        public void FinishCustomBehaviour(SkillBehaviour behaviour, bool isUltimate)
        {
            if (!IsCurrentCustomBehaviour(behaviour))
                return;

            FinishCurrentSkill(isUltimate);
        }

        private void FinishCurrentSkill(bool isUltimate)
        {
            SkillBehaviour behaviourToDestroy = currentCustomBehaviour;
            currentSkill = null;
            currentSkillLevel = 0;
            currentCustomBehaviour = null;
            
            if (owner != null)
                owner.SetActionLocked(false);
            
            if(isUltimate)
                owner.CastingUltimate(false);

            if (behaviourToDestroy != null)
                Destroy(behaviourToDestroy.gameObject);
        }

        private void CancelCurrentSkill()
        {
            currentCustomBehaviour?.Cancel();
            if (currentCustomBehaviour != null)
                Destroy(currentCustomBehaviour.gameObject);
            
            currentSkill = null;
            currentSkillLevel = 0;
            currentCustomBehaviour = null;

            if (owner != null)
                owner.SetActionLocked(false);
        }

        private void BuildPassiveRuntime()
        {
            ClearPassiveAura();

            passiveRuntime?.Reset();
            passiveRuntime = null;

            if (passiveSkill == null ||
                owner == null)
            {
                return;
            }

            int level =
                levelProvider.GetSkillLevel(
                    passiveSkill,
                    owner);

            passiveRuntime =
                new PassiveSkillRuntime(
                    this,
                    passiveSkill,
                    level);

            PlayPassiveAura(level);
        }
        
        private void PlayPassiveAura(int level)
        {
            if (passiveSkill == null ||
                ownerSkeletonAnimation == null ||
                ownerSkeletonAnimation.AnimationState == null)
            {
                return;
            }

            SkillPassiveConfig passiveConfig =
                passiveSkill.GetPassiveConfig(level);

            if (passiveConfig == null ||
                string.IsNullOrEmpty(
                    passiveConfig.PassiveAuraAnimation))
            {
                return;
            }

            int trackIndex =
                Mathf.Max(1, passiveConfig.PassiveAuraTrackIndex);

            activePassiveAuraTrack = trackIndex;

            ownerSkeletonAnimation.AnimationState.SetAnimation(
                trackIndex,
                passiveConfig.PassiveAuraAnimation,
                true);
        }

        private void ClearPassiveAura()
        {
            if (ownerSkeletonAnimation == null ||
                ownerSkeletonAnimation.AnimationState == null ||
                activePassiveAuraTrack < 0)
            {
                activePassiveAuraTrack = -1;
                return;
            }

            ownerSkeletonAnimation.AnimationState.ClearTrack(
                activePassiveAuraTrack);

            activePassiveAuraTrack = -1;
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
        
        public void ResetCooldown(SkillDataSO skillData)
        {
            if (skillData == null)
                return;

            cooldownRemainingBySkill.Remove(skillData);
        }

        private void OnStageLost()
        {
            ResetAllCooldowns();
        }
        
        private void OnStageCleared(int _)
        {
            ResetAllCooldowns();
        }
        
        [Button]
        public void ResetAllCooldowns()
        {
            cooldownRemainingBySkill.Clear();
        }
    }
}
