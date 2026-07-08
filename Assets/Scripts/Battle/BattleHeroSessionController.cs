using System;
using System.Collections.Generic;
using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.UI;
using UnityEngine;

namespace Battle
{
    public enum BattleHeroSessionType
    {
        None = 0,
        Chapter = 1,
        Dungeon = 2
    }

    /// <summary>
    /// Quản lý toàn bộ hero runtime dùng chung cho Chapter và Dungeon.
    /// BattleFlowController chỉ điều phối session; controller này quản lý spawn,
    /// controlled hero, hero chết, ultimate presentation và replace lineup.
    /// </summary>
    public sealed class BattleHeroSessionController : Singleton<BattleHeroSessionController>
    {
        [SerializeField] private HeroTeamController heroTeamController;
        [SerializeField, Min(0)] private int controlledHeroSlotIndex;

        private readonly BattleHeroSpawnService heroSpawnService = new();
        private readonly HashSet<HeroActor> deadHeroes = new();

        private UserDataCache userDataCache;
        private GameCameraController gameCameraController;
        private IHeroBattleContext battleContext;
        private Func<int, Vector3> spawnPositionProvider;
        private Action allHeroesDeadCallback;
        private bool sessionActive;

        public BattleHeroSessionType SessionType { get; private set; }
        public bool CanReplaceLineupHero =>
            sessionActive && SessionType == BattleHeroSessionType.Chapter;
        public int ControlledHeroSlotIndex => controlledHeroSlotIndex;
        public int DeadHeroCount => deadHeroes.Count;

        public event Action LineupActorsChanged;
        public event Action<HeroActor> HeroDied;
        public event Action AllHeroesDead;

        protected override void Awake()
        {
            base.Awake();
            userDataCache = UserDataCache.Instance;
            gameCameraController = GameCameraController.Instance;
        }

        public override UniTask InitializeAsync()
        {
            throw new NotImplementedException();
        }

        private void OnEnable()
        {
            GameEventManager.Subscribe(
                GameEvents.OnChangeHero,
                (Action<int, int>)OnChangeHeroRequested
            );
        }

        private void OnDisable()
        {
            GameEventManager.Unsubscribe(
                GameEvents.OnChangeHero,
                (Action<int, int>)OnChangeHeroRequested
            );
        }

        public void BeginSession(
            BattleHeroSessionType sessionType,
            IHeroBattleContext context,
            Func<int, Vector3> heroSpawnPositionProvider,
            Action onAllHeroesDead)
        {
            SessionType = sessionType;
            battleContext = context;
            spawnPositionProvider = heroSpawnPositionProvider;
            allHeroesDeadCallback = onAllHeroesDead;
            sessionActive = true;
            deadHeroes.Clear();

            if (controlledHeroSlotIndex < 0 ||
                controlledHeroSlotIndex >= userDataCache.BattleHeroSlotCount)
            {
                controlledHeroSlotIndex = 0;
            }
        }

        public async UniTask<bool> SpawnLineupAsync()
        {
            if (!sessionActive || battleContext == null || spawnPositionProvider == null)
            {
                Debug.LogError("[BattleHeroSession] Session has not been initialized.", this);
                return false;
            }

            EnsureValidBattleLineup();
            DespawnAllHeroes(clearSession: false);

            bool spawnedAny = false;

            for (int slotIndex = 0;
                 slotIndex < userDataCache.BattleHeroSlotCount;
                 slotIndex++)
            {
                int heroId = userDataCache.GetBattleHeroIdAt(slotIndex);
                if (heroId <= 0)
                    continue;

                HeroActor hero = await SpawnHeroIntoSlotAsync(heroId, slotIndex);
                if (hero == null)
                    continue;

                spawnedAny = true;

                if (slotIndex == controlledHeroSlotIndex)
                {
                    gameCameraController?.SetFollowHero(hero.transform);
                    TopMainView.Instance?.SetHeroSkeletonAnimationGraphic(hero.HeroData);
                }
            }

            RefreshTeamAndSelection();
            RaiseLineupActorsChanged();
            RefreshControlledHeroSkillUI();

            if (!spawnedAny)
                Debug.LogError("[BattleHeroSession] No battle hero could be spawned.", this);

            return spawnedAny;
        }

        public void DespawnAllHeroes(bool clearSession = true)
        {
            if (userDataCache != null)
            {
                for (int slotIndex = 0;
                     slotIndex < userDataCache.BattleHeroSlotCount;
                     slotIndex++)
                {
                    HeroActor hero = userDataCache.GetInBattleHeroActorAt(slotIndex);
                    if (hero != null)
                    {
                        hero.HeroSkillController?
                            .DespawnAllInstanceOfUltimateSkillAndClassSkill();

                        heroSpawnService.Despawn(hero, OnHeroDead);
                    }

                    userDataCache.TrySetInBattleHeroActor(slotIndex, null);
                }
            }

            heroTeamController?.SetHeroes(null, null);
            deadHeroes.Clear();
            RaiseLineupActorsChanged();

            if (clearSession)
            {
                sessionActive = false;
                SessionType = BattleHeroSessionType.None;
                battleContext = null;
                spawnPositionProvider = null;
                allHeroesDeadCallback = null;
            }
        }

