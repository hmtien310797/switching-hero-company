using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Level.Stage;
using StageSelection.UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.StageSelection
{
    public class StageRewardListView : MonoBehaviour
    {
        [SerializeField]
        private Transform contentRoot;

        [SerializeField]
        private UIChapterStageBaseReward itemPrefab;

        // --- Private Fields ---
        private List<UIChapterStageBaseReward> _rewards = new();

        public void Bind(StageReward[] rewards)
        {
            if (rewards == null ||
                rewards.Length == 0)
                return;

            for (int i = 0; i < rewards.Length; i++)
            {
                StageReward reward = rewards[i];

                if (!reward.IsValid)
                    continue;

                if (_rewards.Count > i)
                {
                    var clone = _rewards[i];
                    clone.Bind(reward);
                }
                else
                {
                    var clone = Instantiate(itemPrefab, contentRoot);
                    clone.Bind(reward);
                    _rewards.Add(clone);
                }
            }

            for (int i = rewards.Length; i < _rewards.Count; i++)
            {
                _rewards[i].gameObject.SetActive(false);
            }
        }
    }
}