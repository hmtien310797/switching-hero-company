using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Skill.UI
{
    public class SkillItemView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private GameObject selectedObject;
        [SerializeField] private GameObject equippedTagObject;
        [SerializeField] private GameObject darkenObject;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text shardText;
        [SerializeField] private Image shardProgress;

        private SkillDataSO skillData;
        private Action<SkillDataSO> clickCallback;

        public Button Button => button;
        public int SkillId => skillData != null ? skillData.SkillId : -1;

        public void Setup(SkillViewSkillState state, bool isSelected, Action<SkillDataSO> onClick)
        {
            skillData = state.SkillData;
            clickCallback = onClick;

            if (icon != null)
                icon.sprite = state.SkillData != null ? state.SkillData.SkillIcon : null;

            if (levelText != null)
                levelText.text = $"Lv.{state.Level}";

            if (shardText != null)
                shardText.text = $"{state.CurrentShard}/{state.RequiredShard}";

            if (shardProgress != null)
                shardProgress.fillAmount = state.RequiredShard > 0 ? (float)state.CurrentShard / state.RequiredShard : 0f;

            if (equippedTagObject != null)
                equippedTagObject.SetActive(state.IsEquipped);

            if (darkenObject != null)
                darkenObject.SetActive(!state.IsOwned);

            if (selectedObject != null)
                selectedObject.SetActive(isSelected);

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => clickCallback?.Invoke(skillData));
        }
    }
}