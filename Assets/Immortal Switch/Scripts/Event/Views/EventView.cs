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
    public enum EEventDisplayMode
    {
        All = -1,
        Popup = 1,
        FullScreen = 2,
    }

    public class EventView : AnimatedUIView
    {
        [SerializeField]
        private RectTransform eventContainer;

        [SerializeField]
        private UIEventItem eventPrefab;

        // --- Private Fields ---
        private List<UIEventItem> _activities = new();

        public override void OnShow(object args)
        {
            base.OnShow(args);

            var activities = DatabaseManager.Instance.GetEventActives(EEventDisplayMode.Popup);

            RefreshItems(activities);
        }

        private void RefreshItems(List<DynamicHeroesGlobalSpecificationsConfigEventRow> activities)
        {
            for (int i = 0; i < activities.Count; i++)
            {
                var activity = activities[i];
                var display = DatabaseManager.Instance.EventDisplayDb.entries.FirstOrDefault(v => v.eventId == activity.eventId);

                if (_activities.Count > i)
                {
                    var clone = _activities[i];
                    clone.gameObject.SetActive(true);
                    clone.Bind(display?.banner, activity.nameVi, activity.eventId, OnClickEvent);
                }
                else
                {
                    var clone = Instantiate(eventPrefab, eventContainer);
                    clone.Bind(display?.banner, activity.nameVi, activity.eventId, OnClickEvent);
                    _activities.Add(clone);
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