using System;
using System.Linq;
using Battle;
using Common;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Immortal_Switch.Scripts.Addressable;
using Immortal_Switch.Scripts.AFKReward.Views;
using Immortal_Switch.Scripts.Bag.Views;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Event.EventLeHoiBangLong;
using Immortal_Switch.Scripts.Event.Views;
using Immortal_Switch.Scripts.GameSetting.Views;
using Immortal_Switch.Scripts.UI.Skill;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Items;
using Immortal_Switch.Scripts.Leaderboard.Views;
using Immortal_Switch.Scripts.Level.Stage;
using Immortal_Switch.Scripts.PlayerSystem.Views;
using Immortal_Switch.Scripts.Reward;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.Constants;
using Immortal_Switch.Scripts.Shared.Views;
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
        private Button btnEventLeHoiBangLong;

        [SerializeField]
        private Button btnLeaderboard;

        [SerializeField]
        private Button btnGoldInfo;

        [SerializeField]
        private Button btnDiamondInfo;

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
        private TMP_Text txtAfkClaimTimer;

        [SerializeField]
        private RewardSyncService rewardSyncService;

        // Phải khớp MIN_CLAIM_SECONDS phía server (nakama/src/handler/afk.js) — nút chỉ
        // interactable khi AFK đã tích lũy đủ ngần này giây kể từ checkpoint gần nhất.
        private const float AfkClaimMinSeconds = 60f;

        // Fallback khi chưa đồng bộ được max_offline_seconds thật từ server (xem MAX_OFFLINE_SECONDS
        // trong nakama/src/handler/afk.js) — chỉ dùng trước lần sync đầu tiên.
        private const double DefaultAfkMaxOfflineSeconds = 43200d;

        private double afkAccumulatedSeconds;
        private double afkMaxOfflineSeconds = DefaultAfkMaxOfflineSeconds;
        private bool afkTimerSynced;

        [SerializeField]
        private GameObject[] disableObjectsWhenPlayDungeon;

        [SerializeField]
        private GameObject[] enableObjectsWhenPlayDungeon;

        [Header("Hero Icon Switch Animation")]
        [SerializeField]
        private Image[] iconHeroImage;

        [SerializeField]
        private Image[] iconHeroClassImage;

        [SerializeField]
        private RectTransform[] heroIconAnchors;

        [SerializeField]
        private RectTransform[] heroClassIconAnchors;

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

        // Cho AFKRewardView (popup) dùng chung bộ đếm live này thay vì tự đứng yên
        // tại ElapsedSeconds snapshot lúc mở popup.
        public double AfkAccumulatedSeconds => afkAccumulatedSeconds;
        public double AfkMaxOfflineSeconds => afkMaxOfflineSeconds;
        private bool isAutoActived = false;
        private HeroDataSO currentSelectedHeroData;
        private int heroDeadCount = 0;

        private void Awake()
        {
            TutorialManager.Instance.OnResolveTarget += OnResolveTarget;
            TutorialManager.Instance.OnClick += OnClickTutorial;
            Instance = this;

            btnLeaderboard.onClick.AddListener(OnClickLeaderboard);
            btnDiamondInfo.onClick.AddListener(OnClickDiamondInfo);
            btnGoldInfo.onClick.AddListener(OnClickGoldInfo);

            btnEventLeHoiBangLong.onClick.AddListener(OnClickEventLeHoiBangLong);
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

        private void OnClickLeaderboard()
        {
            UIManager.Instance.TogglePopupAsync<LeaderboardView>().Forget();
        }

        private void OnClickDiamondInfo()
        {
            ShowItemInfo(ItemIdConstants.DIAMOND);
        }

        private void OnClickGoldInfo()
        {
            ShowItemInfo(ItemIdConstants.GOLD);
        }

        private void ShowItemInfo(int itemId)
        {
            UIManager.Instance
                .OpenPopupAsync<PopupItemInfoView>(new PopupItemInfoArgs
                {
                    ItemId = itemId,
                })
                .Forget();
        }

        private void OnClickEvent()
        {
            UIManager.Instance.TogglePopupAsync<EventView>().Forget();
        }

        private void OnClickEventLeHoiBangLong()
        {
            UIManager.Instance.TogglePopupAsync<EventLeHoiBangLongView>().Forget();
        }

        private void Update()
        {
            UpdatePerformanceOverlay();
            UpdateAfkClaimTimer();
        }

        // Tự đếm nội suy phía client giữa hai lần đồng bộ server, để nút/text không đứng im chờ
        // response — vẫn tiếp tục tick (và cập nhật text) cho tới khi chạm trần max_offline_seconds
        // thật của server, dù nút đã interactable từ lâu (đủ AfkClaimMinSeconds).
        private void UpdateAfkClaimTimer()
        {
            if (!afkTimerSynced ||
                btnActiveFramingClaim == null)
            {
                return;
            }

            if (afkAccumulatedSeconds < afkMaxOfflineSeconds)
            {
                afkAccumulatedSeconds = Math.Min(afkMaxOfflineSeconds, afkAccumulatedSeconds + Time.unscaledDeltaTime);
                UpdateAfkClaimButtonInteractable();
                RefreshAfkClaimTimerText();
            }
        }

        private void UpdateAfkClaimButtonInteractable()
        {
            if (btnActiveFramingClaim == null)
            {
                return;
            }

            btnActiveFramingClaim.interactable = afkTimerSynced && afkAccumulatedSeconds >= AfkClaimMinSeconds;
        }

        // maxOfflineSeconds <= 0 nghĩa là caller không có giá trị mới từ server (vd. sau khi claim) —
        // giữ nguyên trần đã đồng bộ trước đó thay vì rơi về default.
        private void SyncAfkAccumulatedSeconds(double elapsedSeconds, int maxOfflineSeconds = 0)
        {
            afkAccumulatedSeconds = Math.Max(0d, elapsedSeconds);

            if (maxOfflineSeconds > 0)
            {
                afkMaxOfflineSeconds = maxOfflineSeconds;
            }

            afkTimerSynced = true;
            UpdateAfkClaimButtonInteractable();
            RefreshAfkClaimTimerText();
        }

        private void RefreshAfkClaimTimerText()
        {
            if (txtAfkClaimTimer == null)
            {
                return;
            }

            TimeSpan span = TimeSpan.FromSeconds(afkAccumulatedSeconds);
            txtAfkClaimTimer.text = $"{(int)span.TotalHours:00}:{span.Minutes:00}:{span.Seconds:00}";
        }

        // Kéo elapsed_seconds thật từ afk/preview để seed bộ đếm cục bộ — tránh vừa vào game
        // đã cho bấm claim (hoặc ngược lại bắt chờ đủ 60s dù đã tích lũy sẵn từ lúc offline).
        private async UniTaskVoid RefreshAfkClaimAvailabilityAsync()
        {
            RewardSyncService service = PvEBattleController.Instance != null
                ? PvEBattleController.Instance.RewardSyncService
                : rewardSyncService;

            if (service == null)
            {
                return;
            }

            AfkClaimResponse preview = await service.PreviewRewardAsync();

            if (preview == null ||
                !preview.Success)
            {
                return;
            }

            SyncAfkAccumulatedSeconds(preview.ElapsedSeconds, preview.MaxOfflineSeconds);
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
            var items = ItemsManager.Instance.GetAllItem().Values.ToList();
            UIManager.Instance.TogglePopupAsync<BagView>(items, false).Forget();
        }

        private void OnClickShop()
        {
            UIManager.Instance.TogglePopupAsync<ShopView>(false).Forget();
        }

        private void OnClickProfile()
        {
            UIManager.Instance.TogglePopupAsync<ProfileView>().Forget();
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

        // Nút gương — dùng đúng afkAccumulatedSeconds (số giây đang hiển thị trên txtAfkClaimTimer
        // cạnh nút) để mở popup và tính quà, thay vì gọi thêm 1 lần afk/preview riêng — vì lần gọi
        // đó có thể trả về elapsed lệch pha với số đang hiển thị (do sync trước đó hoặc do checkpoint
        // vừa được cập nhật ở nơi khác), khiến nút hiện đã hơn 1 phút mà popup lại trống quà.
        // Claim thật sự (afk/claim, số liệu do server tính lại) chỉ xảy ra khi bấm nút Claim trong
        // popup (OnClickClaim).
        private void OnAfkClaimClicked()
        {
            if (!afkTimerSynced)
            {
                Debug.LogWarning("[TopMainView] AFK timer chưa đồng bộ với server — chưa mở popup.");
                return;
            }

            if (PvEBattleController.Instance == null)
            {
                Debug.LogWarning("[TopMainView] PvEBattleController chưa sẵn sàng — chưa mở popup.");
                return;
            }

            var stageRewards = PvEBattleController.Instance.GetStageRuntimeData().BaseRewards;
            var earnedRewards = StageRewardConverter.FromBaseRewardsElapsed(stageRewards, afkAccumulatedSeconds);

            UIManager.Instance
                .OpenPopupAsync<AFKRewardView>(new AFKRewardArgs
                {
                    OnClaim = OnClickClaim,
                    Rewards = stageRewards,
                    EarnedRewards = earnedRewards,
                    ElapsedSeconds = (int)afkAccumulatedSeconds,
                    MaxOfflineSeconds = (int)afkMaxOfflineSeconds,
                }, false)
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

            // afk/claim luôn reset checkpoint phía server kể cả khi has_reward=false
            // (xem writeAfkState trong nakama/src/handler/afk.js) — đồng bộ lại bộ đếm cục bộ về 0.
            if (response != null &&
                response.Success)
            {
                GameEventManager.Trigger(GameEvents.ON_AFK_REWARD_CLAIM_COUNT);
                SyncAfkAccumulatedSeconds(0d);
            }
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
            btnActiveFramingClaim.interactable = false;
            RefreshAfkClaimAvailabilityAsync().Forget();
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
                switchMainSubHeroButton.onClick.RemoveListener(OnSwitchMainSubHeroButtonClicked);
            }

            btnLeaderboard.onClick.RemoveListener(OnClickLeaderboard);
            btnDiamondInfo.onClick.RemoveListener(OnClickDiamondInfo);
            btnGoldInfo.onClick.RemoveListener(OnClickGoldInfo);
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
                iconHeroClassImage[i].sprite = HeroImageService.GetHeroClassIcon(heroDataSo);
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

            RectTransform iconHeroClass1 = iconHeroClassImage[0].rectTransform;
            RectTransform iconHeroClass2 = iconHeroClassImage[1].rectTransform;

            RectTransform hero1TargetAnchor = isHeroIconSwapped
                ? heroIconAnchors[1]
                : heroIconAnchors[0];

            RectTransform hero2TargetAnchor = isHeroIconSwapped
                ? heroIconAnchors[0]
                : heroIconAnchors[1];

            RectTransform hero1ClassTargetAnchor = isHeroIconSwapped
                ? heroClassIconAnchors[1]
                : heroClassIconAnchors[0];

            RectTransform hero2ClassTargetAnchor = isHeroIconSwapped
                ? heroClassIconAnchors[0]
                : heroClassIconAnchors[1];

            MoveHeroIcon(
                iconIndex: 0,
                iconRect: iconHero1, iconHeroClass1,
                targetAnchor: hero1TargetAnchor, hero1ClassTargetAnchor,
                switchVersion: currentVersion);

            MoveHeroIcon(
                iconIndex: 1,
                iconRect: iconHero2, iconHeroClass2,
                targetAnchor: hero2TargetAnchor, hero2ClassTargetAnchor,
                switchVersion: currentVersion);
        }

        private void MoveHeroIcon(
            int iconIndex,
            RectTransform iconRect,
            RectTransform iconClassRect,
            RectTransform targetAnchor,
            RectTransform targetClassAnchor,
            int switchVersion)
        {
            if (iconRect == null ||
                iconClassRect == null ||
                targetAnchor == null ||
                targetClassAnchor == null ||
                heroIconAnimationRoot == null)
            {
                return;
            }

            heroIconTweens[iconIndex]?.Kill(false);
            heroIconTweens[iconIndex] = null;

            // Đưa icon về cùng một không gian RectTransform để tween.
            // worldPositionStays = true giúp icon không nhảy tại thời điểm đổi parent.
            iconRect.SetParent(heroIconAnimationRoot, true);
            iconClassRect.SetParent(heroIconAnimationRoot, true);

            Vector3 targetLocalPosition =
                heroIconAnimationRoot.InverseTransformPoint(targetAnchor.position);

            Vector3 targetClassLocalPosition =
                heroIconAnimationRoot.InverseTransformPoint(targetClassAnchor.position);

            Vector3 targetLocalScale =
                ConvertWorldScaleToLocalScale(
                    heroIconAnimationRoot,
                    targetAnchor.lossyScale);

            Vector3 targetClassLocalScale =
                ConvertWorldScaleToLocalScale(
                    heroIconAnimationRoot,
                    targetClassAnchor.lossyScale);

            Sequence sequence = DOTween.Sequence();

            sequence.Join(
                iconRect
                    .DOLocalMove(targetLocalPosition, heroIconSwitchDuration)
                    .SetEase(heroIconSwitchEase));

            sequence.Join(
                iconClassRect
                    .DOLocalMove(targetClassLocalPosition, heroIconSwitchDuration)
                    .SetEase(heroIconSwitchEase));

            sequence.Join(
                iconRect
                    .DOScale(targetLocalScale, heroIconSwitchDuration)
                    .SetEase(heroIconSwitchEase));

            sequence.Join(
                iconClassRect
                    .DOScale(targetClassLocalScale, heroIconSwitchDuration)
                    .SetEase(heroIconSwitchEase));

            sequence
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    if (switchVersion != heroIconSwitchVersion)
                        return;

                    AttachIconToAnchor(iconRect, targetAnchor);
                    AttachIconToAnchor(iconClassRect, targetClassAnchor);

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