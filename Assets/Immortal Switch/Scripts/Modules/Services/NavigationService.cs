using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.DungeonSystem.Views;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.HeroUIView;
using Immortal_Switch.Scripts.MissionSystem;
using Immortal_Switch.Scripts.SummonSystem.Shared.UI;
using Immortal_Switch.Scripts.TransmutationSystem.Views;
using Immortal_Switch.Scripts.UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.Modules.Services
{
    /// <summary>
    /// Điều hướng đến giao diện tương ứng với từng event key.
    /// </summary>
    public static class NavigationService
    {
        /// <summary>
        /// Mở giao diện phù hợp với event key.
        /// Trả về true nếu event key đã có route hoặc thuộc gameplay trực tiếp.
        /// </summary>
        public static async UniTask<bool> JumpToAsync(string eventKey)
        {
            GameEventManager.Trigger(GameEvents.ON_NAVIGATION_REQUESTED, eventKey);

            switch (eventKey)
            {
                case EventKeys.EVENT_HERO_SUMMON:
                    await UIManager.Instance.TogglePopupAsync<SummonHubView>();
                    return true;

                case EventKeys.EVENT_HERO_LEVELUP:
                case EventKeys.EVENT_OWN_HERO:
                case EventKeys.EVENT_REACH_POWER:
                    await UIManager.Instance.TogglePopupAsync<HeroCollectionView>();
                    return true;

                case EventKeys.EVENT_EQUIP_ITEM:
                case EventKeys.EVENT_ENHANCE_GEAR:
                case EventKeys.EVENT_FORGE_GEAR:
                    await UIManager.Instance.TogglePopupAsync<EquipView>(
                        new EquipViewData
                        {
                            Type = EquipViewType.WeaponView,
                        });

                    return true;

                case EventKeys.EVENT_SKILL_UPGRADE:
                    if (TopMainView.Instance.HeroSkillBarUI?.CurrentHero != null)
                    {
                        await UIManager.Instance.TogglePopupAsync<EquipView>(
                            new EquipViewData
                            {
                                Type = EquipViewType.SkillView,
                                Data1 = TopMainView.Instance.HeroSkillBarUI.CurrentHero.HeroClass,
                            });

                        return true;
                    }

                    return false;

                case EventKeys.EVENT_DUNGEON_CLEAR:
                    await UIManager.Instance.TogglePopupAsync<DungeonMainView>();
                    return true;

                case EventKeys.EVENT_TRANSMUTE_GEAR:
                    await UIManager.Instance.TogglePopupAsync<TransmutationSystemView>();
                    return true;

                // Các nhiệm vụ này được thực hiện trực tiếp tại gameplay,
                // chỉ cần đóng màn hình nhiệm vụ hiện tại.
                case EventKeys.EVENT_CLEAR_STAGE:
                case EventKeys.EVENT_KILL_MONSTER:
                case EventKeys.EVENT_KILL_BOSS:
                case EventKeys.EVENT_LOGIN:
                case EventKeys.EVENT_CLAIM_IDLE:
                case EventKeys.EVENT_COMPLETE_DAILY:
                case EventKeys.EVENT_COMPLETE_WEEKLY:
                case EventKeys.EVENT_REACH_LEVEL:
                case EventKeys.EVENT_USE_GOLD:
                    return true;

                default:
                    Debug.LogWarning($"[NavigationService] Chưa cấu hình giao diện cho event key: {eventKey}");
                    return false;
            }
        }
    }
}