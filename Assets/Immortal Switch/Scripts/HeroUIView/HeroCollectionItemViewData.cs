using System;
using Immortal_Switch.Scripts.Hero;
using UnityEngine;

namespace Immortal_Switch.Scripts.HeroUIView
{
    [Serializable]
    public class HeroCollectionItemViewData
    {
        public int HeroId;
        public string HeroName;

        public Sprite PortraitIcon;
        public Sprite ShardIcon;
        public Sprite RarityIcon;
        public Sprite ElementIcon;
        public Sprite HeroClassIcon;
        public Sprite BgIcon;
        public Sprite FrameIcon;

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

        public bool IsInLineup;
        public int LineupIdx;
    }
}