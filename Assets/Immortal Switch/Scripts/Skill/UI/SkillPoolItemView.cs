using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Skill.UI
{
    public class SkillPoolItemView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private Image shardFill;
        [SerializeField] private TMP_Text shardText;
        [SerializeField] private GameObject selectedObject;
        [SerializeField] private GameObject equippedObject;
        [SerializeField] private GameObject darkenObject;

        private SkillDataSO skillData;
        private Action<SkillDataSO> clickCallback;

        public void Bind(SkillViewSkillState state, bool selected, Action<SkillDataSO> onClick)
        {
            skillData = state.SkillData;
            clickCallback = onClick;

            icon.sprite = state.SkillData != null ? state.SkillData.SkillIcon : null;
            levelText.text = $"Lv.{state.Level}";
            shardText.text = $"{state.CurrentShard}/{state.RequiredShard}";
            shardFill.fillAmount = state.RequiredShard <= 0 ? 0f : (float)state.CurrentShard / state.RequiredShard;

            if (selectedObject != null)
                selectedObject.SetActive(selected);

            if (equippedObject != null)
                equippedObject.SetActive(state.IsEquipped);

            if (darkenObject != null)
                darkenObject.SetActive(!state.IsOwned);

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => clickCallback?.Invoke(skillData));
        }
    }
}