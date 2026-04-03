using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public class SharedSummonSequenceCardUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private TMP_Text gradeText;
        [SerializeField] private GameObject newTag;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform rectTransform;

        [Header("Anim")]
        [SerializeField] private Vector3 hiddenScale = new Vector3(0.72f, 0.72f, 1f);
        [SerializeField] private Vector3 shownScale = Vector3.one;

        public CanvasGroup CanvasGroup => canvasGroup;

        public void Bind(SharedSummonSequenceItemData data)
        {
            if (iconImage != null)
                iconImage.sprite = data != null ? data.Icon : null;

            if (nameText != null)
                nameText.text = data != null ? data.Name : string.Empty;

            if (amountText != null)
                amountText.text = data != null ? data.AmountText : string.Empty;

            if (gradeText != null)
                gradeText.text = data != null ? data.GradeText : string.Empty;

            if (newTag != null)
                newTag.SetActive(data != null && data.IsNew);

            SetHiddenImmediate();
        }

        public void PrepareForReuse()
        {
            if (rectTransform != null)
                rectTransform.localScale = hiddenScale;

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            if (newTag != null)
                newTag.SetActive(false);
        }

        public void SetVisible(bool value)
        {
            gameObject.SetActive(value);
        }

        public void Reveal()
        {
            ShowImmediate();
        }

        public void ShowImmediate()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }

            if (rectTransform != null)
                rectTransform.localScale = shownScale;
        }

        private void SetHiddenImmediate()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            if (rectTransform != null)
                rectTransform.localScale = hiddenScale;
        }
    }
}