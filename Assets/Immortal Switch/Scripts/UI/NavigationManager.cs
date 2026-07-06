using System.Collections.Generic;
using UnityEngine;

namespace Immortal_Switch.Scripts.UI
{
    public class NavigationManager : MonoBehaviour
    {
        [SerializeField] private List<NavigationButtonBase> buttons = new();
        [SerializeField] private int openOnStartIndex = -1;

        private void Awake()
        {
            AutoCollectIfEmpty();

            foreach (var b in buttons)
                if (b != null) b.Bind(this);
        }

        private void Start()
        {
            if (openOnStartIndex >= 0 && openOnStartIndex < buttons.Count)
            {
                var b = buttons[openOnStartIndex];
                if (b != null && b.State != NavState.Locked)
                    RequestOpen(b);
            }
        }

        public void RequestOpen(NavigationButtonBase target)
        {
            if (target == null) return;
            if (target.State == NavState.Locked) return;

            // Open target, close others except locked
            foreach (var b in buttons)
            {
                if (b == null) continue;

                if (b == target) b.SetStateByManager(NavState.Open);
                else if (b.State != NavState.Locked) b.SetStateByManager(NavState.Closed);
            }
        }

        public void SetLocked(string id, bool locked)
        {
            var b = FindById(id);
            if (b == null) return;

            b.SetStateByManager(locked ? NavState.Locked : NavState.Closed);
        }

        public void SetRedDot(string id, bool show)
        {
            var b = FindById(id);
            if (b == null) return;

            b.SetNotification(show);
        }

        public void OpenById(string id)
        {
            var b = FindById(id);
            if (b != null) RequestOpen(b);
        }

        private NavigationButtonBase FindById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var b in buttons)
                if (b != null && b.NavId == id) return b;
            return null;
        }

        private void AutoCollectIfEmpty()
        {
            if (buttons != null && buttons.Count > 0) return;
            buttons = new List<NavigationButtonBase>(GetComponentsInChildren<NavigationButtonBase>(true));
        }
    }
}