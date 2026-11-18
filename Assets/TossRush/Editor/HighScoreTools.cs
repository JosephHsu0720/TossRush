using UnityEditor;
using UnityEngine;

namespace TossRush.Editor
{
    public static class HighScoreTools
    {
        [MenuItem("TossRush/Open Prototype")]
        public static void OpenPrototype()
        {
            if (!Application.isPlaying)
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TossRush/Scenes/TossRush.unity");
        }

        public static void ClearHighScore()
        {
            if (!CanModifyRecords) return;
            // Preserve eligibility from older saves before deleting their qualifying score.
            if (PlayerPrefs.GetInt(GameDirector.BestKey, 0) >= GameDirector.SpecialUnlockScore)
                PlayerPrefs.SetInt(GameDirector.SpecialUnlockKey, 1);
            PlayerPrefs.DeleteKey(GameDirector.BestKey);
            PlayerPrefs.DeleteKey(GameDirector.SpecialBestKey);
            PlayerPrefs.Save();
            Debug.Log("Let's Toss and Ring Toss high scores cleared. Special mode access preserved.");
        }

        public static void ResetSpecialModeUnlock()
        {
            if (!CanModifyRecords) return;
            // Clear the qualifying score too, otherwise it immediately unlocks again.
            PlayerPrefs.DeleteKey(GameDirector.BestKey);
            PlayerPrefs.DeleteKey(GameDirector.SpecialUnlockKey);
            PlayerPrefs.Save();
            Debug.Log("Special mode locked again; Let's Toss high score cleared. Ring Toss high score preserved.");
        }

        public static bool CanModifyRecords => !EditorApplication.isPlayingOrWillChangePlaymode;
    }
}
