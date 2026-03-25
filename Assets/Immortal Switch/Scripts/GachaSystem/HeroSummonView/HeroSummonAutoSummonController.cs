using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public class HeroSummonAutoSummonController : MonoBehaviour
    {
        [SerializeField] private Toggle autoSummonToggle;
        [SerializeField] private Toggle skipAnimationToggle;

        public bool IsAutoSummonOn => autoSummonToggle != null && autoSummonToggle.isOn;
        public bool IsSkipAnimationOn => skipAnimationToggle != null && skipAnimationToggle.isOn;
    }
}