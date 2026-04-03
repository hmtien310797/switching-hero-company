using UnityEngine;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public abstract class BaseSummonPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject root;

        public abstract SummonCategory Category { get; }

        public virtual void ShowPanel()
        {
            SetVisible(true);
            OnShowPanel();
        }

        public virtual void HidePanel()
        {
            OnHidePanel();
            SetVisible(false);
        }

        public virtual void RefreshView()
        {
        }

        public virtual bool HasNotification()
        {
            return false;
        }

        protected virtual void OnShowPanel()
        {
        }

        protected virtual void OnHidePanel()
        {
        }

        protected void SetVisible(bool visible)
        {
            if (root != null)
                root.SetActive(visible);
            else
                gameObject.SetActive(visible);
        }
    }
}