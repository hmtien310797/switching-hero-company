using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Modules.Analytics;
using Immortal_Switch.Scripts.Modules.Atlas;
using Immortal_Switch.Scripts.Modules.Atlas.Implementations;
using Immortal_Switch.Scripts.Modules.Cache.Analytics;
using Immortal_Switch.Scripts.Modules.Power.Services;
using Immortal_Switch.Scripts.Modules.Power.Services.Interfaces;
using Immortal_Switch.Scripts.Shared.Constants;
using Immortal_Switch.Scripts.UI;

namespace Immortal_Switch.Scripts.Modules
{
    public class ModuleManager : Singleton<ModuleManager>
    {
        public IPowerService PowerService { get; } = new PowerService();

        public static AtlasServiceBase ShopAtlas { get; private set; }
        public static GearAtlasService GearAtlas { get; private set; }
        public static AtlasServiceBase EventAtlas { get; private set; }
        public static AtlasServiceBase CurrencyAtlas { get; private set; }
        public static ItemTierVisualImageService ItemTierVisualAtlas { get; private set; }

        public override async UniTask InitializeAsync()
        {
            // Nạp cache tracking theo tài khoản — phải trước LoginDayService.Initialize (bước 5
            // của bootstrap) vì OnLoginNewDay sẽ gọi RegisterLogin/level tracking đọc cache này.
            AnalyticsTrackingCache.Instance.Init();

            // Subscribe event vòng đời cho analytics (OnLoginNewDay, OnAppResumed) + gửi af_login.
            AppsflyerService.Init();

            GearAtlas = AtlasServiceModule.Register(
                SpriteAtlasConstants.GEAR,
                atlasKey => new GearAtlasService(atlasKey)
            );

            ShopAtlas = AtlasServiceModule.Register(SpriteAtlasConstants.SHOP);
            EventAtlas = AtlasServiceModule.Register(SpriteAtlasConstants.EVENT);
            CurrencyAtlas = AtlasServiceModule.Register(SpriteAtlasConstants.CURRENCY);

            ItemTierVisualAtlas = AtlasServiceModule.Register(
                SpriteAtlasConstants.ITEM_TIER_VISUAL,
                atlasKey => new ItemTierVisualImageService(atlasKey)
            );

            var atlases = new List<AtlasServiceBase>
            {
                ShopAtlas,
                GearAtlas,
                EventAtlas,
                CurrencyAtlas,
                ItemTierVisualAtlas,
            };

            await UniTask.WhenAll(atlases.Select(v => v.InitializeAsync()));
        }
    }
}