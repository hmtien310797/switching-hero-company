using Immortal_Switch.Scripts.SummonSystem.Shared.Data;

namespace Immortal_Switch.Scripts.SummonSystem.HeroSummon
{
    public interface IHeroSummonCurrencyGateway
    {
        int GetHeroTicket();
        int GetGem();

        bool CanSpendHeroTicket(int amount);
        bool CanSpendGem(int amount);

        void SpendHeroTicket(int amount);
        void SpendGem(int amount);
    }
    
    public interface ISummonRewardReceiver
    {
        void GrantReward(SummonRewardItem rewardItem);
        SummonRewardPreviewData GetRewardPreviewData(SummonCategory summonCategory);
        bool ClaimReward(int summonLevel, ISummonRewardReceiver rewardReceiver, SummonCategory summonCategory);
    }
}