using System;
using UnityEngine;

namespace Immortal_Switch.Scripts.Sound
{
    [CreateAssetMenu(
        fileName = "SoundLibrarySO",
        menuName = "Immortal Switch/Sound/Sound Library Addressable")]
    public class SoundLibrarySO : ScriptableObject
    {
        [Header("BGM Addressable Keys")]
        public BgmClipEntry[] bgmClips;

        [Header("SFX Addressable Keys")]
        public SfxClipEntry[] sfxClips;

        public string GetBgmKey(BgmId id)
        {
            if (id == BgmId.None || bgmClips == null)
                return string.Empty;

            for (int i = 0; i < bgmClips.Length; i++)
            {
                if (bgmClips[i].id == id)
                    return bgmClips[i].addressableKey;
            }

            return string.Empty;
        }

        public string GetSfxKey(SoundId id)
        {
            if (id == SoundId.None || sfxClips == null)
                return string.Empty;

            for (int i = 0; i < sfxClips.Length; i++)
            {
                if (sfxClips[i].id == id)
                    return sfxClips[i].addressableKey;
            }

            return string.Empty;
        }

        public float GetBgmVolumeMultiplier(BgmId id)
        {
            if (id == BgmId.None || bgmClips == null)
                return 1f;

            for (int i = 0; i < bgmClips.Length; i++)
            {
                if (bgmClips[i].id == id)
                    return Mathf.Max(0f, bgmClips[i].volumeMultiplier);
            }

            return 1f;
        }

        public float GetSfxVolumeMultiplier(SoundId id)
        {
            if (id == SoundId.None || sfxClips == null)
                return 1f;

            for (int i = 0; i < sfxClips.Length; i++)
            {
                if (sfxClips[i].id == id)
                    return Mathf.Max(0f, sfxClips[i].volumeMultiplier);
            }

            return 1f;
        }
    }

    [Serializable]
    public struct BgmClipEntry
    {
        public BgmId id;
        public string addressableKey;

        [Range(0f, 1f)]
        public float volumeMultiplier;
    }

    [Serializable]
    public struct SfxClipEntry
    {
        public SoundId id;
        public string addressableKey;

        [Range(0f, 1f)]
        public float volumeMultiplier;

        [Tooltip("Nếu bật, SoundManager sẽ preload sound này khi InitializeAsync được gọi.")]
        public bool preload;
    }
}
