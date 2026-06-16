using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.HeroUIView
{
    public class HeroInfoView : AnimatedUIView
    {
        [Header("Root")] [SerializeField] private Button btnClose;
        [SerializeField] private Button btnFormation;
        [SerializeField] private Button btnNext;
        [SerializeField] private Button btnPrev;

        [Header("Database")] [SerializeField] private HeroUIIconConfigSO heroUiDb;

        [Header("References")] [SerializeField]
        private Image imgShard;

        [SerializeField] private TMP_Text txtName;

        [Header("Progress shard")] [SerializeField]
        private TMP_Text txtShard;

        [SerializeField] private Image imgProgress;

        [Header("Star")] [SerializeField] private RectTransform starContainer;
        [SerializeField] private GameObject starTemplate;

        [Header("Race & Element")] [SerializeField]
        private TMP_Text txtRace;

        [SerializeField] private Image imgRace;
        [SerializeField] private TMP_Text txtElement;
        [SerializeField] private Image imgElement;

        [Header("Stats")] [SerializeField] private UIHeroInfoStat statAtk;
        [SerializeField] private UIHeroInfoStat statHp;
        [SerializeField] private UIHeroInfoStat statSpd;

        // --- Private Fields ---
        private int _currentHeroIdx;

        private void Awake()
        {
            btnClose.onClick.AddListener(OnClickClose);
            btnFormation.onClick.AddListener(OnClickFormation);
            btnNext.onClick.AddListener(OnClickNext);
            btnPrev.onClick.AddListener(OnClickPrev);
        }

        private void OnClickClose()
        {
            UIManager.Instance.TogglePopupAsync<HeroInfoView>().Forget();
        }

        private void OnClickFormation()
        {
            UIManager.Instance.TogglePopupAsync<HeroSwitchPopupView>().Forget();
        }

        private void OnClickNext()
        {
            OnChangeHero(1);
        }

        private void OnClickPrev()
        {
            OnChangeHero(-1);
        }

        private void OnChangeHero(int direction)
        {
            var allHeroes = MasterDataCache.Instance.GetAllHeroData();
            var heroCount = allHeroes.Count;
            _currentHeroIdx = (Mathf.Max(0, _currentHeroIdx + direction)) % heroCount;
            RefreshBtnDirection();

            var hero = allHeroes[_currentHeroIdx];
            RefreshHeroVisual(hero);
        }

        public void Bind(int heroId)
        {
            var allHeroes = MasterDataCache.Instance.GetAllHeroData();
            _currentHeroIdx = allHeroes.FindIndex(v => v.Id == heroId);

            if (_currentHeroIdx < 0)
            {
                Debug.LogError($"Hero {heroId} not found");
                return;
            }

            var hero = allHeroes[_currentHeroIdx];

            if (hero != null)
            {
                RefreshHeroVisual(hero);
            }

            RefreshBtnDirection();
        }

        private void RefreshBtnDirection()
        {
            var allHeroes = MasterDataCache.Instance.GetAllHeroData();
            var heroCount = allHeroes.Count;

            if (_currentHeroIdx >= heroCount - 1)
            {
                btnNext.gameObject.SetActive(false);
                btnPrev.gameObject.SetActive(true);
            }
            else if (_currentHeroIdx <= 0)
            {
                btnNext.gameObject.SetActive(true);
                btnPrev.gameObject.SetActive(false);
            }
            else
            {
                btnNext.gameObject.SetActive(true);
                btnPrev.gameObject.SetActive(true);
            }
        }

        private void RefreshHeroVisual(HeroDataSO hero)
        {
            var elementIcon = heroUiDb.GetElementIcon(hero.Element);
            var classIcon = heroUiDb.GetHeroClassIcon(hero.HeroClass);

            if (elementIcon != null)
            {
                imgElement.sprite = elementIcon;
            }

            if (classIcon != null)
            {
                imgRace.sprite = classIcon;
            }

            imgShard.sprite = hero.ShardIcon;
            statAtk.Bind(hero.Attack);
            statHp.Bind(hero.Health);
            statSpd.Bind(hero.AttackSpeed);
        }
    }
}