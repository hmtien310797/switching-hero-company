using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Equipment.UIRuntime
{
    public class UIWeaponStarDisplay : MonoBehaviour
    {
        [SerializeField] private List<Image> starImages;
        [SerializeField] private WeaponStarVisualConfigSO visualConfig;

        public void BindStandard(int star)
        {
            star = Mathf.Clamp(star, 0, 5);

            for (int i = 0; i < starImages.Count; i++)
            {
                if (starImages[i] == null) continue;
                starImages[i].sprite = i < star ? visualConfig.YellowStar : visualConfig.EmptyStar;
            }
        }

        public void BindExclusive(int currentStar)
        {
            int filledCount = Mathf.Clamp(((currentStar - 1) % 5) + 1, 0, 5);
            Sprite filledSprite = GetExclusiveStarSprite(currentStar);

            for (int i = 0; i < starImages.Count; i++)
            {
                if (starImages[i] == null) continue;
                starImages[i].sprite = i < filledCount ? filledSprite : visualConfig.EmptyStar;
            }
        }

        private Sprite GetExclusiveStarSprite(int currentStar)
        {
            if (currentStar <= 5)
                return visualConfig.YellowStar;

            if (currentStar <= 10)
                return visualConfig.RedStar;

            return visualConfig.PurpleStar;
        }
    }
}