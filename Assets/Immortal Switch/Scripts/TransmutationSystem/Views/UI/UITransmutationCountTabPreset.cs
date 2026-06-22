using Immortal_Switch.Scripts.Shared.UI;

namespace Immortal_Switch.Scripts.TransmutationSystem.Views.UI
{
    public class UITransmutationCountTabPreset : UITabPreset
    {
        // --- Private Fields ---
        public int Count { get; private set; }

        public void BindCount(int count)
        {
            Count = count;
        }
    }
}