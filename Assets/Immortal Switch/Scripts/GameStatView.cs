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
    private bool playCompletedStage;

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
        GameEventManager.Subscribe<int>(GameEvents.OnStageCleared, OnStageCleared);
        GameEventManager.Subscribe(GameEvents.OnStageLost, OnStageLost);
        GameEventManager.Subscribe(GameEvents.OnInitNewStage, (Action<bool, bool, StageRuntimeData>)OnInitNewStage);
        GameEventManager.Subscribe(GameEvents.OnStageSessionChange, OnStageSessionChanged);
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
        GameEventManager.Unsubscribe<int>(GameEvents.OnStageCleared, OnStageCleared);
        GameEventManager.Unsubscribe(GameEvents.OnStageLost, OnStageLost);
        GameEventManager.Unsubscribe(GameEvents.OnInitNewStage, (Action<bool, bool, StageRuntimeData>)OnInitNewStage);
        GameEventManager.Unsubscribe(GameEvents.OnStageSessionChange, OnStageSessionChanged);
        GameEventManager.Unsubscribe<DungeonKillAllDto>(GameEvents.OnKillAllDungeonInit, OnKillAllDungeonInit);
        GameEventManager.Unsubscribe(GameEvents.OnKillAllDungeonEnemyCountChanged, (Action<int, int>)OnDungeonEnemyKill);

        GameEventManager.Unsubscribe<DefenseDungeonDto>(GameEvents.OnDefenseDungeonInit, OnDefenseDungeonInit);
        GameEventManager.Unsubscribe<float>(GameEvents.OnDefenseDungeonDataChanged, OnDefenseDungeonDataChange);
    }

    private void OnStageSessionChanged()
    {
        btnGiveUp.gameObject.SetActive(false);
        buttonBoss.SetStatus(ETabPresetStatus.Disabled);
        buttonMap.SetStatus(ETabPresetStatus.Disabled);
    }

    private void OnClickRetreat()
    {
        BattleFlowController.Instance.SurrenderBattle();
        battleTimerController.HideTimer();
        btnGiveUp.gameObject.SetActive(false);
    }

    private void OnClickBoss(int _)
    {
        if (playCompletedStage)
        {
            return;
        }
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
        RefreshGameProgressionState(deadCount == stageDataResolverSo.MaxCreepsPerStage && !playCompletedStage);
    }

    private void OnKillAllDungeonInit(DungeonKillAllDto data)
    {
        currentChapterStageNameText.text = $"{data.DungeonName} {string.Format(NormalChapterStageDataKey, data.Stage)}";
        progressionPreset.SetStatus(ETabPresetStatus.Normal);
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
        monsterKill.gameObject.SetActive(true);
        battleTimerController.HideTimer();
        btnGiveUp.gameObject.SetActive(false);
    }

    private void OnStageCleared(int _)
    {
        btnGiveUp.gameObject.SetActive(false);
        battleTimerController.HideTimer();
    }

    private void OnStageLost()
    {
        battleTimerController.HideTimer();
        btnGiveUp.gameObject.SetActive(false);
    }

    private void OnInitNewStage(bool playCompletedStage, bool isLosingStage, StageRuntimeData stageRuntimeData)
    {
        OnInitNewStage();
        currentChapterStageNameText.text =
            $"{string.Format(NormalChapterStageNameKey, stageRuntimeData.ChapterIndex + 1, stageRuntimeData.ChapterName)} {string.Format(NormalChapterStageDataKey, stageRuntimeData.GlobalStage)}";

        /*currentChapterStageDataText.text = string.Format(NormalChapterStageDataKey, stageRuntimeData.GlobalStage);*/
        this.playCompletedStage = playCompletedStage;
        RefreshGameProgressionState(isLosingStage && !playCompletedStage);
        btnGiveUp.gameObject.SetActive(false);
    }

    public async UniTask InitTimer(float dur, float delay, Action Act, CancellationToken cancellationToken)
    {
        buttonMap.SetStatus(ETabPresetStatus.Disabled);
        buttonBoss.SetStatus(ETabPresetStatus.Disabled);
        monsterKill.gameObject.SetActive(false);
        
        if (delay >= 0)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: cancellationToken);
        }
        
        btnGiveUp.gameObject.SetActive(true);
        battleTimerController.InitTimer(dur, Act, cancellationToken);
    }

    private void OnPlayingDungeon(bool result)
    {
        if (!result)
            return;

        buttonBoss.SetStatus(ETabPresetStatus.Disabled);
        buttonMap.SetStatus(ETabPresetStatus.Disabled);
        btnGiveUp.gameObject.SetActive(false);
    }

    public void ExitDungeonGamePlay()
    {
        btnGiveUp.gameObject.SetActive(false);
        buttonBoss.gameObject.SetActive(false);

        btnGiveUp.gameObject.SetActive(false);
        buttonMap.SetStatus(ETabPresetStatus.Disabled);
        progressionPreset.SetStatus(ETabPresetStatus.Normal);
        currentChapterStageNameText.text = string.Empty;
        battleTimerController.HideTimer();
    }

    private void RefreshGameProgressionState(bool canMoveBoss)
    {
        if (BattleFlowController.Instance.IsDungeonLocked)
        {
            return;
        }
        if (canMoveBoss)
        {
            buttonMap.SetStatus(ETabPresetStatus.Selected);
            buttonBoss.SetStatus(ETabPresetStatus.Selected);
            buttonBoss.SetInteractable(true);
            monsterKill.SetStatus(ETabPresetStatus.Selected);
            progressionPreset.SetStatus(ETabPresetStatus.Selected);
        }
        else
        {
            buttonMap.SetStatus(ETabPresetStatus.Normal);
            buttonBoss.SetStatus(ETabPresetStatus.Normal);
            buttonBoss.SetInteractable(false);
            monsterKill.SetStatus(ETabPresetStatus.Normal);
            progressionPreset.SetStatus(ETabPresetStatus.Normal);
        }
    }
}