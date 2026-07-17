using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Shared.Constants;
using Immortal_Switch.Scripts.Shared.UI;
using Spine.Unity;
using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.Leaderboard.Views.UI
{
    public class UILeaderboardTop : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI txtPlayerName;

        [SerializeField]
        private TextMeshProUGUI txtScore;

        [SerializeField]
        private UILeaderboardReward rewardSlot;

        [SerializeField]
        private SkeletonGraphic heroSkeleton;

        // --- Private Fields ---

        public void Bind(string playerName, int score, int heroId, BigNumber quantity)
        {
            txtPlayerName.text = playerName;
            txtScore.text = $"{score:N0}";

            rewardSlot.Bind(ItemIdConstants.DIAMOND, quantity);
            RefreshSpine(heroId);
        }

        private void RefreshSpine(int heroId)
        {
            /*if (hero.Spine != null)
            {
                // 1. Assign the new skeleton data
                heroSkeleton.skeletonDataAsset = hero.Spine;
                heroSkeleton.material = heroSkeleton.skeletonDataAsset.atlasAssets[0].PrimaryMaterial;

                // 2. Re-initialize the graphic (true forces a full rebuild)
                heroSkeleton.Initialize(true);

                // set animation
                heroSkeleton.AnimationState.ClearTracks();
                heroSkeleton.AnimationState.SetAnimation(0, SpineAnimationNameConstants.IDLE, true);

                // update lại mesh UI
                heroSkeleton.LateUpdate();
            }*/
        }
    }
}