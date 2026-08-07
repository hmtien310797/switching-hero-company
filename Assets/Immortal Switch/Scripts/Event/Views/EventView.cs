using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Event.EventBingo;
using Immortal_Switch.Scripts.Event.EventDice;
using Immortal_Switch.Scripts.Event.EventFishing;
using Immortal_Switch.Scripts.Event.EventLogin;
using Immortal_Switch.Scripts.Event.EventWheel;
using Immortal_Switch.Scripts.Event.Views.UI;
using Immortal_Switch.Scripts.Modules;
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
        private SimpleUIPool<UIEventItem> _pool;

        public override void OnShow(object args)
        {
            base.OnShow(args);

            var activities = DatabaseManager.Instance.GetEventActives(EEventDisplayMode.Popup);

            RefreshItems(activities);
        }

        private void RefreshItems(List<DynamicHeroesGlobalSpecificationsConfigEventRow> activities)
        {
            _pool ??= new SimpleUIPool<UIEventItem>(eventPrefab, eventContainer);

            for (int i = 0; i < activities.Count; i++)
            {
                var activity = activities[i];
                var icon = ModuleManager.EventAtlas.LoadIcon(activity.eventIcon);
                var clone = _pool.Get(i);

                clone.Bind(icon, activity.eventKey, activity.eventId, OnClickEvent);
            }

            _pool.ReleaseFrom(activities.Count);
        }

        private void OnClickEvent(int eventId)
        {
            UIManager.Instance.Close<EventView>();

            switch (eventId)
            {
                case EventIdConstants.EVENT_WHEEL:
                    UIManager.Instance.OpenPopupAsync<EventWheelView>().Forget();
                    break;

                case EventIdConstants.EVENT_NEWBIE_7:
                case EventIdConstants.EVENT_NEWBIE_30:
                    UIManager.Instance.OpenPopupAsync<EventLoginView>(new EventLoginArgs(eventId)).Forget();
                    break;

                case EventIdConstants.EVENT_DICE:
                    UIManager.Instance.OpenPopupAsync<EventDiceView>().Forget();
                    break;

                case EventIdConstants.EVENT_FISHING:
                    UIManager.Instance.OpenPopupAsync<EventFishingView>().Forget();
                    break;

                case EventIdConstants.EVENT_BINGO:
                    UIManager.Instance.OpenPopupAsync<EventBingoView>().Forget();
                    break;
            }
        }
    }
}