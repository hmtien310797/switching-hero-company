using Battle;
using Common;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Immortal_Switch.Scripts.Addressable;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.UI.Skill;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Level.Stage;
using Immortal_Switch.Scripts.PlayerSystem.Views;
using Immortal_Switch.Scripts.Reward;
using Immortal_Switch.Scripts.StageSelection;
using Immortal_Switch.Scripts.Tutorial;
using Sirenix.OdinInspector;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.UI
{
    public class TopMainView : UIView
    {
        public static TopMainView Instance;

        [SerializeField] HeroSkillBarUI heroSkillBarUI;
        [SerializeField] private Button switchMainSubHeroButton;
        [SerializeField] private Button moveButton;
        [SerializeField] CurrencyView currencyView;
        [SerializeField] HeroJoystick heroJostick;
        [SerializeField] private Button autoSkillButton;
        [SerializeField] private Button autoSwitchButton;
        [SerializeField] private Button profileBtn;
        [SerializeField] private GameObject rotateObject;
        [SerializeField] private GameObject[] hideAbleObjects;
        [SerializeField] private SkeletonGraphic skeletonGraphic;

        [Header("Hero Icon Switch Animation")] [SerializeField]
        private Image[] iconHeroImage;

        [SerializeField] private RectTransform[] heroIconAnchors;
        [SerializeField] private RectTransform heroIconAnimationRoot;

        [SerializeField, Min(0.01f)] private float heroIconSwitchDuration = 0.25f;

        [SerializeField] private Ease heroIconSwitchEase = Ease.OutCubic;
        private readonly Tween[] heroIconTweens = new Tween[2];
        private bool isHeroIconSwapped;
        private int heroIconSwitchVersion;
        
        [Header("Hero Icon Slot Visual")]
        [SerializeField] private Vector2 firstPlaceAnchoredPosition = Vector2.zero;
        [SerializeField] private Vector2 secondPlaceAnchoredPosition = new Vector2(0f, -60f);

        [SerializeField] private Vector3 firstPlaceScale = Vector3.one;
        [SerializeField] private Vector3 secondPlaceScale = Vector3.one * 0.9f;
        
        public HeroSkillBarUI HeroSkillBarUI => heroSkillBarUI;
        private bool isAutoActived = false;

        private void Awake()
        {
            TutorialManager.Instance.OnResolveTarget += OnResolveTarget;
            TutorialManager.Instance.OnClick += OnClickTutorial;
            Instance = this;

            profileBtn.onClick.AddListener(OnClickProfile);
            switchMainSubHeroButton.onClick.AddListener(OnSwitchMainSubHeroButtonClicked);

            HideAbleObjects();
            skeletonGraphic.gameObject.SetActive(false);
        }
        private UniTask OnClickTutorial(string arg1, int arg2)
        {
            switch (arg2)
            {
                // step 6
                case 6:
                    //dong het cac ui main view
                    UIManager.Instance.CloseTopMain();
                    OnSwitchMainSubHeroButtonClicked();
                    break;

                case 7:
                    OnSwitchMainSubHeroButtonClicked();
                    break;
                
                case 8:
                    OnClickAutoSkill();
                    break;
            }

            return UniTask.CompletedTask;
        }

        private RectTransform OnResolveTarget(string arg1, int arg2)
        {
            switch (arg2)
            {
                // step 6
                case 6:
                case 7:
                    return switchMainSubHeroButton.transform as RectTransform;

                case 8:
                    return autoSkillButton.transform as RectTransform;
                
                case 10:
                    return autoSwitchButton.transform as RectTransform;

                default:
                    return null;
            }
        }
        private void OnClickProfile()
        {
            UIManager.Instance.OpenPopupAsync<ProfileView>().Forget();
        }


        [Button]
        private void OpenIdleFarmingScreen()
        {
            StageRuntimeData stageRuntimeData = PvEBattleController.Instance.GetStageRuntimeData();
            FarmingIdleScreenService.Open(stageRuntimeData);
        }

        [Button]
        private void CloseIdleFarmingScreen()
        {
            FarmingIdleScreenService.Close();
        }

        private void OnClickAutoSkill()
        {
            isAutoActived = !isAutoActived;
            UserDataCache.Instance.SetAutoSkill(isAutoActived);
            rotateObject.transform
                .DOLocalRotate(new Vector3(0, 0, isAutoActived ? 180 : 0), 0.2f, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear).SetLoops(-1, LoopType.Incremental);
        }
        private void Start()
        {
            autoSkillButton.onClick.AddListener(OnClickAutoSkill);
            SetHeroTeamController(HeroTeamController.Instance);
            moveButton.onClick.AddListener(() =>
            {
                UIManager.Instance.TogglePopupAsync<StageSelectionView>(new StageSelectionOpenArgs
                {
                    CurrentStage = PvEBattleController.Instance.CurrentStage,
                    HighestUnlockedStage = PvEBattleController.Instance.HighestUnlockedStage
                }).Forget();
            });
            
            GameEventManager.Subscribe<int>(GameEvents.OnStageCleared, OnStageEnd);
            GameEventManager.Subscribe(GameEvents.OnStageLost, OnStageLost);
            GameEventManager.Subscribe(GameEvents.OnWaveStart, OnStageStart);
            GameEventManager.Subscribe(GameEvents.OnActiveLineupChanged, SetHeroImage);
        }
        
        private void OnDestroy()
        {
            TutorialManager.Instance.OnResolveTarget -= OnResolveTarget;
            TutorialManager.Instance.OnClick -= OnClickTutorial;
            GameEventManager.Unsubscribe<int>(GameEvents.OnStageCleared, OnStageEnd);
            GameEventManager.Unsubscribe(GameEvents.OnStageLost, OnStageLost);
            GameEventManager.Unsubscribe(GameEvents.OnWaveStart, OnStageStart);
            GameEventManager.Unsubscribe(GameEvents.OnActiveLineupChanged, SetHeroImage);
            for (int i = 0; i < heroIconTweens.Length; i++)
            {
                heroIconTweens[i]?.Kill(false);
                heroIconTweens[i] = null;
            }

            if (Instance == this)
            {
                Instance = null;
            }

            if (switchMainSubHeroButton != null)
            {
                switchMainSubHeroButton.onClick.RemoveListener(
                    OnSwitchMainSubHeroButtonClicked);
            }
        }

        public void SetHeroSkeletonAnimationGraphic(HeroDataSO heroData)
        {
            if (heroData == null ||
                heroData.SkeletonDataAsset == null ||
                skeletonGraphic == null)
            {
                return;
            }

            skeletonGraphic.startingAnimation = string.Empty;
            skeletonGraphic.startingLoop = false;

            skeletonGraphic.skeletonDataAsset = heroData.SkeletonDataAsset;
            skeletonGraphic.Initialize(true);

            skeletonGraphic.AnimationState.ClearTracks();
            skeletonGraphic.Skeleton.SetToSetupPose();

            skeletonGraphic.gameObject.SetActive(false);
        }

        public void PlayHeroSkeletonAnimation()
        {
            if (skeletonGraphic == null)
            {
                return;
            }

            skeletonGraphic.gameObject.SetActive(true);
            skeletonGraphic.AnimationState.ClearTracks();
            skeletonGraphic.AnimationState.SetAnimation(0, "animation", false);
        }

        public void SetHeroImage()
        {
            for (int i = 0; i < UserDataCache.Instance.inBattleHeroes.Length; i++)
            {
                HeroActor currentHero = UserDataCache.Instance.inBattleHeroes[i];
                if (currentHero == null)
                {
                    continue;
                }
                HeroDataSO heroDataSo = currentHero.HeroData;
                iconHeroImage[i].sprite = HeroImageService.GetHeroIcon(heroDataSo);
            }
        }

        private void OnSwitchMainSubHeroButtonClicked()
        {
            // Chuyển Hero gameplay ngay lập tức.
            // Animation icon hoàn toàn không block logic này.
            HeroDataSO currentHeroData =
                PvEBattleController.Instance?.OnSwitchMainSubHeroButtonClicked();

            SetHeroSkeletonAnimationGraphic(currentHeroData);

            SwitchHeroIconVisual();
        }

        private void SwitchHeroIconVisual()
        {
            if (!CanSwitchHeroIcon())
            {
                return;
            }

            isHeroIconSwapped = !isHeroIconSwapped;
            heroIconSwitchVersion++;

            int currentVersion = heroIconSwitchVersion;

            RectTransform iconHero1 = iconHeroImage[0].rectTransform;
            RectTransform iconHero2 = iconHeroImage[1].rectTransform;

            RectTransform hero1TargetAnchor = isHeroIconSwapped
                ? heroIconAnchors[1]
                : heroIconAnchors[0];

            RectTransform hero2TargetAnchor = isHeroIconSwapped
                ? heroIconAnchors[0]
                : heroIconAnchors[1];

            MoveHeroIcon(
                iconIndex: 0,
                iconRect: iconHero1,
                targetAnchor: hero1TargetAnchor,
                switchVersion: currentVersion);

            MoveHeroIcon(
                iconIndex: 1,
                iconRect: iconHero2,
                targetAnchor: hero2TargetAnchor,
                switchVersion: currentVersion);
        }

        private void MoveHeroIcon(
            int iconIndex,
            RectTransform iconRect,
            RectTransform targetAnchor,
            int switchVersion)
        {
            if (iconRect == null || targetAnchor == null)
            {
                return;
            }

            heroIconTweens[iconIndex]?.Kill(false);
            heroIconTweens[iconIndex] = null;

            /*
             * Đây là vị trí thật của anchor trong world space.
             * Không tự tính từ anchoredPosition nữa.
             */
            Vector3 targetWorldPosition = targetAnchor.position;

            /*
             * Scale cuối cùng của icon khi nằm bên trong targetAnchor.
             *
             * Icon luôn có localScale = 1.
             * Scale 0.9 đã nằm trên HeroIconAnchor2.
             */
            Vector3 targetWorldScale = targetAnchor.lossyScale;

            Vector3 targetLocalScaleInCurrentParent =
                ConvertWorldScaleToLocalScale(
                    iconRect.parent,
                    targetWorldScale);

            Sequence sequence = DOTween.Sequence();

            sequence.Join(
                iconRect
                    .DOMove(targetWorldPosition, heroIconSwitchDuration)
                    .SetEase(Ease.Linear));

            sequence.Join(
                iconRect
                    .DOScale(
                        targetLocalScaleInCurrentParent,
                        heroIconSwitchDuration)
                    .SetEase(Ease.Linear));

            sequence
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    if (switchVersion != heroIconSwitchVersion)
                    {
                        return;
                    }

                    AttachIconToAnchor(iconRect, targetAnchor);

                    heroIconTweens[iconIndex] = null;
                });

            heroIconTweens[iconIndex] = sequence;
        }
        
        private static Vector3 ConvertWorldScaleToLocalScale(
            Transform parent,
            Vector3 desiredWorldScale)
        {
            if (parent == null)
            {
                return desiredWorldScale;
            }

            Vector3 parentWorldScale = parent.lossyScale;

            return new Vector3(
                SafeDivide(desiredWorldScale.x, parentWorldScale.x),
                SafeDivide(desiredWorldScale.y, parentWorldScale.y),
                SafeDivide(desiredWorldScale.z, parentWorldScale.z));
        }
        
        
        private static void AttachIconToAnchor(
            RectTransform iconRect,
            RectTransform targetAnchor)
        {
            /*
             * worldPositionStays = false vì targetAnchor chính là
             * vị trí cuối cùng mà icon vừa tween đến.
             */
            iconRect.SetParent(targetAnchor, false);

            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);

            iconRect.anchoredPosition = Vector2.zero;
            iconRect.localRotation = Quaternion.identity;
            iconRect.localScale = Vector3.one;
        }
        

        private bool CanSwitchHeroIcon()
        {
            return iconHeroImage != null &&
                   iconHeroImage.Length >= 2 &&
                   iconHeroImage[0] != null &&
                   iconHeroImage[1] != null &&
                   heroIconAnchors != null &&
                   heroIconAnchors.Length >= 2 &&
                   heroIconAnchors[0] != null &&
                   heroIconAnchors[1] != null;
        }

        private static float SafeDivide(float value, float divisor)
        {
            return Mathf.Approximately(divisor, 0f)
                ? value
                : value / divisor;
        }
        

        private void SetHeroTeamController(HeroTeamController heroTeamController)
        {
            heroJostick.SetTarget(heroTeamController);
        }

        public CurrencyView CurrencyView => currencyView;

        private void OnStageLost()
        {
            HideAbleObjects();
        }

        private void OnStageEnd(int _)
        {
            HideAbleObjects();
        }

        private void HideAbleObjects()
        {
            for (int i = 0; i < hideAbleObjects.Length; i++)
            {
                hideAbleObjects[i].SetActive(false);
            }
        }

        private void OnStageStart()
        {
            for (int i = 0; i < hideAbleObjects.Length; i++)
            {
                hideAbleObjects[i].SetActive(true);
            }
        }
    }
}