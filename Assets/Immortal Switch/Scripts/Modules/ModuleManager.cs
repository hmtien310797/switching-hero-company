using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Modules.Atlas;
using Immortal_Switch.Scripts.Modules.Atlas.Implementations;
using Immortal_Switch.Scripts.Modules.Power.Services;
using Immortal_Switch.Scripts.Modules.Power.Services.Interfaces;
using Immortal_Switch.Scripts.Shared.Constants;

namespace Immortal_Switch.Scripts.Modules
{
    public class ModuleManager : Singleton<ModuleManager>
    {
        public IPowerService PowerService { get; } = new PowerService();

        public static AtlasServiceBase ShopAtlas { get; private set; }
        public static GearAtlasService GearAtlas { get; private set; }
        public static AtlasServiceBase EventAtlas { get; private set; }
        public static AtlasServiceBase CurrencyAtlas { get; private set; }

        public override async UniTask InitializeAsync()
        {
            GearAtlas = AtlasServiceModule.Register(
                SpriteAtlasConstants.GEAR,
                atlasKey => new GearAtlasService(atlasKey)
            );

            ShopAtlas = AtlasServiceModule.Register(SpriteAtlasConstants.SHOP);
            EventAtlas = AtlasServiceModule.Register(SpriteAtlasConstants.EVENT);
            CurrencyAtlas = AtlasServiceModule.Register(SpriteAtlasConstants.CURRENCY);

            var atlases = new List<AtlasServiceBase>
            {
                ShopAtlas,
                GearAtlas,
                EventAtlas,
                CurrencyAtlas,
            };

            await UniTask.WhenAll(atlases.Select(v => v.InitializeAsync()));
        }
    }
}