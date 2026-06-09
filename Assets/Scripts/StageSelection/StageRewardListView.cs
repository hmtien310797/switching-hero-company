using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.ItemSystem;
using Immortal_Switch.Scripts.Level.Stage;
using UnityEngine;

namespace Immortal_Switch.Scripts.StageSelection
{
    public class StageRewardListView : MonoBehaviour
    {
        [SerializeField] private Transform contentRoot;
        [SerializeField] private RewardItemView itemPrefab;

        public async UniTask Bind(StageReward[] rewards)
        {
            Clear();

            if (rewards == null || rewards.Length == 0)
                return;

            for (int i = 0; i < rewards.Length; i++)
            {
                StageReward reward = rewards[i];

                if (!reward.IsValid)
                    continue;

                RewardItemView item = Instantiate(itemPrefab, contentRoot);
                Sprite icon = await ItemSystemManager.Instance.Database.GetCurrencyIcon(reward.ResourceType);
                item.Bind(reward, icon);
            }
        }

        private void Clear()
        {
            if (contentRoot == null)
                return;

            for (int i = contentRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(contentRoot.GetChild(i).gameObject);
            }
        }
    }
}