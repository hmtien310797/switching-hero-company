using Immortal_Switch.Scripts.Core;
using System;
using System.Threading;
using Battle;
using Battle.Dungeon;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Level.Stage;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class GameStatView : MonoBehaviour
{
    [SerializeField] private TMP_Text currentDeadMonsterQuantityText;
    [FormerlySerializedAs("currentChapterStageText")] 
    [SerializeField] private TMP_Text currentChapterStageNameText;
    [SerializeField] private TMP_Text currentChapterStageDataText;
    [SerializeField] private TMP_Text currentChapterStageEnemyCountText;
    [SerializeField] private Image progressSlide;
    [SerializeField] private Button buttonMap;
    [SerializeField] private Button buttonGiveUp;
    [SerializeField] private Button buttonBoss;
    [SerializeField] private GameObject shinyBossButton;
    [SerializeField] private GameObject monsterKill;
    
    [field:SerializeField]
    public BattleTimerController battleTimerController {get; private set;}
    public static GameStatView Instance { get; private set; }
    
    private const string DeadMonsterQuantityKey = "{0}/{1}";
    private const string NormalChapterStageNameKey = "{0}.{1}";
    private const string NormalChapterStageDataKey = "Stage {0}";
    private const string KillAllDungeonNameKey = "{0}";
    private const string KillAllDungeonStatKey = "Stage {0}  {1}/{2}";

    void Awake()
    {
        Instance = this;
        
        buttonBoss.onClick.AddListener(PvEBattleController.Instance.SpawnBossDirectly);
        buttonBoss.interactable = false;
        buttonGiveUp.interactable = false;
        
        GameEventManager.Subscribe<int>(GameEvents.OnEnemyDead, OnEnemyDead);
        GameEventManager.Subscribe(GameEvents.OnWaveStart, OnInitNewStage);
        GameEventManager.Subscribe<int>(GameEvents.OnStageCleared, OnStageCleared);
        GameEventManager.Subscribe(GameEvents.OnStageLost, OnStageLost);
        GameEventManager.Subscribe(GameEvents.OnInitNewStage,(Action<bool, bool, StageRuntimeData>) OnInitNewStage);
        
        GameEventManager.Subscribe<DungeonKillAllDto>(GameEvents.OnKillAllDungeonInit, OnKillAllDungeonInit);
        GameEventManager.Subscribe(GameEvents.OnKillAllDungeonEnemyCountChanged, (Action<int, int>)OnDungeonEnemyKill);
        
        GameEventManager.Subscribe<DefenseDungeonDto>(GameEvents.OnDefenseDungeonInit, OnDefenseDungeonInit);
        GameEventManager.Subscribe<float>(GameEvents.OnDefenseDungeonDataChanged, OnDefenseDungeonDataChange);
    }

    private void OnDestroy()
    {
        GameEventManager.Unsubscribe<int>(GameEvents.OnEnemyDead, OnEnemyDead);
        GameEventManager.Unsubscribe(GameEvents.OnWaveStart, OnInitNewStage);
        GameEventManager.Unsubscribe<int>(GameEvents.OnStageCleared, OnStageCleared);
        GameEventManager.Unsubscribe(GameEvents.OnStageLost, OnStageLost);
        GameEventManager.Unsubscribe(GameEvents.OnInitNewStage,(Action<bool, bool, StageRuntimeData>) OnInitNewStage);
        GameEventManager.Unsubscribe<DungeonKillAllDto>(GameEvents.OnKillAllDungeonInit, OnKillAllDungeonInit);
        GameEventManager.Unsubscribe(GameEvents.OnKillAllDungeonEnemyCountChanged, (Action<int, int>)OnDungeonEnemyKill);
        GameEventManager.Unsubscribe<float>(GameEvents.OnDefenseDungeonDataChanged, OnDefenseDungeonDataChange);
    }

    private void OnEnemyDead(int deadCount)
    {
        currentDeadMonsterQuantityText.text = string.Format(DeadMonsterQuantityKey, deadCount, GameData.Instance.maxCreepsPerStage);
        progressSlide.fillAmount = (float)deadCount / GameData.Instance.maxCreepsPerStage;
    }

    private void OnKillAllDungeonInit(DungeonKillAllDto data)
    {
        currentChapterStageNameText.text = data.DungeonName;
        currentChapterStageDataText.text = string.Format(NormalChapterStageDataKey, data.Stage);
        currentChapterStageEnemyCountText.text =
            string.Format(DeadMonsterQuantityKey, data.KilledCount, data.TotalEnemyCount);
        progressSlide.fillAmount = (float)data.KilledCount / data.TotalEnemyCount;
    }
    
    private void OnDefenseDungeonInit(DefenseDungeonDto data)
    {
        currentChapterStageNameText.text = data.Name;
        currentChapterStageDataText.text = string.Format(NormalChapterStageDataKey, data.Stage);
        currentChapterStageEnemyCountText.text = string.Empty;
        progressSlide.fillAmount = 1f;
    }

    private void OnDungeonEnemyKill(int killCount, int totalEnemy)
    {
        currentChapterStageEnemyCountText.text =
            string.Format(DeadMonsterQuantityKey, killCount, totalEnemy);
        
        progressSlide.fillAmount = (float) killCount / totalEnemy;
    }
    
    private void OnDefenseDungeonDataChange(float progress)
    {
        progressSlide.fillAmount = progress;
    }

    private void OnInitNewStage()
    {
        buttonMap.gameObject.SetActive(true);
        monsterKill.SetActive(true);
        battleTimerController.HideTimer();
        currentChapterStageEnemyCountText.text = string.Empty;
    }

    private void OnStageCleared(int _)
    {
        buttonBoss.interactable = false;
        battleTimerController.HideTimer();
    }

    private void OnStageLost()
    {
        buttonBoss.interactable = true;
        battleTimerController.HideTimer();
    }

    private void OnInitNewStage(bool playCompletedStage, bool isLosingStage, StageRuntimeData stageRuntimeData)
    {
        currentChapterStageNameText.text = string.Format(NormalChapterStageNameKey, stageRuntimeData.ChapterIndex + 1, stageRuntimeData.ChapterName);
        currentChapterStageDataText.text = string.Format(NormalChapterStageDataKey, stageRuntimeData.GlobalStage);
        buttonBoss.gameObject.SetActive(!playCompletedStage);
        shinyBossButton.gameObject.SetActive(isLosingStage && !playCompletedStage);
        buttonGiveUp.gameObject.SetActive(false);
    }

    public async UniTask InitTimer(float dur, float delay, Action Act, CancellationToken cancellationToken)
    {
        if (delay >= 0)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: cancellationToken);
        }
        battleTimerController.InitTimer(dur, Act, cancellationToken);
        buttonMap.gameObject.SetActive(false);
        monsterKill.SetActive(false);
        buttonGiveUp.gameObject.SetActive(true);
        buttonBoss.gameObject.SetActive(false);
    }

    public void SetHeaderName(string name)
    {
        currentChapterStageNameText.text = name;
    }

    public void HideTimer()
    {
        battleTimerController.HideTimer();
    }
}
