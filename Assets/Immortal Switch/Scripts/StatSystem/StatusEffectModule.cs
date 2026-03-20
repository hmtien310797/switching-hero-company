using System;

namespace Immortal_Switch.Scripts.StatSystem
{
    public class StatusEffectModule
    {
        public event Action<StatusEffectType> OnStatusChanged;

        private StatusEffectType currentStatus = StatusEffectType.None;

        public StatusEffectType CurrentStatus => currentStatus;

        public bool HasStatus(StatusEffectType type)
        {
            return (currentStatus & type) != 0;
        }

        public void AddStatus(StatusEffectType type)
        {
            var old = currentStatus;
            currentStatus |= type;

            if (old != currentStatus)
                OnStatusChanged?.Invoke(currentStatus);
        }

        public void RemoveStatus(StatusEffectType type)
        {
            var old = currentStatus;
            currentStatus &= ~type;

            if (old != currentStatus)
                OnStatusChanged?.Invoke(currentStatus);
        }

        public void ClearAll()
        {
            if (currentStatus == StatusEffectType.None)
                return;

            currentStatus = StatusEffectType.None;
            OnStatusChanged?.Invoke(currentStatus);
        }
    }
}