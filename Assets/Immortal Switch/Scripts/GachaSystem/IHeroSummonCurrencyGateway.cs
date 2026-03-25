namespace Immortal_Switch.Scripts.GachaSystem
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
    
    public interface IHeroSummonRewardReceiver
    {
        void GrantReward(HeroSummonRewardItem rewardItem);
    }
}