using UnityEngine;

namespace Immortal_Switch.Scripts.Modules.Atlas.Implementations
{
    public class GearAtlasService : AtlasServiceBase
    {
        public GearAtlasService(string atlasKey) : base(atlasKey)
        {
        }

        public Sprite LoadSprite(string gearType, string tier)
        {
            var iconKey = $"ic_gear_{gearType}_{tier}".ToLower();
            Debug.Log($"GearAtlasService: {iconKey}");
            return LoadSprite(iconKey);
        }
    }
}