        public bool CanReplaceHero(int sourceHeroId, int targetHeroId)
        {
            if (!CanReplaceLineupHero)
                return false;

            if (sourceHeroId <= 0 || targetHeroId <= 0 || sourceHeroId == targetHeroId)
                return false;

            if (!userDataCache.ContainsBattleHero(sourceHeroId))
                return false;

            return !userDataCache.ContainsBattleHero(targetHeroId);
        }

        public void RequestReplaceHero(int sourceHeroId, int targetHeroId)
        {
            ReplaceHeroAsync(sourceHeroId, targetHeroId).Forget();
        }

        public async UniTask<bool> ReplaceHeroAsync(int sourceHeroId, int targetHeroId)
        {
            if (!CanReplaceHero(sourceHeroId, targetHeroId))
            {
                if (SessionType == BattleHeroSessionType.Dungeon)
                {
                    Debug.LogWarning(
                        "[BattleHeroSession] Hero replacement is disabled during Dungeon.",
                        this
                    );
                }

                return false;
            }

            int slotIndex = userDataCache.FindBattleHeroSlot(sourceHeroId);
            if (slotIndex < 0)
                return false;

            HeroActor oldHero = userDataCache.GetInBattleHeroActorAt(slotIndex);
            if (oldHero == null)
                return false;

            HeroDataSO newHeroData = DatabaseManager.Instance.GetHeroDataById(targetHeroId);
            if (newHeroData == null)
                return false;

            HeroActor newHero = await heroSpawnService.SpawnAsync(
                newHeroData,
                oldHero.transform.position,
                battleContext,
                heroTeamController,
                userDataCache.AutoSkill,
                OnHeroDead
            );

            if (newHero == null)
                return false;

            if (!userDataCache.TryReplaceBattleHero(
                    slotIndex,
                    sourceHeroId,
                    targetHeroId,
                    newHero))
            {
                heroSpawnService.Despawn(newHero, OnHeroDead);
                return false;
            }

            deadHeroes.Remove(oldHero);
            TopMainView.Instance?.SetHeroSkeletonAnimationGraphic(newHeroData);

            bool wasControlled = oldHero.IsChosen || slotIndex == controlledHeroSlotIndex;

            oldHero.HeroSkillController?
                .DespawnAllInstanceOfUltimateSkillAndClassSkill();
            heroSpawnService.Despawn(oldHero, OnHeroDead);

            RefreshTeamAndSelection();
            RaiseLineupActorsChanged();
            GameEventManager.Trigger(GameEvents.OnActiveLineupChanged);
            RefreshControlledHeroSkillUI();

            if (wasControlled)
                gameCameraController?.SetFollowHero(newHero.transform);

            SyncLineupToServerAsync().Forget();
            return true;
        }

        public HeroActor GetControlledHero()
        {
            NormalizeControlledSlot();
            return userDataCache.GetInBattleHeroActorAt(controlledHeroSlotIndex);
        }

        public HeroActor GetFollowerHero()
        {
            NormalizeControlledSlot();
            int followerSlotIndex = controlledHeroSlotIndex == 0 ? 1 : 0;
            return userDataCache.GetInBattleHeroActorAt(followerSlotIndex);
        }

        public HeroDataSO SelectControlledHeroSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= userDataCache.BattleHeroSlotCount)
                return null;

            HeroActor hero = userDataCache.GetInBattleHeroActorAt(slotIndex);
            if (hero == null || hero.IsDead)
                return null;

