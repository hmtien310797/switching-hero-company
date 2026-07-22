using System;
using Immortal_Switch.Scripts.Event.EventLeHoiBangLong.Controller;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Event.EventLeHoiBangLong.Layout
{
    public class EventLeHoiBangLongTopLayout : MonoBehaviour
    {
        [Header("Button references")]
        [SerializeField]
        private Button btnBack;

        [SerializeField]
        private Button btnHome;

        [SerializeField]
        private Button btnHelp;

        // --- Private Fields ---
        private Action<EEventLeHoiBangLongLayoutType> _onChangeLayout;
        private Action _onClose;
        private Action _onHelp;

        private void Awake()
        {
            btnBack.onClick.AddListener(OnClickBack);
            btnHome.onClick.AddListener(OnClickHome);
            btnHelp.onClick.AddListener(OnClickHelp);
        }

        private void OnDestroy()
        {
            btnBack.onClick.RemoveListener(OnClickBack);
            btnHome.onClick.RemoveListener(OnClickHome);
            btnHelp.onClick.RemoveListener(OnClickHelp);
        }

        private void OnClickHelp()
        {
            _onHelp?.Invoke();
        }

        private void OnClickHome()
        {
            _onClose?.Invoke();
        }

        private void OnClickBack()
        {
            _onChangeLayout?.Invoke(EEventLeHoiBangLongLayoutType.Main);
        }

        public void Bind(Action<EEventLeHoiBangLongLayoutType> onChangeLayout, Action onClose, Action onHelp)
        {
            _onChangeLayout = onChangeLayout;
            _onClose = onClose;
            _onHelp = onHelp;
        }

        public void SetEnableBack(bool active)
        {
            btnBack.gameObject.SetActive(active);
        }

        public void SetEnableHelp(bool active)
        {
            btnHelp.gameObject.SetActive(active);
        }
    }
}