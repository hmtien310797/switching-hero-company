namespace Immortal_Switch.Scripts.GrowthSystem
{
    public static class GrowthDebugFormatter
    {
        public static string FormatPercent(float value)
        {
            return $"{value * 100f:0.##}%";
        }
    }
}