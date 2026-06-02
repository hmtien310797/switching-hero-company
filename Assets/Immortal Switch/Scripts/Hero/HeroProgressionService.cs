using UnityEngine;

namespace Immortal_Switch.Scripts.Hero
{
    public class HeroProgressionService
    {
        private readonly HeroProgressionDatabaseSO database;
        private readonly HeroCollectionSaveData saveData;

        public HeroProgressionService(HeroProgressionDatabaseSO database, HeroCollectionSaveData saveData)
        {
            this.database = database;
            this.saveData = saveData;
        }

        public OwnedHeroData GetOrCreateOwnedHero(int heroId)
        {
            var data = saveData.OwnedHeroes.Find(x => x.HeroId == heroId);
            if (data != null) return data;

            data = new OwnedHeroData
            {
                HeroId = heroId,
                IsUnlocked = false,
                CurrentTier = HeroProgressTier.Common,
                CurrentStarInTier = 0,
                CurrentShard = 0
            };

            saveData.OwnedHeroes.Add(data);
            return data;
        }

        public bool HasHero(int heroId)
        {
            return GetOrCreateOwnedHero(heroId).IsUnlocked;
        }

        public bool UnlockHero(HeroDataSO hero)
        {
            if (hero == null) return false;

            var owned = GetOrCreateOwnedHero(hero.Id);
            if (owned.IsUnlocked) return false;

            var config = database.GetProgressionConfig(hero.Id);
            if (config == null)
            {
                Debug.LogError($"Missing HeroProgressionConfig for heroId = {hero.Id}");
                return false;
            }

            owned.IsUnlocked = true;
            owned.CurrentTier = config.StartingTier;
            owned.CurrentStarInTier = config.StartingStarInTier;
            owned.CurrentShard = 0;
            return true;
        }

        public void AddShard(int heroId, int amount)
        {
            if (amount <= 0) return;

            var owned = GetOrCreateOwnedHero(heroId);
            owned.CurrentShard += amount;
        }

        public int GetShard(int heroId) => GetOrCreateOwnedHero(heroId).CurrentShard;

        public HeroProgressionNode GetCurrentNode(int heroId)
        {
            var owned = GetOrCreateOwnedHero(heroId);
            if (!owned.IsUnlocked) return null;

            var config = database.GetProgressionConfig(heroId);
            if (config == null) return null;

            return config.GetNode(owned.CurrentTier, owned.CurrentStarInTier);
        }

        public HeroProgressionNode GetNextNode(int heroId)
        {
            var owned = GetOrCreateOwnedHero(heroId);
            if (!owned.IsUnlocked) return null;

            var config = database.GetProgressionConfig(heroId);
            if (config == null) return null;

            return config.GetNextNode(owned.CurrentTier, owned.CurrentStarInTier);
        }

        public int GetMaxStarInCurrentTier(int heroId)
        {
            var owned = GetOrCreateOwnedHero(heroId);
            if (!owned.IsUnlocked) return 0;

            var config = database.GetProgressionConfig(heroId);
            if (config == null) return 0;

            return config.GetMaxStarInTier(owned.CurrentTier);
        }

        public bool CanUpgrade(int heroId)
        {
            var owned = GetOrCreateOwnedHero(heroId);
            if (!owned.IsUnlocked) return false;

            var currentNode = GetCurrentNode(heroId);
            if (currentNode == null || currentNode.IsMaxNode) return false;

            return owned.CurrentShard >= currentNode.ShardCostToNext;
        }

        public bool UpgradeHero(int heroId)
        {
            var owned = GetOrCreateOwnedHero(heroId);
            if (!owned.IsUnlocked) return false;

            var currentNode = GetCurrentNode(heroId);
            if (currentNode == null || currentNode.IsMaxNode) return false;
            if (owned.CurrentShard < currentNode.ShardCostToNext) return false;

            owned.CurrentShard -= currentNode.ShardCostToNext;
            owned.CurrentTier = currentNode.NextTier;
            owned.CurrentStarInTier = currentNode.NextStarInTier;
            
            return true;
        }

        public HeroStatSnapshot GetCurrentStats(int heroId)
        {
            var hero = database.GetHero(heroId);
            if (hero == null)
            {
                Debug.LogError($"Hero not found: heroId = {heroId}");
                return null;
            }

            var owned = GetOrCreateOwnedHero(heroId);
            if (!owned.IsUnlocked)
            {
                Debug.LogWarning($"Hero not unlocked: heroId = {heroId}");
                return null;
            }

            var node = GetCurrentNode(heroId);
            if (node == null)
            {
                Debug.LogError($"Missing progression node for heroId = {heroId}, tier = {owned.CurrentTier}, star = {owned.CurrentStarInTier}");
                return null;
            }

            return new HeroStatSnapshot
            {
                HeroId = hero.Id,
                CurrentTier = owned.CurrentTier,
                CurrentStarInTier = owned.CurrentStarInTier,
                CurrentShard = owned.CurrentShard,

                Health = node.HealthMultiplier == 0 ? hero.Health : node.HealthMultiplier,
                Defense = node.DefenseMultiplier == 0? hero.Defense : node.DefenseMultiplier,
                Attack = node.AttackMultiplier == 0 ? hero.Attack : node.AttackMultiplier,
                
                //temp lock
                // AttackSpeed = hero.AttackSpeed * node.AttackSpeedMultiplier,
                // CritChance = hero.CritChance * node.CritChanceMultiplier,
                // CritDamage = hero.CritDamage * node.CritDamageMultiplier,
                // Accuracy = hero.Accuracy * node.AccuracyMultiplier,
                // MoveSpeed = hero.MoveSpeed * node.MoveSpeedMultiplier
            };
        }
    }
}