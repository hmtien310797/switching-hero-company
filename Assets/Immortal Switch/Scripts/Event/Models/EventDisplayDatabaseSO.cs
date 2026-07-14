using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Immortal_Switch.Scripts.Event.Models
{
    [CreateAssetMenu(fileName = "EventDisplayDatabase", menuName = "ScriptableObjects/Event/DisplayDatabase")]
    public class EventDisplayDatabaseSO : ScriptableObject
    {
        public List<EventDisplayEntry> entries = new();
    }

    [Serializable]
    public class EventDisplayEntry
    {
        public int eventId;

        [PreviewField]
        public Sprite banner;
    }
}