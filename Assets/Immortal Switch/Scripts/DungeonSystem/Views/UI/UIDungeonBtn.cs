using Immortal_Switch.Scripts.DungeonSystem.Models;
using Immortal_Switch.Scripts.Shared.UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.DungeonSystem.Views.UI
{
    public class UIDungeonBtn : UITabPreset
    {
        [Header("Config")]
        [field: SerializeField]
        public int DungeonId { get; private set; }
    }
}