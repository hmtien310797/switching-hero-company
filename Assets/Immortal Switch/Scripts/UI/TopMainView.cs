using System;
using Battle;
using Common;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Immortal_Switch.Scripts.Addressable;
using Immortal_Switch.Scripts.AFKReward.Views;
using Immortal_Switch.Scripts.Bag.Views;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Event.Views;
using Immortal_Switch.Scripts.GameSetting.Views;
using Immortal_Switch.Scripts.UI.Skill;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Items;
using Immortal_Switch.Scripts.Level.Stage;
using Immortal_Switch.Scripts.PlayerSystem.Views;
using Immortal_Switch.Scripts.Reward;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shop.Views;
using Immortal_Switch.Scripts.StageSelection;
using Immortal_Switch.Scripts.Tutorial;
using Sirenix.OdinInspector;
using Spine.Unity;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.UI
{
    public class TopMainView : UIView
    {
        public static TopMainView Instance;

        [SerializeField]
        HeroSkillBarUI heroSkillBarUI;

        [SerializeField]
        private Button switchMainSubHeroButton;

        [SerializeField]
        private Button moveButton;

        [SerializeField]
        private Button btnBag;

        [SerializeField]
        private Button btnShop;

        [SerializeField]
        private Button btnEvent;

        [SerializeField]
        CurrencyView currencyView;

        [SerializeField]
        HeroJoystick heroJostick;

        [SerializeField]
        private Button autoSkillButton;

        [SerializeField]
        private Button autoSwitchButton;

        [SerializeField]
        private Button profileBtn;

        [Header("Player references")]
        [SerializeField]
        private TMP_Text txtPlayerName;

        [SerializeField]
        private TMP_Text txtPlayerLevel;

        [SerializeField]
        private Image imgPlayerProgress;

        [SerializeField]
        private GameObject rotateObject;

        [SerializeField]
        private GameObject[] hideAbleObjects;

        [SerializeField]
        private SkeletonGraphic skeletonGraphic;

        [SerializeField]
        private Button btnActiveFramingClaim;

        [SerializeField]
        private RewardSyncService rewardSyncService;

        [SerializeField]
        private GameObject[] disableObjectsWhenPlayDungeon;

        [SerializeField]
        private GameObject[] enableObjectsWhenPlayDungeon;

        [Header("Hero Icon Switch Animation")]
        [SerializeField]
        private Image[] iconHeroImage;

        [SerializeField]
        private RectTransform[] heroIconAnchors;

        [SerializeField]
        private RectTransform heroIconAnimationRoot;

        [SerializeField, Min(0.01f)]
        private float heroIconSwitchDuration = 0.25f;

        [SerializeField]
        private Ease heroIconSwitchEase = Ease.OutCubic;

        [SerializeField]
        private Button buttonSetting;

        [Header("Performance Overlay")]
        [SerializeField]
        private TMP_Text txtFps;

        [SerializeField, Min(0.05f)]
        private float perfOverlayRefreshInterval = 0.5f;

        private ProfilerRecorder drawCallsRecorder;
        private ProfilerRecorder batchesRecorder;
        private ProfilerRecorder setPassCallsRecorder;

        private float perfFpsAccumulator;
        private int perfFpsFrameCount;
        private float perfRefreshTimer;

        private readonly Tween[] heroIconTweens = new Tween[2];
        private bool isHeroIconSwapped;
        private int heroIconSwitchVersion;

        public HeroSkillBarUI HeroSkillBarUI => heroSkillBarUI;
        private bool isAutoActived = false;
        private HeroDataSO currentSelectedHeroData;
        private int heroDeadCount = 0;

        private void Awake()
        {
            TutorialManager.Instance.OnResolveTarget += OnResolveTarget;
            TutorialManager.Instance.OnClick += OnClickTutorial;
            Instance = this;

            btnEvent.onClick.AddListener(OnClickEvent);
            btnShop.onClick.AddListener(OnClickShop);
            btnBag.onClick.AddListener(OnClickBag);
            profileBtn.onClick.AddListener(OnClickProfile);
            switchMainSubHeroButton.onClick.AddListener(OnSwitchMainSubHeroButtonClicked);

            HideAbleObjects();
            skeletonGraphic.gameObject.SetActive(false);

            drawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            batchesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            setPassCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
        }

        private void OnClickEvent()
        {
            UIManager.Instance.TogglePopupAsync<EventView>().Forget();
        }

        private void Update()
        {
            UpdatePerformanceOverlay();
        }

        // Cac counter Render (Draw Calls/Batches/SetPass) chi co du lieu trong
        // Development Build hoac Editor Play Mode - build release thuong tra ve 0.
        private void UpdatePerformanceOverlay()
        {
            if (txtFps == null)
            {
                return;
            }

            perfFpsAccumulator += Time.unscaledDeltaTime;
            perfFpsFrameCount++;
            perfRefreshTimer += Time.unscaledDeltaTime;

            if (perfRefreshTimer < perfOverlayRefreshInterval)
            {
                return;
            }

            float avgFps = perfFpsAccumulator > 0f ? perfFpsFrameCount / perfFpsAccumulator : 0f;
            long drawCalls = drawCallsRecorder.Valid ? drawCallsRecorder.LastValue : 0;
            long batches = batchesRecorder.Valid ? batchesRecorder.LastValue : 0;
            long setPassCalls = setPassCallsRecorder.Valid ? setPassCallsRecorder.LastValue : 0;

            txtFps.text = $"FPS: {avgFps:0.0}\nDrawCall: {drawCalls}\nBatches: {batches}\nSetPass: {setPassCalls}";

            perfFpsAccumulator = 0f;
            perfFpsFrameCount = 0;
            perfRefreshTimer = 0f;
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

        private void OnClickBag()
        {
            var items = ItemsManager.Instance.GetItems();
            UIManager.Instance.TogglePopupAsync<BagView>(items, false).Forget();
        }

        private void OnClickShop()
        {
            UIManager.Instance.TogglePopupAsync<ShopView>(false).Forget();
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

        // Nút gương — chỉ PEEK (afk/preview, không commit) để mở popup với số liệu thật.
        // Claim thật sự chỉ xảy ra khi player bấm nút Claim bên trong popup (OnClickClaim).
        private void OnAfkClaimClicked()
        {
            OnAfkClaimClickedAsync().Forget();
        }

        private async UniTaskVoid OnAfkClaimClickedAsync()
        {
            // Field rewardSyncService của riêng TopMainView không được wire trong prefab gốc
            // (fileID: 0) — lấy thẳng từ PvEBattleController, nơi nó luôn được gán thật, giống
            // cách CurrencyTextBinder đang làm.
            RewardSyncService service = PvEBattleController.Instance != null
                ? PvEBattleController.Instance.RewardSyncService
                : rewardSyncService;

            if (service == null)
            {
                Debug.LogWarning("[TopMainView] RewardSyncService not found — not opening popup.");
                return;
            }

            AfkClaimResponse preview = await service.PreviewRewardAsync();
            if (preview == null || !preview.Success)
            {
                Debug.LogWarning("[TopMainView] afk/preview failed — not opening popup.");
                return;
            }

            // Luôn mở popup khi call thành công, kể cả chưa đủ MIN_CLAIM_SECONDS (has_reward=false) —
            // vẫn hiển thị elapsed_seconds/rewards thật (có thể là 0), thay vì im lặng không phản
            // hồi gì khiến nút trông như bị đứng.
            var stageRewards  = PvEBattleController.Instance.GetStageRuntimeData().BaseRewards;
            var earnedRewards = StageRewardConverter.FromRewardDtos(preview.Rewards);

            UIManager.Instance
                .OpenPopupAsync<AFKRewardView>(new AFKRewardArgs
                {
                    OnClaim        = OnClickClaim,
                    Rewards        = stageRewards,
                    EarnedRewards  = earnedRewards,
                    ElapsedSeconds = preview.ElapsedSeconds,
                })
                .Forget();
        }

        // Nút Claim/ClaimX2 bên trong popup — đây mới là lúc commit thật (afk/claim).
        // "Claim x2" (xem ads nhân đôi) chưa có RPC hỗ trợ nên tạm thời vẫn chỉ cộng đúng 1x.
        private void OnClickClaim(bool isClaimX2)
        {
            OnClickClaimAsync(isClaimX2).Forget();
        }

        private async UniTaskVoid OnClickClaimAsync(bool isClaimX2)
        {
            RewardSyncService service = PvEBattleController.Instance != null
                ? PvEBattleController.Instance.RewardSyncService
                : rewardSyncService;

            if (service == null)
                return;

            AfkClaimResponse response = await service.ClaimRewardAsync();
            Debug.Log($"[TopMainView] afk/claim isClaimX2={isClaimX2} hasReward={response?.HasReward}");
        }

        private void OnClickAutoSkill()
        {
            isAutoActived = !isAutoActived;
            UserDataCache.Instance.SetAutoSkill(isAutoActived);

            rotateObject.transform
                .DOLocalRotate(new Vector3(0, 0, isAutoActived ? 180 : 0), 0.2f, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental);
        }

        private void Start()
        {
            var cachedName = UserDataCache.Instance?.DisplayName;

            if (!string.IsNullOrEmpty(cachedName))
                SetDisplayName(cachedName);

            RefreshPlayerInfo();
            autoSkillButton.onClick.AddListener(OnClickAutoSkill);
            btnActiveFramingClaim.onClick.AddListener(OnAfkClaimClicked);
            SetHeroTeamController(HeroTeamController.Instance);

            moveButton.onClick.AddListener(() =>
            {
                UIManager.Instance.TogglePopupAsync<StageSelectionView>(new StageSelectionOpenArgs
                    {
                        CurrentStage = PvEBattleController.Instance.CurrentStage,
                        HighestUnlockedStage = PvEBattleController.Instance.HighestUnlockedStage
                    })
                    .Forget();
            });

            buttonSetting.onClick.AddListener(() => { UIManager.Instance.TogglePopupAsync<SettingView>(); });

            GameEventManager.Subscribe<int>(GameEvents.OnStageCleared, OnStageEnd);
            GameEventManager.Subscribe(GameEvents.OnStageLost, OnStageLost);
            GameEventManager.Subscribe(GameEvents.OnWaveStart, OnStageStart);
            GameEventManager.Subscribe(GameEvents.OnActiveLineupChanged, SetHeroImage);
            GameEventManager.Subscribe<bool>(GameEvents.OnPlayDungeon, OnPlayDungeon);
            BattleHeroSessionController.Instance.HeroDied += OnHeroDied;
        }

        private void OnDestroy()
        {
            drawCallsRecorder.Dispose();
            batchesRecorder.Dispose();
            setPassCallsRecorder.Dispose();

            TutorialManager.Instance.OnResolveTarget -= OnResolveTarget;
            TutorialManager.Instance.OnClick -= OnClickTutorial;
            GameEventManager.Unsubscribe<int>(GameEvents.OnStageCleared, OnStageEnd);
            GameEventManager.Unsubscribe(GameEvents.OnStageLost, OnStageLost);
            GameEventManager.Unsubscribe(GameEvents.OnWaveStart, OnStageStart);
            GameEventManager.Unsubscribe(GameEvents.OnActiveLineupChanged, SetHeroImage);
            GameEventManager.Unsubscribe<bool>(GameEvents.OnPlayDungeon, OnPlayDungeon);

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
            currentSelectedHeroData =
                PvEBattleController.Instance?.OnSwitchMainSubHeroButtonClicked();

            SetHeroSkeletonAnimationGraphic(currentSelectedHeroData);

            SwitchHeroIconVisual();
        }

        private void OnPlayDungeon(bool result)
        {
            for (int i = 0; i < disableObjectsWhenPlayDungeon.Length; i++)
            {
                GameObject currentGameObject = disableObjectsWhenPlayDungeon[i];
                currentGameObject.SetActive(!result);
            }

            if (!result)
            {
                return;
            }

            switchMainSubHeroButton.interactable = true;
            heroDeadCount = 0;

            for (int i = 0; i < enableObjectsWhenPlayDungeon.Length; i++)
            {
                GameObject currentGameObject = enableObjectsWhenPlayDungeon[i];
                currentGameObject.SetActive(result);
            }
        }
        
        private void OnHeroDied(HeroActor actor)
        {
            heroDeadCount++;
            if (heroDeadCount > 1)
            {
                return;
            }
            if (actor.IsChosen)
            {
                OnSwitchMainSubHeroButtonClicked();
            }
            switchMainSubHeroButton.interactable = false;
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
            if (iconRect == null ||
                targetAnchor == null)
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

        public void SetDisplayName(string displayName)
        {
            if (txtPlayerName != null)
                txtPlayerName.text = displayName;
        }

        public void RefreshPlayerInfo()
        {
            var playerLevelInfo = DatabaseManager.Instance.GetLevelByTotalExp(UserDataCache.Instance.Exp);
            txtPlayerLevel.text = $"Lv.{playerLevelInfo.level:00}";
            imgPlayerProgress.fillAmount = playerLevelInfo.progress;
        }

        public CurrencyView CurrencyView => currencyView;

        private void OnStageLost()
        {
            HideAbleObjects();
        }

        private void OnStageEnd(int _)
        {
            if (BattleFlowController.Instance.IsDungeonLocked)
            {
                return;
            }

            HideAbleObjects();
        }

        private void HideAbleObjects()
        {
            if (BattleFlowController.Instance.IsDungeonLocked)
            {
                return;
            }

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

            switchMainSubHeroButton.interactable = true;
            heroDeadCount = 0;
        }
    }
}