using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public class SummonCategoryButtonView : MonoBehaviour
    {
        [SerializeField] private SummonCategory category;
        [SerializeField] private Button button;
        [SerializeField] private GameObject selectedObject;
        [SerializeField] private GameObject redDotObject;
        [SerializeField] private CanvasGroup canvasGroup;

        public SummonCategory Category => category;
        public Button Button => button;

        public void SetSelected(bool value)
        {
            if (selectedObject != null)
                selectedObject.SetActive(value);
        }

        public void SetRedDot(bool value)
        {
            if (redDotObject != null)
                redDotObject.SetActive(value);
        }

        public void SetInteractable(bool value)
        {
            if (button != null)
                button.interactable = value;

            if (canvasGroup != null)
            {
                canvasGroup.interactable = value;
                canvasGroup.blocksRaycasts = value;
                canvasGroup.alpha = value ? 1f : 0.7f;
            }
        }
    }
}