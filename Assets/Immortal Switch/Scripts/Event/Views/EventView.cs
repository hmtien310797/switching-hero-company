using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Event.EventWheel;
using Immortal_Switch.Scripts.Event.Views.UI;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.Constants;
using Immortal_Switch.Scripts.UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.Event.Views
{
    public class EventView : AnimatedUIView
    {
        [SerializeField]
        private RectTransform eventContainer;

        [SerializeField]
        private UIEventItem eventPrefab;

        // --- Private Fields ---
        private List<UIEventItem> _events = new();

        public override void OnShow(object args)
        {
            base.OnShow(args);

            var eventActives = DatabaseManager.Instance.GetEventActives();
            RefreshItems(eventActives);
        }

        private void RefreshItems(List<DynamicHeroesGlobalSpecificationsConfigEventRow> events)
        {
            for (int i = 0; i < events.Count; i++)
            {
                var evt = events[i];
                var display = DatabaseManager.Instance.EventDisplayDb.entries.FirstOrDefault(v => v.eventId == evt.eventId);

                if (_events.Count > i)
                {
                    var clone = _events[i];
                    clone.gameObject.SetActive(true);
                    clone.Bind(display?.banner, evt.nameVi, evt.eventId, OnClickEvent);
                }
                else
                {
                    var clone = Instantiate(eventPrefab, eventContainer);
                    clone.Bind(display?.banner, evt.nameVi, evt.eventId, OnClickEvent);
                    _events.Add(clone);
                }
            }
        }

        private void OnClickEvent(int eventId)
        {
            UIManager.Instance.Close<EventView>();

            switch (eventId)
            {
                case EventIdConstants.EVENT_WHEEL:
                    UIManager.Instance.OpenPopupAsync<EventWheelView>().Forget();
                    break;
            }
        }
    }
}