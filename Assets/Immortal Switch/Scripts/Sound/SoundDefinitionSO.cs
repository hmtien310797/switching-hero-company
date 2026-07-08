using System;
using UnityEngine;

namespace Immortal_Switch.Scripts.Sound
{
    [CreateAssetMenu(
        fileName = "SoundDefinitionSO",
        menuName = "Immortal Switch/Sound/Sound Definition Addressable")]
    public class SoundDefinitionSO : ScriptableObject
    {
        [Header("Library")]
        public SoundLibrarySO soundLibrary;

        [Header("Scene BGM")]
        public SceneBgmEntry[] sceneBgms;

        [Header("SFX Pool")]
        [Min(1)] public int initialSfxSourceCount = 12;
        [Min(1)] public int maxSfxSourceCount = 32;

        [Header("Default Volume")]
        [Range(0f, 1f)] public float defaultBgmVolume = 1f;
        [Range(0f, 1f)] public float defaultSfxVolume = 1f;

        [Header("Cross Fade")]
        [Min(0f)] public float defaultBgmFadeDuration = 1f;

        [Header("BGM Ducking")]
        [Range(0f, 1f)] public float duckingVolumeMultiplier = 0.45f;
        [Min(0f)] public float duckingFadeDuration = 0.15f;
    }

    [Serializable]
    public class SceneBgmEntry
    {
        public string sceneName;
        public BgmId bgmId = BgmId.None;
    }
}
