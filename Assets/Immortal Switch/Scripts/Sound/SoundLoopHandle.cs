using UnityEngine;

namespace Immortal_Switch.Scripts.Sound
{
    public readonly struct SoundLoopHandle
    {
        public readonly int Id;
        public readonly SoundId SoundId;
        public readonly AudioSource Source;

        public bool IsValid => Id > 0 && Source != null;

        public SoundLoopHandle(int id, SoundId soundId, AudioSource source)
        {
            Id = id;
            SoundId = soundId;
            Source = source;
        }
    }
}
