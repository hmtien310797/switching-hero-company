using UnityEngine;

namespace Scripts.Battle
{
    public class HeroBehaviorParams : MonoBehaviour
    {
        [SerializeField] float distFlashConst = 5f;
        [SerializeField] float distSkillRange = 5f;
        [SerializeField] float intervalSwitch = 20f;
        [SerializeField] float switchArea = 5f;
        [SerializeField] readonly Vector3 heroSpawnPosition = new Vector3(0f, 0f, 12f);
        [SerializeField] bool isMain = false;
        [SerializeField] float passiveTime = 5f;

        private int heroId = -1;
        private bool isInSkillAction = false;
        private bool isInSwitchAction = false;
        private bool isInFlashAction = false;
        private bool isValid = false;
        private bool isPriorityNearTarget = false;

        public float DistFlashConst { get => distFlashConst; }
        public float DistSkillRange { get => distSkillRange; }
        public float IntervalSwitch { get => intervalSwitch; }
        public float SwitchArea { get => switchArea; }
        public Vector3 HeroSpawnPosition { get => heroSpawnPosition; }
        public bool IsInSkillAction { get => isInSkillAction; set => isInSkillAction = value; }
        public bool IsInSwitchAction { get => isInSwitchAction; set => isInSwitchAction = value; }
        public bool IsInFlashAction { get => isInFlashAction; set => isInFlashAction = value; }
        public bool IsValid { get => isValid; set => isValid = value; }
        public bool IsPriorityNearTarget { get => isPriorityNearTarget; set => isPriorityNearTarget = value; }
        public int HeroId { get => heroId; set => heroId = value; }
    }
}
