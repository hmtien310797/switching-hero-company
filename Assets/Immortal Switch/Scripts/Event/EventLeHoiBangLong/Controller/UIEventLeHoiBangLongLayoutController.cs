using System;
using System.Collections.Generic;
using System.Linq;
using Immortal_Switch.Scripts.Event.EventLeHoiBangLong.Layout;
using Immortal_Switch.Scripts.Shared;
using UnityEngine;

namespace Immortal_Switch.Scripts.Event.EventLeHoiBangLong.Controller
{
    public enum EEventLeHoiBangLongLayoutType
    {
        /// <summary>
        /// main layout
        /// </summary>
        Main = 0,

        /// <summary>
        /// dang nhap moi ngay
        /// </summary>
        SevenDayLogin = 1,

        /// <summary>
        /// trieu hoi bang long
        /// </summary>
        Summon = 2,

        /// <summary>
        /// nhiem vu
        /// </summary>
        Mission = 3,
    }

    [Serializable]
    public class EventLeHoiBangLongLayoutItem
    {
        public EEventLeHoiBangLongLayoutType type;
        public GameObject layout;
    }

    public class UIEventLeHoiBangLongLayoutController : MonoBehaviour
    {
        [SerializeField]
        private List<EventLeHoiBangLongLayoutItem> layouts = new();

        // --- Private Fields ---
        private EventLeHoiBangLongLayoutItem _selectedLayout;
        private Action<EEventLeHoiBangLongLayoutType> _onChangeLayout;

        public void DisableLayouts()
        {
            foreach (var layout in layouts)
            {
                layout.layout.SetActive(false);
            }
        }

        public void Bind(Action<EEventLeHoiBangLongLayoutType> onChangeLayout)
        {
            _onChangeLayout = onChangeLayout;
        }

        public void ChangeLayout(EEventLeHoiBangLongLayoutType type)
        {
            if (_selectedLayout != null)
            {
                if (_selectedLayout.type == type)
                {
                    return;
                }

                _selectedLayout.layout.SetActive(false);
                _selectedLayout = null;
            }

            foreach (var layout in layouts)
            {
                if (layout.type == type)
                {
                    _selectedLayout = layout;
                    _selectedLayout.layout.SetActive(true);

                    BindLayout();
                    return;
                }
            }
        }

        public void BindLayout()
        {
            if (_selectedLayout == null)
            {
                return;
            }

            var remainTime = DateTime.Now.AddMinutes(30).TimeOfDay.TotalSeconds;

            switch (_selectedLayout.type)
            {
                case EEventLeHoiBangLongLayoutType.Main:
                {
                    var milestones = DatabaseManager.Instance.GetEventLHBLMilestone();

                    _selectedLayout.layout
                        .GetComponent<EventLeHoiBangLongMainLayout>()
                        .Bind(milestones, _onChangeLayout, remainTime);

                    break;
                }

                case EEventLeHoiBangLongLayoutType.SevenDayLogin:
                {
                    var sevenDayRewards = DatabaseManager.Instance.GetEventLHBLCheckIn();
                    var eventPointRewards = DatabaseManager.Instance.GetEventLHBLCheckInEventPointReward();

                    _selectedLayout.layout
                        .GetComponent<EventLeHoiBangLongSevenDayLoginLayout>()
                        .Bind(
                            sevenDayRewards,
                            eventPointRewards,
                            remainTime
                        );

                    break;
                }

                case EEventLeHoiBangLongLayoutType.Summon:
                {
                    var maxPoint = DatabaseManager.Instance.GetEventLHBLMilestone().LastOrDefault()?.pointsRequired ?? 1;

                    _selectedLayout.layout
                        .GetComponent<EventLeHoiBangLongSummonLayout>()
                        .Bind(ChangeLayout, maxPoint, remainTime);

                    break;
                }

                case EEventLeHoiBangLongLayoutType.Mission:
                {
                    var missions = DatabaseManager.Instance.GetEventLHBLMissions();
                    var milestones = DatabaseManager.Instance.GetEventLHBLMissionMilestones();

                    _selectedLayout.layout
                        .GetComponent<EventLeHoiBangLongMissionLayout>()
                        .Bind(missions, milestones);

                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}