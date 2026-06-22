using Immortal_Switch.Scripts.Shared.UI;

namespace Immortal_Switch.Scripts.TransmutationSystem.Views.UI
{
    public class UITransmutationUniqueOptionEntry : UITabPreset
    {
        // --- Private Fields ---
        public string CurrentStat { get; private set; }

        public void BindStat(string newStat)
        {
            CurrentStat = newStat;
        }

        public bool IsMatch(string stat)
        {
            return !string.IsNullOrWhiteSpace(CurrentStat) && CurrentStat == stat;
        }
    }
}