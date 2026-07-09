using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Common;
using Immortal_Switch.Scripts.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immortal_Switch.Scripts.Sound
{
    public class SoundManager : Singleton<SoundManager>
    {
        [SerializeField] private SoundDefinitionSO soundDefinition;
        [SerializeField] private bool initializeOnAwake = true;

        private readonly List<AudioSource> sfxSources = new();
        private readonly Dictionary<int, AudioSource> loopSources = new();
        private readonly Dictionary<SoundId, float> lastPlayTimeBySoundId = new();
        private readonly Dictionary<SoundId, AudioClip> cachedSfxClips = new();
        private readonly HashSet<SoundId> loadingSfxIds = new();

        private AudioSource bgmSourceA;
        private AudioSource bgmSourceB;
        private AudioSource activeBgmSource;
        private AudioSource inactiveBgmSource;

        private AudioClip activeBgmClip;
        private AudioClip inactiveBgmClip;

        private int nextLoopHandleId = 1;
        private int currentBgmFadeVersion;
        private int currentDuckingVersion;

        private float bgmVolume = 1f;
        private float sfxVolume = 1f;
        private float currentBgmVolumeMultiplier = 1f;
        private float currentDuckingMultiplier = 1f;

        private bool bgmMuted;
        private bool sfxMuted;
        private bool suppressSfx;
        private bool initialized;

        private BgmId currentBgmId = BgmId.None;

        public float BgmVolume => bgmVolume;
        public float SfxVolume => sfxVolume;
        public bool IsBgmMuted => bgmMuted;
        public bool IsSfxMuted => sfxMuted;
        public BgmId CurrentBgmId => currentBgmId;

        private SoundLibrarySO Library =>
            soundDefinition != null ? soundDefinition.soundLibrary : null;
        

        protected override void Awake()
        {
            base.Awake();
            if (initializeOnAwake)
            {
                InitializeAsync().Forget();
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }
        
        public override async UniTask InitializeAsync()
        {
            if (initialized)
                return;

            bgmVolume = soundDefinition != null ? soundDefinition.defaultBgmVolume : 1f;
            sfxVolume = soundDefinition != null ? soundDefinition.defaultSfxVolume : 1f;

            CreateBgmSources();
            CreateInitialSfxSources();

            initialized = true;

            await PreloadMarkedSfxAsync();
            await PlayBgmBySceneName(SceneManager.GetActiveScene().name);
        }
        
        private void CreateBgmSources()
        {
            if (bgmSourceA != null)
                return;

            bgmSourceA = gameObject.AddComponent<AudioSource>();
            bgmSourceB = gameObject.AddComponent<AudioSource>();

            SetupBgmSource(bgmSourceA);
            SetupBgmSource(bgmSourceB);

            activeBgmSource = bgmSourceA;
            inactiveBgmSource = bgmSourceB;
        }

        private void SetupBgmSource(AudioSource source)
        {
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 0f;
        }

        private void CreateInitialSfxSources()
        {
            if (sfxSources.Count > 0)
                return;

            int count = soundDefinition != null
                ? soundDefinition.initialSfxSourceCount
                : 12;

            for (int i = 0; i < count; i++)
            {
                CreateSfxSource();
            }
        }

        private AudioSource CreateSfxSource()
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();

            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.volume = sfxVolume;
            source.pitch = 1f;

            sfxSources.Add(source);
            return source;
        }

        private async UniTask PreloadMarkedSfxAsync()
        {
            if (Library == null || Library.sfxClips == null)
                return;

            List<UniTask> tasks = new();

            for (int i = 0; i < Library.sfxClips.Length; i++)
            {
                SfxClipEntry entry = Library.sfxClips[i];

                if (!entry.preload || entry.id == SoundId.None)
                    continue;

                tasks.Add(PreloadSingleSfxAsync(entry.id));
            }

            if (tasks.Count > 0)
            {
                await UniTask.WhenAll(tasks);
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            suppressSfx = false;
            PlayBgmBySceneName(scene.name).Forget();
        }

        public async UniTask PlayBgmBySceneName(string sceneName, float fadeDuration = -1f)
        {
            if (soundDefinition == null || soundDefinition.sceneBgms == null)
                return;

            for (int i = 0; i < soundDefinition.sceneBgms.Length; i++)
            {
                SceneBgmEntry entry = soundDefinition.sceneBgms[i];

                if (entry == null || entry.sceneName != sceneName)
                    continue;

                await PlayBgm(entry.bgmId, fadeDuration);
                return;
            }
        }

        public async UniTask PlayBgm(BgmId bgmId, float fadeDuration = -1f)
        {
            if (bgmId == BgmId.None)
            {
                await StopBgm(fadeDuration);
                return;
            }

            if (Library == null)
                return;

            if (currentBgmId == bgmId && activeBgmSource != null && activeBgmSource.isPlaying)
                return;

            int fadeVersion = ++currentBgmFadeVersion;

            string key = Library.GetBgmKey(bgmId);

            if (string.IsNullOrWhiteSpace(key))
            {
                Debug.LogError($"[SoundManager] Missing BGM addressable key. BgmId={bgmId}");
                return;
            }

            AudioClip newClip = await AddressableSpawnService.LoadAudioClipAsync(key);

            if (fadeVersion != currentBgmFadeVersion)
            {
                if (newClip != null)
                    AddressableSpawnService.ReleaseAudioClip(newClip);

                return;
            }

            if (newClip == null)
                return;

            AudioClip oldActiveClip = activeBgmClip;
            AudioClip oldInactiveClip = inactiveBgmClip;

            currentBgmId = bgmId;
            currentBgmVolumeMultiplier = Library.GetBgmVolumeMultiplier(bgmId);

            if (currentBgmVolumeMultiplier <= 0f)
                currentBgmVolumeMultiplier = 1f;

            if (fadeDuration < 0f)
            {
                fadeDuration = soundDefinition != null
                    ? soundDefinition.defaultBgmFadeDuration
                    : 1f;
            }

            inactiveBgmClip = newClip;
            inactiveBgmSource.clip = newClip;
            inactiveBgmSource.volume = 0f;
            inactiveBgmSource.loop = true;
            inactiveBgmSource.Play();

            float targetVolume = GetCurrentBgmTargetVolume();

            await CrossFadeBgm(
                activeBgmSource,
                inactiveBgmSource,
                targetVolume,
                fadeDuration,
                fadeVersion);

            if (fadeVersion != currentBgmFadeVersion)
            {
                AddressableSpawnService.ReleaseAudioClip(newClip);
                return;
            }

            SwapBgmSources();

            activeBgmClip = newClip;
            inactiveBgmClip = null;

            ReleaseBgmClipIfValid(oldActiveClip, newClip);
            ReleaseBgmClipIfValid(oldInactiveClip, newClip);
        }

        public async UniTask StopBgm(float fadeDuration = -1f)
        {
            if (fadeDuration < 0f)
            {
                fadeDuration = soundDefinition != null
                    ? soundDefinition.defaultBgmFadeDuration
                    : 1f;
            }

            int fadeVersion = ++currentBgmFadeVersion;
            AudioClip oldActiveClip = activeBgmClip;
            AudioClip oldInactiveClip = inactiveBgmClip;

            await FadeOutBgm(activeBgmSource, fadeDuration, fadeVersion);

            if (fadeVersion != currentBgmFadeVersion)
                return;

            currentBgmId = BgmId.None;
            activeBgmClip = null;
            inactiveBgmClip = null;

            ReleaseBgmClipIfValid(oldActiveClip, null);
            ReleaseBgmClipIfValid(oldInactiveClip, null);
        }

        private async UniTask CrossFadeBgm(
            AudioSource from,
            AudioSource to,
            float targetVolume,
            float duration,
            int fadeVersion)
        {
            if (duration <= 0f)
            {
                from.Stop();
                from.clip = null;
                from.volume = 0f;
                to.volume = targetVolume;
                return;
            }

            float elapsed = 0f;
            float fromStartVolume = from.volume;

            while (elapsed < duration)
            {
                if (fadeVersion != currentBgmFadeVersion)
                    return;

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                from.volume = Mathf.Lerp(fromStartVolume, 0f, t);
                to.volume = Mathf.Lerp(0f, targetVolume, t);

                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            if (fadeVersion != currentBgmFadeVersion)
                return;

            from.Stop();
            from.clip = null;
            from.volume = 0f;
            to.volume = targetVolume;
        }

        private async UniTask FadeOutBgm(AudioSource source, float duration, int fadeVersion)
        {
            if (source == null)
                return;

            if (duration <= 0f)
            {
                source.Stop();
                source.clip = null;
                source.volume = 0f;
                return;
            }

            float elapsed = 0f;
            float startVolume = source.volume;

            while (elapsed < duration)
            {
                if (fadeVersion != currentBgmFadeVersion)
                    return;

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                source.volume = Mathf.Lerp(startVolume, 0f, t);

                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            if (fadeVersion != currentBgmFadeVersion)
                return;

            source.Stop();
            source.clip = null;
            source.volume = 0f;
        }

        private void SwapBgmSources()
        {
            AudioSource temp = activeBgmSource;
            activeBgmSource = inactiveBgmSource;
            inactiveBgmSource = temp;
        }

        public void PlaySfx(SoundId soundId, float volumeMultiplier = 1f, float pitch = 1f)
        {
            PlaySfxAsync(soundId, volumeMultiplier, pitch).Forget();
        }

        public async UniTask PlaySfxAsync(SoundId soundId, float volumeMultiplier = 1f, float pitch = 1f)
        {
            if (suppressSfx || soundId == SoundId.None || sfxMuted)
                return;

            AudioClip clip = await GetOrLoadSfxClipAsync(soundId);

            if (clip == null)
                return;

            float libraryVolumeMultiplier = Library != null
                ? Library.GetSfxVolumeMultiplier(soundId)
                : 1f;

            PlaySfxClip(clip, volumeMultiplier * libraryVolumeMultiplier, pitch);
        }

        public void PlaySfxRandomPitch(
            SoundId soundId,
            float volumeMultiplier = 1f,
            float minPitch = 0.95f,
            float maxPitch = 1.05f)
        {
            float pitch = Random.Range(minPitch, maxPitch);
            PlaySfx(soundId, volumeMultiplier, pitch);
        }

        public void PlaySfxThrottled(
            SoundId soundId,
            float cooldown,
            float volumeMultiplier = 1f,
            float pitch = 1f)
        {
            if (soundId == SoundId.None)
                return;

            if (lastPlayTimeBySoundId.TryGetValue(soundId, out float lastPlayTime))
            {
                if (Time.unscaledTime - lastPlayTime < cooldown)
                    return;
            }

            lastPlayTimeBySoundId[soundId] = Time.unscaledTime;
            PlaySfx(soundId, volumeMultiplier, pitch);
        }

        private void PlaySfxClip(AudioClip clip, float volumeMultiplier, float pitch)
        {
            if (clip == null || suppressSfx || sfxMuted)
                return;

            AudioSource source = GetAvailableSfxSource();

            if (source == null)
                return;

            source.clip = clip;
            source.loop = false;
            source.pitch = pitch;
            source.volume = sfxVolume * volumeMultiplier;
            source.Play();
        }

        public UniTask<SoundLoopHandle> PlayLoopSfxAsync(
            SoundId soundId,
            float volumeMultiplier = 1f,
            float pitch = 1f)
        {
            return PlayLoopSfxInternalAsync(soundId, volumeMultiplier, pitch);
        }

        private async UniTask<SoundLoopHandle> PlayLoopSfxInternalAsync(
            SoundId soundId,
            float volumeMultiplier,
            float pitch)
        {
            if (suppressSfx || soundId == SoundId.None || sfxMuted)
                return default;

            AudioClip clip = await GetOrLoadSfxClipAsync(soundId);

            if (clip == null)
                return default;

            AudioSource source = GetAvailableSfxSource();

            if (source == null)
                return default;

            float libraryVolumeMultiplier = Library != null
                ? Library.GetSfxVolumeMultiplier(soundId)
                : 1f;

            int handleId = nextLoopHandleId++;

            source.clip = clip;
            source.loop = true;
            source.pitch = pitch;
            source.volume = sfxVolume * volumeMultiplier * libraryVolumeMultiplier;
            source.Play();

            loopSources[handleId] = source;
            return new SoundLoopHandle(handleId, soundId, source);
        }

        public void StopLoopSfx(SoundLoopHandle handle)
        {
            if (!handle.IsValid)
                return;

            if (!loopSources.TryGetValue(handle.Id, out AudioSource source))
                return;

            if (source != handle.Source)
            {
                loopSources.Remove(handle.Id);
                return;
            }

            ResetSfxSource(source);
            loopSources.Remove(handle.Id);
        }

        public void StopAllSfx()
        {
            for (int i = 0; i < sfxSources.Count; i++)
            {
                ResetSfxSource(sfxSources[i]);
            }

            loopSources.Clear();
            lastPlayTimeBySoundId.Clear();
        }

        private void ResetSfxSource(AudioSource source)
        {
            if (source == null)
                return;

            source.Stop();
            source.clip = null;
            source.loop = false;
            source.pitch = 1f;
            source.volume = sfxVolume;
        }

        private AudioSource GetAvailableSfxSource()
        {
            for (int i = 0; i < sfxSources.Count; i++)
            {
                AudioSource source = sfxSources[i];

                if (!source.isPlaying)
                    return source;
            }

            int maxCount = soundDefinition != null
                ? soundDefinition.maxSfxSourceCount
                : 32;

            if (sfxSources.Count >= maxCount)
                return null;

            return CreateSfxSource();
        }

        private async UniTask<AudioClip> GetOrLoadSfxClipAsync(SoundId soundId)
        {
            if (Library == null)
                return null;

            if (cachedSfxClips.TryGetValue(soundId, out AudioClip cachedClip))
                return cachedClip;

            if (loadingSfxIds.Contains(soundId))
            {
                while (loadingSfxIds.Contains(soundId))
                {
                    await UniTask.Yield(PlayerLoopTiming.Update);
                }

                cachedSfxClips.TryGetValue(soundId, out cachedClip);
                return cachedClip;
            }

            string key = Library.GetSfxKey(soundId);

            if (string.IsNullOrWhiteSpace(key))
            {
                Debug.LogError($"[SoundManager] Missing SFX addressable key. SoundId={soundId}");
                return null;
            }

            loadingSfxIds.Add(soundId);
            AudioClip clip = await AddressableSpawnService.LoadAudioClipAsync(key);
            loadingSfxIds.Remove(soundId);

            if (clip == null)
                return null;

            cachedSfxClips[soundId] = clip;
            return clip;
        }


        private async UniTask PreloadSingleSfxAsync(SoundId soundId)
        {
            await GetOrLoadSfxClipAsync(soundId);
        }

        public async UniTask PreloadSfxAsync(params SoundId[] soundIds)
        {
            if (soundIds == null)
                return;

            List<UniTask> tasks = new();

            for (int i = 0; i < soundIds.Length; i++)
            {
                SoundId soundId = soundIds[i];

                if (soundId == SoundId.None)
                    continue;

                tasks.Add(PreloadSingleSfxAsync(soundId));
            }

            if (tasks.Count > 0)
                await UniTask.WhenAll(tasks);
        }

        public void ReleaseCachedSfx(SoundId soundId)
        {
            if (!cachedSfxClips.TryGetValue(soundId, out AudioClip clip))
                return;

            cachedSfxClips.Remove(soundId);
            AddressableSpawnService.ReleaseAudioClip(clip);
        }
        
        public void ReleaseCachedSfxCollection(SoundId[] soundIds)
        {
            for (int i = 0; i < soundIds.Length; i++)
            {
                SoundId currentSound = soundIds[i];
                if (!cachedSfxClips.TryGetValue(currentSound, out AudioClip clip))
                    return;

                cachedSfxClips.Remove(currentSound);
                AddressableSpawnService.ReleaseAudioClip(clip);
            }
        }

        public void ReleaseCachedSfxClips()
        {
            foreach (KeyValuePair<SoundId, AudioClip> pair in cachedSfxClips)
            {
                AddressableSpawnService.ReleaseAudioClip(pair.Value);
            }

            cachedSfxClips.Clear();
            loadingSfxIds.Clear();
        }

        public void SetBgmVolume(float value)
        {
            bgmVolume = Mathf.Clamp01(value);
            ApplyBgmVolumeImmediate();
        }

        public void SetSfxVolume(float value)
        {
            sfxVolume = Mathf.Clamp01(value);

            for (int i = 0; i < sfxSources.Count; i++)
            {
                AudioSource source = sfxSources[i];

                if (source == null)
                    continue;

                if (!source.isPlaying)
                    source.volume = sfxVolume;
            }
        }

        public void SetBgmMuted(bool muted)
        {
            bgmMuted = muted;
            ApplyBgmVolumeImmediate();
        }

        public void SetSfxMuted(bool muted)
        {
            sfxMuted = muted;

            if (sfxMuted)
                StopAllSfx();
        }

        public void SetSuppressSfx(bool suppress)
        {
            suppressSfx = suppress;
        }

        private void ApplyBgmVolumeImmediate()
        {
            if (activeBgmSource == null)
                return;

            activeBgmSource.volume = GetCurrentBgmTargetVolume();
        }

        private float GetCurrentBgmTargetVolume()
        {
            if (bgmMuted)
                return 0f;

            return bgmVolume * currentBgmVolumeMultiplier * currentDuckingMultiplier;
        }

        public async UniTask DuckBgm(float duration)
        {
            float duckMultiplier = soundDefinition != null
                ? soundDefinition.duckingVolumeMultiplier
                : 0.45f;

            float fadeDuration = soundDefinition != null
                ? soundDefinition.duckingFadeDuration
                : 0.15f;

            int duckVersion = ++currentDuckingVersion;

            await SetDuckingMultiplier(duckMultiplier, fadeDuration, duckVersion);

            if (duration > 0f)
            {
                await UniTask.Delay(
                    Mathf.RoundToInt(duration * 1000f),
                    ignoreTimeScale: true);
            }

            if (duckVersion != currentDuckingVersion)
                return;

            await SetDuckingMultiplier(1f, fadeDuration, duckVersion);
        }

        public async UniTask SetDuckingMultiplier(float targetMultiplier, float fadeDuration)
        {
            int duckVersion = ++currentDuckingVersion;
            await SetDuckingMultiplier(targetMultiplier, fadeDuration, duckVersion);
        }

        private async UniTask SetDuckingMultiplier(
            float targetMultiplier,
            float fadeDuration,
            int duckVersion)
        {
            targetMultiplier = Mathf.Clamp01(targetMultiplier);

            if (activeBgmSource == null)
            {
                currentDuckingMultiplier = targetMultiplier;
                return;
            }

            if (fadeDuration <= 0f)
            {
                currentDuckingMultiplier = targetMultiplier;
                ApplyBgmVolumeImmediate();
                return;
            }

            float elapsed = 0f;
            float startMultiplier = currentDuckingMultiplier;

            while (elapsed < fadeDuration)
            {
                if (duckVersion != currentDuckingVersion)
                    return;

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);

                currentDuckingMultiplier = Mathf.Lerp(startMultiplier, targetMultiplier, t);
                ApplyBgmVolumeImmediate();

                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            if (duckVersion != currentDuckingVersion)
                return;

            currentDuckingMultiplier = targetMultiplier;
            ApplyBgmVolumeImmediate();
        }

        public void PauseAllSound()
        {
            if (activeBgmSource != null)
                activeBgmSource.Pause();

            for (int i = 0; i < sfxSources.Count; i++)
            {
                AudioSource source = sfxSources[i];

                if (source != null && source.isPlaying)
                    source.Pause();
            }
        }

        public void ResumeAllSound()
        {
            if (activeBgmSource != null)
                activeBgmSource.UnPause();

            for (int i = 0; i < sfxSources.Count; i++)
            {
                AudioSource source = sfxSources[i];

                if (source != null)
                    source.UnPause();
            }
        }

        public void CleanupOnLogout()
        {
            suppressSfx = true;

            currentBgmFadeVersion++;
            currentDuckingVersion++;

            StopAllSfx();
            ReleaseCachedSfxClips();

            currentBgmId = BgmId.None;
            currentDuckingMultiplier = 1f;
            currentBgmVolumeMultiplier = 1f;

            if (activeBgmSource != null)
            {
                activeBgmSource.Stop();
                activeBgmSource.clip = null;
                activeBgmSource.volume = 0f;
            }

            if (inactiveBgmSource != null)
            {
                inactiveBgmSource.Stop();
                inactiveBgmSource.clip = null;
                inactiveBgmSource.volume = 0f;
            }

            ReleaseBgmClips();
        }

        private void ReleaseBgmClips()
        {
            ReleaseBgmClipIfValid(activeBgmClip, null);
            ReleaseBgmClipIfValid(inactiveBgmClip, null);

            activeBgmClip = null;
            inactiveBgmClip = null;
        }

        private void ReleaseBgmClipIfValid(AudioClip clip, AudioClip keepClip)
        {
            if (clip == null || clip == keepClip)
                return;

            AddressableSpawnService.ReleaseAudioClip(clip);
        }
    }
}
