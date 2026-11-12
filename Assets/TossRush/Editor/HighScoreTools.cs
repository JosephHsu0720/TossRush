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
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TossRush/Scenes/TossRushPrototype.unity");
        }

        [MenuItem("TossRush/Clear High Score")]
        public static void ClearHighScore()
        {
            PlayerPrefs.DeleteKey(GameDirector.BestKey);
            PlayerPrefs.Save();
            if (Application.isPlaying)
                foreach (var game in Object.FindObjectsByType<GameDirector>(FindObjectsSortMode.None)) game.ClearHighScore();
            Debug.Log("TossRush high score cleared.");
        }
    }
}
