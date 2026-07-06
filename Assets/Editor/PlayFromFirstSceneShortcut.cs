#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.ShortcutManagement;
using UnityEngine;

public static class SceneShortcutEditor
{
    [Shortcut("Game/Play From First Scene", KeyCode.Q, ShortcutModifiers.Control)]
    private static void PlayFromFirstScene()
    {
        OpenSceneByIndex(0, true);
    }

    [Shortcut("Game/Open First Scene", KeyCode.W, ShortcutModifiers.Control)]
    private static void OpenFirstScene()
    {
        OpenSceneByIndex(0, false);
    }

    [Shortcut("Game/Open Last Scene", KeyCode.E, ShortcutModifiers.Control)]
    private static void OpenLastScene()
    {
        var scenes = EditorBuildSettings.scenes;

        if (scenes == null || scenes.Length == 0)
        {
            Debug.LogError("Không tìm thấy scene nào trong Build Settings.");
            return;
        }

        int lastEnabledSceneIndex = -1;

        for (int i = scenes.Length - 1; i >= 0; i--)
        {
            if (scenes[i].enabled)
            {
                lastEnabledSceneIndex = i;
                break;
            }
        }

        if (lastEnabledSceneIndex < 0)
        {
            Debug.LogError("Không có scene nào được enable trong Build Settings.");
            return;
        }

        OpenSceneByIndex(lastEnabledSceneIndex, false);
    }

    private static void OpenSceneByIndex(int sceneIndex, bool playAfterOpen)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        var scenes = EditorBuildSettings.scenes;

        if (scenes == null || scenes.Length == 0)
        {
            Debug.LogError("Không tìm thấy scene nào trong Build Settings.");
            return;
        }

        if (sceneIndex < 0 || sceneIndex >= scenes.Length)
        {
            Debug.LogError($"Scene index không hợp lệ: {sceneIndex}");
            return;
        }

        var scene = scenes[sceneIndex];

        if (!scene.enabled)
        {
            Debug.LogError($"Scene đang bị disable trong Build Settings: {scene.path}");
            return;
        }

        bool shouldContinue = EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        if (!shouldContinue)
            return;

        EditorSceneManager.OpenScene(scene.path);

        Debug.Log(playAfterOpen
            ? $"Open and play scene: {scene.path}"
            : $"Open scene: {scene.path}");

        if (!playAfterOpen)
            return;

        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.isPlaying = true;
            }
        };
    }
}
#endif