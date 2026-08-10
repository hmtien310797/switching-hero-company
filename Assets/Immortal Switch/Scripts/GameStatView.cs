using Immortal_Switch.Scripts.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using Battle;
using Battle.Dungeon;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Level.Stage;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.UI;
using Immortal_Switch.Scripts.StageSelection;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class GameStatView : MonoBehaviour
{
    [Header("Progression kill references")]
    [SerializeField]
    private Image imgKillProgress;

    [SerializeField]
    private RectTransform rtKillProgressFx;

    [SerializeField]
    private TMP_Text currentDeadMonsterQuantityText;

    [FormerlySerializedAs("currentChapterStageText")]
    [SerializeField]
    private TMP_Text currentChapterStageNameText;

    [SerializeField]
    private UITabPreset buttonMap;

    [SerializeField]
    private UITabPreset buttonBoss;

    [SerializeField]
    private UITabPreset progressionPreset;

    [SerializeField]
    private GameObject shinyBossButton;

    [SerializeField]
    private UITabPreset monsterKill;

    [Header("Boss progression references")]
    [SerializeField]
    private List<GameObject> bossStateObjects;

    [SerializeField]
    private Button btnGiveUp;

    [field: SerializeField]
    public BattleTimerController battleTimerController { get; private set; }

    public static GameStatView Instance { get; private set; }

    private const string DeadMonsterQuantityKey = "{0}/{1}";
    private const string NormalChapterStageNameKey = "{0}.{1}";
    private const string BossChapterStageDataKey = "Stage <color=#332825>{0}</color>";
    private const string NormalChapterStageDataKey = "Stage <color=#71C68C>{0}</color>";
    private const string KillAllDungeonNameKey = "{0}";
    private const string KillAllDungeonStatKey = "Stage {0}  {1}/{2}";

    // --- Private Fields ---
    private float _killProgressWidth;
    private float _killProgressFxWidth;

    private StageDataResolverSO stageDataResolverSo;

    void Awake()
    {
        _killProgressWidth = imgKillProgress.rectTransform.rect.width;
        _killProgressFxWidth = rtKillProgressFx.rect.width;

        Instance = this;

        buttonMap.Bind(0, string.Empty, OnClickChapterStage);
        buttonBoss.Bind(0, string.Empty, OnClickBoss);
        btnGiveUp.onClick.AddListener(OnClickRetreat);

        //buttonBoss.interactable = false;
        //btnGiveUp.interactable = false;

        GameEventManager.Subscribe<bool>(GameEvents.OnPlayDungeon, OnPlayingDungeon);
        GameEventManager.Subscribe<int>(GameEvents.OnEnemyDead, OnEnemyDead);
        GameEventManager.Subscribe(GameEvents.OnWaveStart, OnInitNewStage);
        GameEventManager.Subscribe<int>(GameEvents.OnStageCleared, OnStageCleared);
        GameEventManager.Subscribe(GameEvents.OnStageLost, OnStageLost);
        GameEventManager.Subscribe(GameEvents.OnInitNewStage, (Action<bool, bool, StageRuntimeData>)OnInitNewStage);

        GameEventManager.Subscribe<DungeonKillAllDto>(GameEvents.OnKillAllDungeonInit, OnKillAllDungeonInit);
        GameEventManager.Subscribe(GameEvents.OnKillAllDungeonEnemyCountChanged, (Action<int, int>)OnDungeonEnemyKill);

        GameEventManager.Subscribe<DefenseDungeonDto>(GameEvents.OnDefenseDungeonInit, OnDefenseDungeonInit);
        GameEventManager.Subscribe<float>(GameEvents.OnDefenseDungeonDataChanged, OnDefenseDungeonDataChange);
        stageDataResolverSo = DatabaseManager.Instance.StageDataResolver;
    }

    private void OnDestroy()
    {
        GameEventManager.Unsubscribe<bool>(GameEvents.OnPlayDungeon, OnPlayingDungeon);
        GameEventManager.Unsubscribe<int>(GameEvents.OnEnemyDead, OnEnemyDead);
        GameEventManager.Unsubscribe(GameEvents.OnWaveStart, OnInitNewStage);
        GameEventManager.Unsubscribe<int>(GameEvents.OnStageCleared, OnStageCleared);
        GameEventManager.Unsubscribe(GameEvents.OnStageLost, OnStageLost);
        GameEventManager.Unsubscribe(GameEvents.OnInitNewStage, (Action<bool, bool, StageRuntimeData>)OnInitNewStage);

        GameEventManager.Unsubscribe<DungeonKillAllDto>(GameEvents.OnKillAllDungeonInit, OnKillAllDungeonInit);
        GameEventManager.Unsubscribe(GameEvents.OnKillAllDungeonEnemyCountChanged, (Action<int, int>)OnDungeonEnemyKill);

        GameEventManager.Unsubscribe<DefenseDungeonDto>(GameEvents.OnDefenseDungeonInit, OnDefenseDungeonInit);
        GameEventManager.Unsubscribe<float>(GameEvents.OnDefenseDungeonDataChanged, OnDefenseDungeonDataChange);
    }

    private void OnClickRetreat()
    {
        BattleFlowController.Instance.SurrenderBattle();
        battleTimerController.HideTimer();
    }

    private void OnClickBoss(int _)
    {
        buttonBoss.gameObject.SetActive(false);
        PvEBattleController.Instance.SpawnBossDirectly();
    }

    private void OnClickChapterStage(int _)
    {
        UIManager.Instance
            .TogglePopupAsync<StageSelectionView>(new StageSelectionOpenArgs
            {
                CurrentStage = PvEBattleController.Instance.CurrentStage,
                HighestUnlockedStage = PvEBattleController.Instance.HighestUnlockedStage,
            })
            .Forget();
    }

    private void OnEnemyDead(int deadCount)
    {
        currentDeadMonsterQuantityText.text =
            string.Format(DeadMonsterQuantityKey, deadCount, stageDataResolverSo.MaxCreepsPerStage);

        OnDefenseDungeonDataChange((float)deadCount / stageDataResolverSo.MaxCreepsPerStage);
        RefreshGameProgressionState(deadCount == stageDataResolverSo.MaxCreepsPerStage);
    }

    private void OnKillAllDungeonInit(DungeonKillAllDto data)
    {
        currentChapterStageNameText.text = $"{data.DungeonName} {string.Format(NormalChapterStageDataKey, data.Stage)}";
        /*currentChapterStageDataText.text = string.Format(NormalChapterStageDataKey, data.Stage);

        currentChapterStageEnemyCountText.text =
            string.Format(DeadMonsterQuantityKey, data.KilledCount, data.TotalEnemyCount);*/

        OnDefenseDungeonDataChange((float)data.KilledCount / data.TotalEnemyCount);
    }

    private void OnDefenseDungeonInit(DefenseDungeonDto data)
    {
        currentChapterStageNameText.text = $"{data.Name} {string.Format(NormalChapterStageDataKey, data.Stage)}";
        /*currentChapterStageDataText.text = string.Format(NormalChapterStageDataKey, data.Stage);
        currentChapterStageEnemyCountText.text = string.Empty;*/

        OnDefenseDungeonDataChange(1f);
    }

    private void OnDungeonEnemyKill(int killCount, int totalEnemy)
    {
        /*currentChapterStageEnemyCountText.text =
            string.Format(DeadMonsterQuantityKey, killCount, totalEnemy);*/

        OnDefenseDungeonDataChange((float)killCount / totalEnemy);
    }

    private void OnDefenseDungeonDataChange(float progress)
    {
        imgKillProgress.fillAmount = progress;

        var newAnchoredPosition = _killProgressWidth * progress - _killProgressFxWidth;
        var anchoredPosition = rtKillProgressFx.anchoredPosition;

        anchoredPosition.x = newAnchoredPosition;
        rtKillProgressFx.anchoredPosition = anchoredPosition;
    }

    private void OnInitNewStage()
    {
        //buttonMap.gameObject.SetActive(true);
        monsterKill.gameObject.SetActive(true);
        battleTimerController.HideTimer();
        SetActiveBossProgression(false);

        //buttonGiveUp.gameObject.SetActive(false);
        /*currentChapterStageEnemyCountText.text = string.Empty;*/
    }

    private void OnStageCleared(int _)
    {
        SetActiveBossProgression(false);

        //buttonBoss.SetInteractable(false);

        //buttonBoss.interactable = false;
        battleTimerController.HideTimer();

        //buttonGiveUp.gameObject.SetActive(false);
    }

    private void OnStageLost()
    {
        //buttonBoss.interactable = true;
        //buttonBoss.SetInteractable(true);
        battleTimerController.HideTimer();
        SetActiveBossProgression(false);

        //buttonGiveUp.gameObject.SetActive(false);
    }

    private void OnInitNewStage(bool playCompletedStage, bool isLosingStage, StageRuntimeData stageRuntimeData)
    {
        currentChapterStageNameText.text =
            $"{string.Format(NormalChapterStageNameKey, stageRuntimeData.ChapterIndex + 1, stageRuntimeData.ChapterName)} {string.Format(NormalChapterStageDataKey, stageRuntimeData.GlobalStage)}";

        /*currentChapterStageDataText.text = string.Format(NormalChapterStageDataKey, stageRuntimeData.GlobalStage);*/
        buttonBoss.gameObject.SetActive(!playCompletedStage);

        // Trước đây buttonBoss.interactable chỉ được mở qua sự kiện OnStageLost (lúc hero chết
        // thật trong session) — không đủ cho case resume sau khi đóng/mở lại app với
        // isLosingStage = true do server báo stage_creeps_cleared (không có OnStageLost nào
        // xảy ra). Set trực tiếp ở đây theo đúng nguồn dữ liệu (isLosingStage) cho cả 2 case.
        RefreshGameProgressionState(isLosingStage && !playCompletedStage);

        //buttonBoss.interactable = isLosingStage && !playCompletedStage;
        //shinyBossButton.gameObject.SetActive(isLosingStage && !playCompletedStage);
        //buttonGiveUp.gameObject.SetActive(false);
        SetActiveBossProgression(false);
    }

    public async UniTask InitTimer(float dur, float delay, Action Act, CancellationToken cancellationToken)
    {
        buttonMap.gameObject.SetActive(false);
        monsterKill.gameObject.SetActive(false);

        //buttonGiveUp.interactable = true;
        buttonBoss.gameObject.SetActive(false);

        if (delay >= 0)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: cancellationToken);
        }

        SetActiveBossProgression(true);

        //buttonGiveUp.gameObject.SetActive(true);
        battleTimerController.InitTimer(dur, Act, cancellationToken);
    }

    private void OnPlayingDungeon(bool result)
    {
        if (!result)
            return;

        buttonBoss.gameObject.SetActive(false);
        buttonMap.gameObject.SetActive(false);

        //buttonGiveUp.gameObject.SetActive(false);
        SetActiveBossProgression(false);
    }

    public void ExitDungeonGamePlay()
    {
        SetActiveBossProgression(false);
        buttonBoss.gameObject.SetActive(false);

        //buttonGiveUp.gameObject.SetActive(false);
        //buttonMap.gameObject.SetActive(false);
        battleTimerController.HideTimer();
    }

    private void SetActiveBossProgression(bool active)
    {
        if (active)
        {
            foreach (var o in bossStateObjects)
            {
                o.SetActive(true);
            }

            btnGiveUp.gameObject.SetActive(true);
        }
        else
        {
            foreach (var o in bossStateObjects)
            {
                o.SetActive(false);
            }

            btnGiveUp.gameObject.SetActive(false);
        }
    }

    private void RefreshGameProgressionState(bool canMoveBoss)
    {
        if (canMoveBoss)
        {
            buttonBoss.gameObject.SetActive(true);
            //buttonMap.gameObject.SetActive(true);

            buttonMap.SetStatus(ETabPresetStatus.Selected);
            buttonBoss.SetStatus(ETabPresetStatus.Selected);
            monsterKill.SetStatus(ETabPresetStatus.Selected);
            progressionPreset.SetStatus(ETabPresetStatus.Selected);
        }
        else
        {
            buttonBoss.gameObject.SetActive(false);
            //buttonMap.gameObject.SetActive(true);

            buttonMap.SetStatus(ETabPresetStatus.Normal);
            buttonBoss.SetStatus(ETabPresetStatus.Normal);
            monsterKill.SetStatus(ETabPresetStatus.Normal);
            progressionPreset.SetStatus(ETabPresetStatus.Normal);
        }
    }
}