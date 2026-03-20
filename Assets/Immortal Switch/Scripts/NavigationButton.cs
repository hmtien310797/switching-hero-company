using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts
{
    public enum NavState { Closed, Open, Locked }

    public abstract class NavigationButtonBase : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Button button;
        [SerializeField] private GameObject imageHide;    // ON when Closed
        [SerializeField] private GameObject imageLock;    // ON when Locked
        [SerializeField] private GameObject imageRedDot;
        [SerializeField] private TMP_Text textTMP;

        [Header("Id")]
        [SerializeField] private string navId;
        public string NavId => navId;

        public NavState State { get; private set; } = NavState.Closed;

        private bool hasNotification;
        private NavigationManager owner;

        protected virtual void Reset()
        {
            AutoMap();
        }
        

        protected virtual void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            button.onClick.AddListener(HandleClickInternal);
            RefreshVisual();
        }

        protected virtual void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(HandleClickInternal);
        }

        internal void Bind(NavigationManager manager) => owner = manager;

        private void HandleClickInternal()
        {
            if (State == NavState.Locked) return;              // locked -> no click
            if (owner == null) return;

            owner.RequestOpen(this);                           // manager enforces rule
        }

        // ===== Public API used by manager =====

        public void SetNotification(bool value)
        {
            hasNotification = value;
            RefreshVisual();
        }

        internal void SetStateByManager(NavState newState)
        {
            if (State == newState) return;

            var prev = State;
            State = newState;

            if (button != null)
                button.interactable = (State != NavState.Locked);

            RefreshVisual();

            // Hooks (virtual) for derived behaviours
            OnStateChanged(prev, newState);

            if (newState == NavState.Open) OnOpened();
            else if (newState == NavState.Closed) OnClosed();
            else if (newState == NavState.Locked) OnLocked();
        }

        // ===== Hooks for derived classes =====
        protected virtual void OnOpened() { }
        protected virtual void OnClosed() { }
        protected virtual void OnLocked() { }
        protected virtual void OnStateChanged(NavState prev, NavState next) { }

        // ===== Visual =====
        private void RefreshVisual()
        {
            if (imageHide != null) imageHide.SetActive(State == NavState.Closed);
            if (imageLock != null) imageLock.SetActive(State == NavState.Locked);

            // locked => never show red dot
            if (imageRedDot != null)
                imageRedDot.SetActive(State != NavState.Locked && hasNotification);
        }
        
        protected void AutoMap()
        {
            if (button == null)
                button = GetComponent<Button>();
            
            if (imageHide == null) imageHide = FindChildGO("ImageHide");
            if (imageLock == null) imageLock = FindChildGO("ImageLock");
            if (imageRedDot == null) imageRedDot = FindChildGO("ImageRedDot");
            var textGo = FindChildGO("Text");
            if (textGo != null) textTMP = textGo.GetComponent<TMPro.TMP_Text>();
        }

        private GameObject FindChildGO(string childName)
        {
            // Tìm đúng child trực tiếp hoặc sâu hơn (kể cả inactive)
            var t = transform.Find(childName);
            if (t != null) return t.gameObject;

            // Fallback: search deep
            var all = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name == childName)
                    return all[i].gameObject;
            }
            return null;
        }
    }
}