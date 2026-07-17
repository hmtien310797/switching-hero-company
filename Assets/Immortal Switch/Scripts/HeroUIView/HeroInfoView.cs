using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Addressable;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.Constants;
using Immortal_Switch.Scripts.Skill;
using Immortal_Switch.Scripts.UI;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.HeroUIView
{
    public class HeroInfoView : AnimatedUIView
    {
        [Header("Root")] [SerializeField] private Button btnClose;
        [SerializeField] private Button btnUpgrade;
        [SerializeField] private Button btnFormation;
        [SerializeField] private Button btnNext;
        [SerializeField] private Button btnPrev;

        [Header("Database")] [SerializeField] private HeroUIIconConfigSO heroUiDb;
        [SerializeField] private HeroProgressionDatabaseSO heroDatabase;

        [Header("References")] [SerializeField]
        private Image imgShard;

        [SerializeField] private TMP_Text txtName;

        [Header("Progress shard")] [SerializeField]
        private TMP_Text txtShard;

        [SerializeField] private Image imgProgress;

        [Header("Hero spine")] [SerializeField]
        private SkeletonGraphic heroSkeletonGraphic;

        [Header("Star")] [SerializeField] private RectTransform starContainer;
        [SerializeField] private GameObject starTemplate;

        [Header("Race & Element")] [SerializeField]
        private TMP_Text txtRace;

        [SerializeField] private Image imgRace;
        [SerializeField] private TMP_Text txtElement;
        [SerializeField] private TMP_Text txtClass;
        [SerializeField] private TMP_Text txtHeroName;
        [SerializeField] private Image imgElement;
        [SerializeField] private TMP_Text shardText;
        [SerializeField] private Image shardProgress;
        [SerializeField] private Image tierImage;

        [Header("Stats")] [SerializeField] private UIHeroInfoStat statAtk;
        [SerializeField] private UIHeroInfoStat statHp;
        [SerializeField] private UIHeroInfoStat statSpd;

        [SerializeField] private HeroSkillDetailUI ultimateSkillDetailUI;
        [SerializeField] private HeroSkillDetailUI passiveSkillDetailUI;

        [SerializeField] private UIHeroAllSkillDetail uiHeroAllSkillDetail;
        
        private const string HERO_SPRITE_ATLAS_KEY = "hero_sprite_atlas";
        private int _currentHeroIdx;
        private SpriteAtlas _heroSpriteAlas;
        private HeroCollectionItemViewData heroCollectionItemViewData;
        public int heroId { get; private set; }
        private HeroStatSnapshot heroStatSnapshot;

        private void Awake()
        {
            btnClose.onClick.AddListener(OnClickClose);
            btnFormation.onClick.AddListener(OnClickFormation);
            btnNext.onClick.AddListener(OnClickNext);
            btnPrev.onClick.AddListener(OnClickPrev);
            btnUpgrade.onClick.AddListener(UpgradeHero);
        }
        
        public override async UniTask PlayShowAsync(object args)
        {
            if (_heroSpriteAlas == null)
            {
                _heroSpriteAlas = await AddressableSpriteAtlasService.AcquireAtlasAsync(HERO_SPRITE_ATLAS_KEY);
            }

            base.PlayShowAsync(args).Forget();
        }

        public void SetHeroCollectionViewData(HeroCollectionItemViewData heroItemViewData)
        {
            heroCollectionItemViewData = heroItemViewData;
            shardText.text = $"{heroCollectionItemViewData.CurrentShard}/{heroCollectionItemViewData.RequiredShardToNext}";
            shardProgress.fillAmount = heroCollectionItemViewData.ProgressNormalized;
            tierImage.sprite = HeroImageService.GetHeroTierIcon(heroCollectionItemViewData.DisplayTier);
            heroStatSnapshot = HeroProgressionManager.Instance.Service.GetCurrentStats(heroId);
            statAtk.Bind(heroStatSnapshot.Attack);
            statHp.Bind(heroStatSnapshot.Health);
            statSpd.Bind(heroStatSnapshot.AttackSpeed);
        }

        private void OnClickClose()
        {
            UIManager.Instance.TogglePopupAsync<HeroInfoView>().Forget();
        }

        private void OnClickFormation()
        {
            UIManager.Instance.TogglePopupAsync<HeroSwitchPopupView>(_heroSpriteAlas).Forget();
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
            var allHeroes = DatabaseManager.Instance.GetAllHeroData();
            var heroCount = allHeroes.Count;
            _currentHeroIdx = (Mathf.Max(0, _currentHeroIdx + direction)) % heroCount;
            RefreshBtnDirection();

            var hero = allHeroes[_currentHeroIdx];
            heroId = hero.Id;
            RefreshHeroVisual(hero);
        }

        private void UpgradeHero()
        {
            HeroProgressionManager.Instance.UpgradeHero(heroId);
        }

        public void Bind(int heroId)
        {
            var allHeroes = DatabaseManager.Instance.GetAllHeroData();
            _currentHeroIdx = allHeroes.FindIndex(v => v.Id == heroId);
            this.heroId = heroId;

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
            var allHeroes = DatabaseManager.Instance.GetAllHeroData();
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
            var data = HeroCollectionItemViewDataFactory.Build(
                hero,
                heroDatabase,
                HeroProgressionManager.Instance.Service,
                heroUiDb);

            if (data != null)
            {
                txtShard.text = $"{data.CurrentShard} / {data.RequiredShardToNext}";
                imgProgress.fillAmount = data.ProgressNormalized;
                tierImage.sprite = HeroImageService.GetHeroTierIcon(data.DisplayTier);
            }

            if (hero.Spine != null)
            {
                // 1. Assign the new skeleton data
                heroSkeletonGraphic.skeletonDataAsset = hero.Spine;
                heroSkeletonGraphic.material = heroSkeletonGraphic.skeletonDataAsset.atlasAssets[0].PrimaryMaterial;

                // 2. Re-initialize the graphic (true forces a full rebuild)
                heroSkeletonGraphic.Initialize(true);

                // set animation
                heroSkeletonGraphic.AnimationState.ClearTracks();
                heroSkeletonGraphic.AnimationState.SetAnimation(0, SpineAnimationNameConstants.IDLE, true);

                // update lại mesh UI
                heroSkeletonGraphic.LateUpdate();
            }

            var element = heroUiDb.GetElement(hero.Element);
            var @class = heroUiDb.GetHeroClass(hero.HeroClass);

            if (element != null)
            {
                imgElement.sprite = element.Icon;
                txtElement.text = element.ElementName;
            }

            if (@class != null)
            {
                txtClass.text = @class.ClassName;
            }

            imgRace.sprite = HeroImageService.GetHeroClassIcon(hero);
            imgShard.sprite = hero.ShardIcon;
            txtHeroName.text = hero.Name;
            heroStatSnapshot = HeroProgressionManager.Instance.Service.GetCurrentStats(heroId);

            if (heroStatSnapshot != null)
            {
                statAtk.Bind(heroStatSnapshot.Attack);
                statHp.Bind(heroStatSnapshot.Health);
                statSpd.Bind(heroStatSnapshot.AttackSpeed);
            }
            else
            {
                statAtk.Bind(0);
                statHp.Bind(0);
                statSpd.Bind(0);
            }
            
            SkillDataSO ultimateSkillData = DatabaseManager.Instance.GetUltimateSkillDataByHeroId(heroId);
            if (ultimateSkillData != null)
            {
                ultimateSkillDetailUI.Bind(heroStatSnapshot?.ultimateSkillLevel ?? 1,
                    ultimateSkillData, ShowSkillDetail);
            }
            
            SkillDataSO passiveSkillData = DatabaseManager.Instance.GetPassiveSkillDataByHeroId(heroId);
            if (passiveSkillData != null)
            {
                passiveSkillDetailUI.Bind(heroStatSnapshot?.passiveSkillLevel ?? 1,
                    passiveSkillData, ShowSkillDetail);
            }
        }

        private void ShowSkillDetail(SkillDataSO skillDataSo, int currentLevel)
        {
            uiHeroAllSkillDetail.Show(skillDataSo, currentLevel);
        }
    }
}