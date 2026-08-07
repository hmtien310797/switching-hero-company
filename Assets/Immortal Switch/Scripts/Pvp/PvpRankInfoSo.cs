using System;
using Immortal_Switch.Scripts.Pvp.Models;
using UnityEngine;

namespace Immortal_Switch.Scripts.Pvp
{
    [CreateAssetMenu(menuName = "PvP/Pvp Rank Info Visual")]
    public class PvpRankInfoSo : ScriptableObject
    {
        public PvpRankInfoData[] RankInfoData;
        public PvpTierInfoData[] TierInfoData;

        public Sprite GetIconAtRank(int rank)
        {
            if(rank is < 1 or > 3) return null;
            return RankInfoData[rank-1].Icon;
        }
        
        public Sprite GetIconAtTier(PvpRankTier rankTier)
        {
            for (int i = 0; i < TierInfoData.Length; i++)
            {
                if (TierInfoData[i].Tier == rankTier)
                {
                    return TierInfoData[i].Icon;
                }
            }

            return null;
        }
        
        public Sprite GetIconAtTier(string rankTier)
        {
            for (int i = 0; i < TierInfoData.Length; i++)
            {
                if (TierInfoData[i].Tier.ToString().ToLower() == rankTier)
                {
                    return TierInfoData[i].Icon;
                }
            }

            return null;
        }
    }

    [Serializable]
    public struct PvpRankInfoData
    {
        public int Rank;
        public Sprite Icon;
    }

    [Serializable]
    public struct PvpTierInfoData
    {
        public PvpRankTier Tier;
        public Sprite Icon;
    }
}