using System;
using System.Collections.Generic;
using Battle.Dungeon.Editor;
using Immortal_Switch.Editor.GameData;
using Immortal_Switch.Scripts.Equipment.Editor;
using Immortal_Switch.Scripts.Hero.Editor;
using Immortal_Switch.Scripts.Pvp.Editor;
using UnityEditor;
using UnityEngine;

namespace Editor.ExcelConfigTool.Services
{
    /// <summary>
    /// Chạy toàn bộ các importer "Tools/Game Data" (và Weapons) theo thứ tự,
    /// mỗi bước một lúc, dừng lại ngay khi có bước thất bại.
    ///
    /// Sau khi tất cả importer hoàn tất sẽ gọi onFinished(success).
    /// Excel Config Tool sync được chạy SAU (ngoài coordinator) vì nó có thể
    /// regenerate scripts và trigger domain reload - coordinator không nên sống
    /// xuyên qua domain reload.
    /// </summary>
    public static class GameDataSyncCoordinator
    {
        public static bool IsRunning { get; private set; }
        public static string CurrentStep { get; private set; }

        private readonly struct Step
        {
            public readonly string Name;
            public readonly Func<EditorWindow> Open;
            public readonly Action<EditorWindow> Run;
            public readonly Func<EditorWindow, bool> IsRunning;
            public readonly Func<EditorWindow, bool> LastFailed;

            public Step(
                string name,
                Func<EditorWindow> open,
                Action<EditorWindow> run,
                Func<EditorWindow, bool> isRunning,
                Func<EditorWindow, bool> lastFailed)
            {
                Name = name;
                Open = open;
                Run = run;
                IsRunning = isRunning;
                LastFailed = lastFailed;
            }
        }

        private static readonly List<Step> Steps = new();
        private static int _currentIndex;
        private static Step _currentStep;
        private static EditorWindow _window;
        private static bool _polling;
        private static Action<bool> _onFinished;

        public static void Start(Action<bool> onFinished)
        {
            if (IsRunning)
            {
                return;
            }

            _onFinished = onFinished;
            BuildSteps();
            _currentIndex = 0;
            IsRunning = true;

            EditorApplication.update += Tick;
            EditorApplication.delayCall += StartNextStep;
        }

        public static void Cancel()
        {
            if (!IsRunning)
            {
                return;
            }

            Debug.Log("[GameDataSync] Cancelled by user.");
            Stop(false);
        }

        private static void BuildSteps()
        {
            Steps.Clear();

            AddStep<HeroDataCsvImporterWindow>("Hero Data");
            AddStep<BossDataCsvImporterWindow>("Boss Data");
            AddStep<CreepDataCsvImporterWindow>("Creep Data");
            AddStep<DungeonGoogleSheetImporterWindow>("Dungeon");
            AddStep<SkillDataGoogleSheetImporterWindow>("Skill Data");
            AddStep<HeroProgressionGoogleSheetImporterWindow>("Hero Progression");
            AddStep<PvpTierRewardGoogleSheetImporterWindow>("PvP Tier Reward");
            AddStep<StageConfigCsvImporterWindow>("Stage Chapter Config");
            AddStep<HeroSummonLevelImporterWindow>("Hero Summon Level");
            AddStep<SkillSummonLevelImporterWindow>("Skill Summon Level");
            AddStep<WeaponSummonLevelImporterWindow>("Weapon Summon Level");
            AddStep<WeaponCsvImporterWindow>("Weapon Data");
        }

        private static void AddStep<T>(string name)
            where T : EditorWindow, IGameDataSyncStep
        {
            Steps.Add(new Step(
                name,
                () => GetWindow<T>(),
                window => ((T)window).RunForBatch(),
                window => ((T)window).IsRunning,
                window => ((T)window).LastImportFailed
            ));
        }

        private static T GetWindow<T>()
            where T : EditorWindow
        {
            return EditorWindow.GetWindow<T>(false);
        }

        private static void StartNextStep()
        {
            if (!IsRunning)
            {
                return;
            }

            if (_currentIndex >= Steps.Count)
            {
                Stop(true);
                return;
            }

            _currentStep = Steps[_currentIndex];
            CurrentStep = _currentStep.Name;
            Debug.Log($"[GameDataSync] --- Step {_currentIndex + 1}/{Steps.Count}: {CurrentStep} ---");

            _window = _currentStep.Open();

            try
            {
                _currentStep.Run(_window);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameDataSync] '{CurrentStep}' failed to start:\n{e}");
                FinishStepFailed();
                return;
            }

            if (_currentStep.IsRunning(_window))
            {
                _polling = true;
            }
            else
            {
                // Step đồng bộ (vd StageConfig) đã chạy xong ngay trong Run().
                FinishStep();
            }
        }

        private static void Tick()
        {
            if (!IsRunning || !_polling)
            {
                return;
            }

            if (_currentStep.IsRunning(_window))
            {
                return;
            }

            _polling = false;
            FinishStep();
        }

        private static void FinishStep()
        {
            if (_currentStep.LastFailed(_window))
            {
                FinishStepFailed();
                return;
            }

            Debug.Log($"[GameDataSync] '{CurrentStep}' completed.");

            CloseWindow();
            _currentIndex++;
            EditorApplication.delayCall += StartNextStep;
        }

        private static void FinishStepFailed()
        {
            string failedName = CurrentStep;

            Debug.LogError($"[GameDataSync] '{failedName}' failed. Batch stopped.");

            CloseWindow();
            EditorUtility.DisplayDialog(
                "Game Data Sync",
                $"'{failedName}' failed.\n\nBatch đã dừng lại. Xem Console để biết chi tiết.",
                "OK"
            );

            Stop(false);
        }

        private static void CloseWindow()
        {
            if (_window != null)
            {
                _window.Close();
            }

            _window = null;
            _currentStep = default;
        }

        private static void Stop(bool success)
        {
            CloseWindow();

            IsRunning = false;
            _polling = false;
            _currentIndex = 0;
            CurrentStep = string.Empty;

            EditorApplication.update -= Tick;

            var callback = _onFinished;
            _onFinished = null;
            callback?.Invoke(success);
        }
    }
}