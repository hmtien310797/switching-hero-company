using Immortal_Switch.Scripts.Event.EventWheel.Controller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Event.EventWheel.Layout
{
    public class EventLayout : MonoBehaviour
    {
        [Header("Header references")]
        [SerializeField]
        private TextMeshProUGUI txtTitle;

        [SerializeField]
        private TextMeshProUGUI txtCountdown;

        [SerializeField]
        private Toggle toggleSkipAnimation;

        [Header("References")]
        [SerializeField]
        private Button btnNormal;

        [SerializeField]
        private Button btnPremium;

        [SerializeField]
        private Button btnX1;

        [SerializeField]
        private Button btnX10;

        [SerializeField]
        private TextMeshProUGUI txtX1;

        [SerializeField]
        private TextMeshProUGUI txtX10;

        [Header("Wheel references")]
        [SerializeField]
        private WheelController normal;

        [SerializeField]
        private WheelController premium;

        // --- Private Fields ---
        private bool _isPremium;

        private int _normalX1;
        private int _normalX10;

        private int _premiumX1;
        private int _premiumX10;

        private void Awake()
        {
            btnNormal.onClick.AddListener(OnClickNormal);
            btnPremium.onClick.AddListener(OnClickPremium);

            btnX1.onClick.AddListener(OnClickX1);
            btnX10.onClick.AddListener(OnClickX10);
        }

        private void OnEnable()
        {
            OnClickNormal();
        }

        private void OnClickNormal()
        {
            _isPremium = false;
            txtTitle.text = "Vòng quay cơ bản";

            normal.gameObject.SetActive(true);
            premium.gameObject.SetActive(false);
        }

        private void OnClickPremium()
        {
            _isPremium = true;
            txtTitle.text = "Vòng quay cao cấp";

            normal.gameObject.SetActive(false);
            premium.gameObject.SetActive(true);
        }

        public void Bind(int normalX1, int normalX10, int premiumX1, int premiumX10)
        {
            _normalX1 = normalX1;
            _normalX10 = normalX10;

            _premiumX1 = premiumX1;
            _premiumX10 = premiumX10;

            RefreshPrice();
        }

        private void OnClickX1()
        {
            StartSpin();
        }

        private void OnClickX10()
        {
            StartSpin();
        }

        private void StartSpin()
        {
            if (_isPremium)
            {
                premium.StartSpin();
            }
            else
            {
                normal.StartSpin();
            }
        }

        private void RefreshPrice()
        {
            if (_isPremium)
            {
                txtX1.text = _premiumX1.ToString();
                txtX10.text = _premiumX10.ToString();
            }
            else
            {
                txtX1.text = _normalX1.ToString();
                txtX10.text = _normalX10.ToString();
            }
        }
    }
}