            controlledHeroSlotIndex = slotIndex;
            RefreshTeamAndSelection();
            RefreshControlledHeroSkillUI();
            gameCameraController?.SetFollowHero(hero.transform);
            TopMainView.Instance?.SetHeroSkeletonAnimationGraphic(hero.HeroData);
            return hero.HeroData;
        }

        public HeroDataSO SwitchControlledHero()
        {
            int nextSlotIndex = controlledHeroSlotIndex == 0 ? 1 : 0;
            return SelectControlledHeroSlot(nextSlotIndex);
        }

        public void RefreshControlledHeroSkillUI()
        {
            TopMainView.Instance?.HeroSkillBarUI?.BindHero(GetControlledHero());
        }

        public void OnSelectedHeroCastUltimateSkill()
        {
            gameCameraController?.ZoomToHero().Forget();
            TopMainView.Instance?.PlayHeroSkeletonAnimation();
        }

        private async UniTask<HeroActor> SpawnHeroIntoSlotAsync(int heroId, int slotIndex)
        {
            HeroActor hero = await heroSpawnService.SpawnAsync(
                heroId,
                spawnPositionProvider(slotIndex),
                battleContext,
                heroTeamController,
                userDataCache.AutoSkill,
                OnHeroDead
            );

            if (hero == null)
                return null;

            if (!userDataCache.TrySetInBattleHeroActor(slotIndex, hero))
            {
                heroSpawnService.Despawn(hero, OnHeroDead);
                return null;
            }

            return hero;
        }

        private void OnChangeHeroRequested(int sourceHeroId, int targetHeroId)
        {
            RequestReplaceHero(sourceHeroId, targetHeroId);
        }

        private void OnHeroDead(HeroActor hero)
        {
            if (hero == null || !deadHeroes.Add(hero))
                return;

            HeroDied?.Invoke(hero);

            if (!AreAllSpawnedHeroesDead())
                return;

            AllHeroesDead?.Invoke();
            allHeroesDeadCallback?.Invoke();
        }

        private bool AreAllSpawnedHeroesDead()
        {
            bool hasAnyHero = false;

            for (int slotIndex = 0;
                 slotIndex < userDataCache.BattleHeroSlotCount;
                 slotIndex++)
            {
                HeroActor hero = userDataCache.GetInBattleHeroActorAt(slotIndex);
                if (hero == null)
                    continue;

                hasAnyHero = true;
                if (!hero.IsDead)
                    return false;
            }

            return hasAnyHero;
        }

        private void EnsureValidBattleLineup()
        {
            List<int> heroIds = new(userDataCache.InBattleHeroIdList);
            while (heroIds.Count < userDataCache.BattleHeroSlotCount)
                heroIds.Add(-1);

            if (heroIds.Exists(id => id > 0))
                return;

            var owned = userDataCache.HeroList?.Owned;
            if (owned != null && owned.Length > 0)
            {
                for (int i = 0; i < heroIds.Count && i < owned.Length; i++)
                    heroIds[i] = owned[i].HeroId;
            }
            else
            {
                heroIds[0] = 2;
                if (heroIds.Count > 1)
                    heroIds[1] = 4;
            }

            userDataCache.SetBattleLineup(heroIds);
        }

        private void RefreshTeamAndSelection()
        {
            HeroActor heroA = userDataCache.GetInBattleHeroActorAt(0);
            HeroActor heroB = userDataCache.GetInBattleHeroActorAt(1);
            heroTeamController?.SetHeroes(heroA, heroB);

            NormalizeControlledSlot();

            for (int i = 0; i < userDataCache.BattleHeroSlotCount; i++)
            {
                HeroActor hero = userDataCache.GetInBattleHeroActorAt(i);
                if (hero != null)
                    hero.SetChosen(i == controlledHeroSlotIndex);
            }

            if (controlledHeroSlotIndex == 0)
                heroTeamController?.SelectHeroA();
            else
                heroTeamController?.SelectHeroB();
        }

        private void NormalizeControlledSlot()
        {
            if (controlledHeroSlotIndex < 0 ||
                controlledHeroSlotIndex >= userDataCache.BattleHeroSlotCount ||
                userDataCache.GetInBattleHeroActorAt(controlledHeroSlotIndex) == null)
            {
                controlledHeroSlotIndex =
                    userDataCache.GetInBattleHeroActorAt(0) != null ? 0 : 1;
            }
        }

        private void RaiseLineupActorsChanged()
        {
            LineupActorsChanged?.Invoke();
        }

        private async UniTask SyncLineupToServerAsync()
        {
            if (NakamaClient.Instance == null || !NakamaClient.Instance.IsLoggedIn)
                return;

            var lineupUids = new string[userDataCache.InBattleHeroIdList.Count];
            bool missingUid = false;
            for (int i = 0; i < lineupUids.Length; i++)
            {
                int heroId = userDataCache.InBattleHeroIdList[i];
                if (heroId > 0)
                {
                    lineupUids[i] = userDataCache.GetHeroUid(heroId);
                    if (string.IsNullOrEmpty(lineupUids[i]))
                        missingUid = true;
                }
                else
                {
                    lineupUids[i] = null;
                }
            }

            // HeroList.Owned có thể chưa có hero mới summon trong session này
            // (IsAcquired check dùng HeroProgressionService, không phải HeroList.Owned).
            // Nếu thiếu UID, refresh hero/list trước để tránh ghi null lên server.
            if (missingUid)
            {
                try
                {
                    var heroListResponse = await NakamaClient.Instance.GetHeroListAsync();
                    if (heroListResponse?.Owned != null && userDataCache.HeroList != null)
                        userDataCache.HeroList.Owned = heroListResponse.Owned;

                    for (int i = 0; i < lineupUids.Length; i++)
                    {
                        int heroId = userDataCache.InBattleHeroIdList[i];
                        lineupUids[i] = heroId > 0 ? userDataCache.GetHeroUid(heroId) : null;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[PvE] hero/list refresh failed before lineup sync: {e.Message}");
                    return;
                }
            }

            try
            {
                var response = await NakamaClient.Instance.SetLineupAsync(lineupUids);
                if (response != null && response.Updated && userDataCache.HeroList != null)
                    userDataCache.HeroList.Lineup = response.Lineup;
            }
            catch (Exception e)
            {
                Debug.LogError($"[PvE] hero/set_lineup RPC failed: {e.Message}");
            }
        }
    }
}
