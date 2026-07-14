using Immortal_Switch.Scripts.Addressable;
using Immortal_Switch.Scripts.Hero;
using UnityEngine;

namespace Immortal_Switch.Scripts.HeroUIView
{
    public static class HeroCollectionItemViewDataFactory
    {
        public static HeroCollectionItemViewData Build(
            HeroDataSO hero,
            HeroProgressionDatabaseSO heroDatabase,
            HeroProgressionService service,
            HeroUIIconConfigSO heroUIIconConfig)
        {
            if (hero == null || heroDatabase == null || service == null)
                return null;

            var progressionConfig = heroDatabase.GetProgressionConfig(hero.Id);
            bool isAcquired = service.HasHero(hero.Id);

            HeroProgressTier displayTier = HeroProgressTier.Common;

            if (isAcquired)
            {
                var owned = service.GetOrCreateOwnedHero(hero.Id);
                displayTier = owned.CurrentTier;
            }
            else if (progressionConfig != null)
            {
                displayTier = progressionConfig.StartingTier;
            }
            
            var viewData = new HeroCollectionItemViewData
            {
                HeroId = hero.Id,
                HeroName = hero.Name,
                SummonRarity = hero.SummonRarity,
                PortraitIcon = HeroImageService.GetHeroIcon(hero),
                ShardIcon = hero.ShardIcon,
                RarityIcon = HeroImageService.GetHeroTierIcon(displayTier),
                ElementIcon = heroUIIconConfig != null ? heroUIIconConfig.GetElementIcon(hero.Element) : null,
                HeroClassIcon = heroUIIconConfig != null ? heroUIIconConfig.GetHeroClassIcon(hero.HeroClass) : null,
                BgIcon = HeroImageService.GetHeroTierBackground(displayTier),
                FrameIcon = HeroImageService.GetHeroTierFrame(displayTier),
                IsAcquired = isAcquired,
                Element = hero.Element,
                HeroClass = hero.HeroClass,
                DisplayTier = displayTier
            };

            if (!viewData.IsAcquired)
            {
                int maxStarAtStartingTier = progressionConfig != null
                    ? progressionConfig.GetMaxStarInTier(displayTier)
                    : 0;

                // Hero chưa unlock vẫn có thể đã tích shard (server) hướng tới mốc đầu tiên.
                var startingNode = progressionConfig != null
                    ? progressionConfig.GetNode(progressionConfig.StartingTier, progressionConfig.StartingStarInTier)
                    : null;
                int currentShard = service.GetOrCreateOwnedHero(hero.Id).CurrentShard;
                int requiredShard = startingNode != null ? startingNode.ShardCostToNext : 0;

                viewData.CurrentStarInTier = 0;
                viewData.MaxStarInTier = maxStarAtStartingTier;
                viewData.CurrentShard = currentShard;
                viewData.RequiredShardToNext = requiredShard;
                viewData.ProgressNormalized = requiredShard <= 0
                    ? 0f
                    : Mathf.Clamp01((float)currentShard / requiredShard);
                viewData.IsMaxNode = false;

                return viewData;
            }

            var ownedData = service.GetOrCreateOwnedHero(hero.Id);
            var currentNode = service.GetCurrentNode(hero.Id);
            int maxStar = service.GetMaxStarInCurrentTier(hero.Id);

            viewData.CurrentStarInTier = ownedData.CurrentStarInTier;
            viewData.MaxStarInTier = maxStar;
            viewData.CurrentShard = ownedData.CurrentShard;
            viewData.IsMaxNode = currentNode == null || currentNode.IsMaxNode;

            if (currentNode == null || currentNode.IsMaxNode)
            {
                viewData.RequiredShardToNext = 0;
                viewData.ProgressNormalized = 1f;
            }
            else
            {
                viewData.RequiredShardToNext = currentNode.ShardCostToNext;
                viewData.ProgressNormalized = currentNode.ShardCostToNext <= 0
                    ? 0f
                    : Mathf.Clamp01((float)ownedData.CurrentShard / currentNode.ShardCostToNext);
            }

            return viewData;
        }

        public static int Sort(
            HeroCollectionItemViewData a,
            HeroCollectionItemViewData b)
        {
            if (a.IsInLineup != b.IsInLineup)
                return a.IsInLineup ? -1 : 1;

            if (a.IsAcquired != b.IsAcquired)
                return a.IsAcquired ? -1 : 1;

            int tierCompare = b.DisplayTier.CompareTo(a.DisplayTier);
            if (tierCompare != 0)
                return tierCompare;

            return a.HeroId.CompareTo(b.HeroId);
        }
    }
}