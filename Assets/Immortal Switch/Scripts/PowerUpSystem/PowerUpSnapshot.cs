using System.Collections.Generic;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.PowerUpSystem
{
    public class PowerUpSnapshot
    {
        public readonly Dictionary<StatType, float> FlatAdds = new();
        public readonly Dictionary<StatType, float> BasePercents = new();

        public void Clear()
        {
            FlatAdds.Clear();
            BasePercents.Clear();
        }

        public void Add(PowerUpModifierData data)
        {
            var target = data.ValueKind == PowerUpValueKind.FlatAdd
                ? FlatAdds
                : BasePercents;

            if (target.ContainsKey(data.TargetStat))
                target[data.TargetStat] += data.Value;
            else
                target[data.TargetStat] = data.Value;
        }

        public float GetFlat(StatType stat)
        {
            return FlatAdds.TryGetValue(stat, out var value) ? value : 0f;
        }

        public float GetPercentOfBase(StatType stat)
        {
            return BasePercents.TryGetValue(stat, out var value) ? value : 0f;
        }
    }
}