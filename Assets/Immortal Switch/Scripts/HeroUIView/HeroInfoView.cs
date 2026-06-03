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
        [SerializeField] private HeroProgressionDatabaseSO heroDb;
        [SerializeField] private HeroUIIconConfigSO heroUiDb;

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

        private void Awake()
        {
            btnClose.onClick.AddListener(OnClickClose);
        }

        private void OnClickClose()
        {
            UIManager.Instance.TogglePopupAsync<HeroInfoView>().Forget();
        }

        public void Bind(int heroId)
        {
            var hero = heroDb.GetHero(heroId);

            if (hero != null)
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
}