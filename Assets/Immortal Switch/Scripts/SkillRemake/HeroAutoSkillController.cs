using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Immortal_Switch.Scripts.Skill
{
    public enum AutoClassSkillOrder
    {
        SlotOrder,
        RoundRobin
    }

    [DisallowMultipleComponent]
    public sealed class HeroAutoSkillController : MonoBehaviour
    {
        [Header("Auto Skill")]
        [SerializeField] private bool autoCastEnabled = true;

        [Header("Skill Types")]
        [SerializeField] private bool autoCastClassSkills = true;
        [SerializeField] private bool autoCastUltimate = true;

        [Header("Priority")]
        [SerializeField] private bool ultimateHasPriority = true;
        [SerializeField] private AutoClassSkillOrder classSkillOrder = AutoClassSkillOrder.SlotOrder;

        [Header("Timing")]
        [SerializeField, Min(0.02f)] private float scanInterval = 0.15f;

        [Tooltip("Nếu true: gặp skill đang ready nhưng cast fail thì dừng lượt scan này. Hữu ích để tránh auto gọi nhiều skill khi hero đang cố MoveToCastRange.")]
        [SerializeField] private bool stopAfterFirstReadyAttempt = true;

        [Header("Debug")]
        [SerializeField] private bool debugLog;
        
        [SerializeField] private HeroActor owner;

        private HeroSkillController skillController;
        private float scanTimer;
        private int nextRoundRobinSlot;

        public bool AutoCastEnabled
        {
            get => autoCastEnabled;
            set => autoCastEnabled = value;
        }

        public bool AutoCastClassSkills
        {
            get => autoCastClassSkills;
            set => autoCastClassSkills = value;
        }

        public bool AutoCastUltimate
        {
            get => autoCastUltimate;
            set => autoCastUltimate = value;
        }

        public void Init(HeroSkillController controller)
        {
            skillController = controller != null ? controller : GetComponent<HeroSkillController>();
            scanTimer = 0f;
            nextRoundRobinSlot = Mathf.Clamp(nextRoundRobinSlot, 0, HeroSkillController.ClassSkillSlotCount - 1);
        }

        private void Awake()
        {
            if (skillController == null)
                skillController = GetComponent<HeroSkillController>();
        }

        private void OnEnable()
        {
            scanTimer = 0f;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        private void Tick(float deltaTime)
        {
            if (!autoCastEnabled || owner.StateMachine.CurrentStateId == HeroStateId.ManualMove || 
                owner.StateMachine.CurrentStateId == HeroStateId.Spawn ||
                owner.IsDead || owner.StateMachine.CurrentStateId == HeroStateId.Dead)
                return;

            scanTimer -= deltaTime;
            if (scanTimer > 0f)
                return;

            scanTimer = scanInterval;
            TryAutoCastNow();
        }

        [ContextMenu("Debug Auto Cast Now")]
        public async UniTask<bool> TryAutoCastNow()
        {
            if (!CanAutoCastNow())
                return false;

            bool result;

            if (autoCastUltimate && ultimateHasPriority)
            {
                result = await TryCastUltimateAsync();
                if (result)
                    return true;
            }

            if (autoCastClassSkills)
            {
                result = await TryCastClassSkill();
                    return true;
            }

            if (autoCastUltimate && !ultimateHasPriority)
            {
                result = await TryCastUltimateAsync();
                if(result)
                    return true;
            }

            return false;
        }

        private bool CanAutoCastNow()
        {
            if (owner == null || owner.IsDead)
                return false;

            if (owner.Stats != null && !owner.Stats.CanCastSkill())
                return false;
            
            return owner.StateMachine.CurrentStateId != HeroStateId.Ultimate;
        }

        private async UniTask<bool> TryCastUltimateAsync()
        {
            if (!skillController.CanCastUltimate())
                return false;

            bool success = await skillController.TryCastUltimateAsync();

            if (debugLog)
            {
                Debug.Log(
                    $"[HeroAutoSkillController] Try Ultimate: {(success ? "OK" : "FAILED")}",
                    this
                );
            }

            return success;
        }

        private async UniTask<bool> TryCastClassSkill()
        {
            return classSkillOrder switch
            {
                AutoClassSkillOrder.RoundRobin => await TryCastClassSkillRoundRobin(),
                _ => await TryCastClassSkillSlotOrder()
            };
        }

        private async UniTask<bool> TryCastClassSkillSlotOrder()
        {
            for (int i = 0; i < HeroSkillController.ClassSkillSlotCount; i++)
            {
                if (!skillController.CanCastClassSkillAt(i))
                    continue;

                bool success = await skillController.TryCastClassSkillAtAsync(i);

                if (debugLog)
                {
                    Debug.Log(
                        $"[HeroAutoSkillController] Try Class Skill Slot {i + 1}: {(success ? "OK" : "FAILED")}",
                        this
                    );
                }

                if (success)
                    return true;

                if (stopAfterFirstReadyAttempt)
                    return false;
            }

            return false;
        }

        private async UniTask<bool> TryCastClassSkillRoundRobin()
        {
            for (int offset = 0; offset < HeroSkillController.ClassSkillSlotCount; offset++)
            {
                int index = (nextRoundRobinSlot + offset) % HeroSkillController.ClassSkillSlotCount;

                if (!skillController.CanCastClassSkillAt(index))
                    continue;

                bool success = await skillController.TryCastClassSkillAtAsync(index);

                if (debugLog)
                {
                    Debug.Log(
                        $"[HeroAutoSkillController] Try Class Skill Slot {index + 1}: {(success ? "OK" : "FAILED")}",
                        this
                    );
                }

                if (success)
                {
                    nextRoundRobinSlot = (index + 1) % HeroSkillController.ClassSkillSlotCount;
                    return true;
                }

                if (stopAfterFirstReadyAttempt)
                    return false;
            }

            return false;
        }
    }
}