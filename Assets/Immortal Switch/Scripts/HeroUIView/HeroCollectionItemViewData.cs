using Immortal_Switch.Hero;
using UnityEngine;

namespace Immortal_Switch.Scripts.HeroUIView
{
    public class HeroCollectionItemViewData
    {
        public int HeroId;
        public string HeroName;

        public Sprite PortraitIcon;
        public Sprite ShardIcon;
        public Sprite RarityIcon;
        public Sprite ElementIcon;
        public Sprite HeroClassIcon;

        public bool IsAcquired;

        public SummonRarity SummonRarity;
        public Element Element;
        public HeroClass HeroClass;

        public HeroProgressTier DisplayTier;
        public int CurrentStarInTier;
        public int MaxStarInTier;

        public int CurrentShard;
        public int RequiredShardToNext;
        public float ProgressNormalized;

        public bool IsMaxNode;
    }
}