using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.SummonSystem.Shared.Data;

namespace Immortal_Switch.Scripts.SummonSystem.HeroSummon
{
    public interface IHeroSummonCurrencyGateway
    {
        BigNumber GetHeroTicket();
        BigNumber GetGem();

        bool CanSpendHeroTicket(BigNumber amount);
        bool CanSpendGem(int amount);

        void SpendHeroTicket(BigNumber amount);
        void SpendGem(int amount);
    }
    
    public interface ISummonRewardReceiver
    {
        void GrantReward(SummonRewardItem rewardItem);
        SummonRewardPreviewData GetRewardPreviewData(SummonCategory summonCategory);
        UniTask ClaimReward(int summonLevel, ISummonRewardReceiver rewardReceiver, SummonCategory summonCategory);
    }
}