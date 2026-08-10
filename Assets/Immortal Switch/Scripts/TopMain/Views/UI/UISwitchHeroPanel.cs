using System;
using Common;
using Immortal_Switch.Scripts.Addressable;
using Immortal_Switch.Scripts.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.TopMain.Views.UI
{
    public class UISwitchHeroPanel : MonoBehaviour
    {
        [Header("Hero icon references")]
        [SerializeField]
        private GameObject goBgBottom;

        [SerializeField]
        private GameObject goBgTop;

        [SerializeField]
        private GameObject hero1SelectStatus;

        [SerializeField]
        private GameObject hero1NormalStatus;

        [SerializeField]
        private GameObject hero2SelectStatus;

        [SerializeField]
        private GameObject hero2NormalStatus;

        [SerializeField]
        private Image[] hero1Icons;

        [SerializeField]
        private Image[] hero2Icons;

        [SerializeField]
        private Image class1Icon;

        [SerializeField]
        private Image class2Icon;

        [SerializeField]
        private Button btnSwitch;

        // --- Public Fields ---
        public RectTransform BtnSwitch => btnSwitch.transform as RectTransform;

        // --- Private Fields ---
        private Action _onSwitchHero;

        private int _switchCount;

        private void Awake()
        {
            btnSwitch.onClick.AddListener(OnClickSwitch);
            UserDataCache.Instance.OnBattleLineupChanged += OnActiveLineupChanged;
            GameEventManager.Subscribe(GameEvents.OnActiveLineupChanged, OnActiveLineupChanged);
        }

        private void OnClickSwitch()
        {
            _onSwitchHero?.Invoke();

            _switchCount++;

            RefreshBg();
        }

        /// <summary>
        /// phải gọi ở OnEnable, flow hiện tại thì các event như OnBattleLineupChanged, OnActiveLineupChanged được gọi trước khi UI đc tạo ra
        /// </summary>
        private void OnEnable()
        {
            OnActiveLineupChanged();
            RefreshBg();
        }

        private void OnDestroy()
        {
            btnSwitch.onClick.RemoveListener(OnClickSwitch);
            GameEventManager.Unsubscribe(GameEvents.OnActiveLineupChanged, OnActiveLineupChanged);

            if (UserDataCache.Instance != null)
            {
                UserDataCache.Instance.OnBattleLineupChanged -= OnActiveLineupChanged;
            }
        }

        public void SetInteractable(bool active)
        {
            btnSwitch.interactable = active;
        }

        public void Bind(
            Action onSwitchHero
        )
        {
            _onSwitchHero = onSwitchHero;
        }

        private void OnActiveLineupChanged()
        {
            var heroes = UserDataCache.Instance.inBattleHeroes;

            if (heroes.Length > 0)
            {
                var hero = heroes[0];

                if (hero != null)
                {
                    var so = hero.HeroData;
                    var icHero = HeroImageService.GetHeroIcon(so);
                    var icClass = HeroImageService.GetHeroClassIcon(so);

                    SetIcon(icHero, hero1Icons);
                    SetIcon(icClass, class1Icon);
                }
            }

            if (heroes.Length > 1)
            {
                var hero = heroes[1];

                if (hero != null)
                {
                    var so = hero.HeroData;
                    var icHero = HeroImageService.GetHeroIcon(so);
                    var icClass = HeroImageService.GetHeroClassIcon(so);

                    SetIcon(icHero, hero2Icons);
                    SetIcon(icClass, class2Icon);
                }
            }
        }

        private void RefreshBg()
        {
            var isHero1Selected = _switchCount % 2 == 0;

            if (isHero1Selected)
            {
                goBgTop.SetActive(true);
                goBgBottom.SetActive(false);

                hero1SelectStatus.SetActive(true);
                hero1NormalStatus.SetActive(false);

                hero2SelectStatus.SetActive(false);
                hero2NormalStatus.SetActive(true);

                SetActive(true, class1Icon);
                SetActive(false, class2Icon);
            }
            else
            {
                goBgTop.SetActive(false);
                goBgBottom.SetActive(true);

                hero1SelectStatus.SetActive(false);
                hero1NormalStatus.SetActive(true);

                hero2SelectStatus.SetActive(true);
                hero2NormalStatus.SetActive(false);

                SetActive(false, class1Icon);
                SetActive(true, class2Icon);
            }
        }

        private void SetActive(bool active, params Image[] images)
        {
            foreach (var img in images)
            {
                if (img != null)
                {
                    img.gameObject.SetActive(active);
                }
            }
        }

        private void SetIcon(Sprite icon, params Image[] images)
        {
            foreach (var img in images)
            {
                if (img != null)
                {
                    img.sprite = icon;
                }
            }
        }
    }
}