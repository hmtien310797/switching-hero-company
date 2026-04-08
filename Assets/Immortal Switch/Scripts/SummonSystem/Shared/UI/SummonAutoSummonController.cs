using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.SummonSystem.Shared.UI
{
    public class SummonAutoSummonController : MonoBehaviour
    {
        [SerializeField] private Toggle autoSummonToggle;
        [SerializeField] private Toggle skipAnimationToggle;

        public bool IsAutoSummonOn => autoSummonToggle != null && autoSummonToggle.isOn;
        public bool IsSkipAnimationOn => skipAnimationToggle != null && skipAnimationToggle.isOn;
    }
}