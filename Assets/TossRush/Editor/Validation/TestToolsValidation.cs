using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace TossRush.Editor
{
    public static class TestToolsValidation
    {
        [Serializable] public sealed class Check { public string name; public bool passed; }
        [Serializable] public sealed class Report { public bool passed; public List<Check> checks = new List<Check>(); }

        public static void Run()
        {
            if (!HighScoreTools.CanModifyRecords) throw new InvalidOperationException("Stop Play Mode first.");
            string[] keys = { GameDirector.BestKey, GameDirector.SpecialBestKey, GameDirector.SpecialUnlockKey };
            var had = keys.Select(PlayerPrefs.HasKey).ToArray(); var saved = keys.Select(k => PlayerPrefs.GetInt(k)).ToArray();
            var report = new Report();
            void Check(string name, bool pass) => report.checks.Add(new Check { name = name, passed = pass });
            try
            {
                PlayerPrefs.SetInt(GameDirector.BestKey, 2000);
                PlayerPrefs.SetInt(GameDirector.SpecialBestKey, 5000);
                PlayerPrefs.SetInt(GameDirector.SpecialUnlockKey, 1);
                HighScoreTools.ClearHighScore();
                Check("Clear High Score deletes both mode records", !PlayerPrefs.HasKey(GameDirector.BestKey) && !PlayerPrefs.HasKey(GameDirector.SpecialBestKey));
                Check("Clear High Score preserves a permanent unlock", PlayerPrefs.GetInt(GameDirector.SpecialUnlockKey) == 1);

                PlayerPrefs.DeleteKey(GameDirector.SpecialUnlockKey);
                PlayerPrefs.SetInt(GameDirector.BestKey, 1199);
                PlayerPrefs.SetInt(GameDirector.SpecialBestKey, 5000);
                HighScoreTools.ClearHighScore();
                Check("Clearing scores does not unlock an unqualified player", !PlayerPrefs.HasKey(GameDirector.SpecialUnlockKey));

                PlayerPrefs.SetInt(GameDirector.BestKey, 1200);
                HighScoreTools.ClearHighScore();
                Check("Clearing a qualifying legacy score preserves its unlocked eligibility", PlayerPrefs.GetInt(GameDirector.SpecialUnlockKey) == 1);

                PlayerPrefs.SetInt(GameDirector.BestKey, 3000);
                PlayerPrefs.SetInt(GameDirector.SpecialBestKey, 7000);
                HighScoreTools.ResetSpecialModeUnlock();
                Check("Reset deletes the unlock flag", !PlayerPrefs.HasKey(GameDirector.SpecialUnlockKey));
                Check("Reset also deletes the qualifying normal score", !PlayerPrefs.HasKey(GameDirector.BestKey));
                Check("Reset preserves the special-mode high score", PlayerPrefs.GetInt(GameDirector.SpecialBestKey) == 7000);
                HighScoreTools.ResetSpecialModeUnlock();
                Check("Repeated reset stays locked and preserves the special record", PlayerPrefs.GetInt(GameDirector.BestKey) < 1200
                    && !PlayerPrefs.HasKey(GameDirector.SpecialUnlockKey) && PlayerPrefs.GetInt(GameDirector.SpecialBestKey) == 7000);
            }
            finally
            {
                for (int i = 0; i < keys.Length; i++)
                    if (had[i]) PlayerPrefs.SetInt(keys[i], saved[i]); else PlayerPrefs.DeleteKey(keys[i]);
                PlayerPrefs.Save();
            }
            Check("Original player records restored", keys.Select((key, i) => PlayerPrefs.HasKey(key) == had[i] && PlayerPrefs.GetInt(key) == saved[i]).All(x => x));
            report.passed = report.checks.All(c => c.passed);
            Directory.CreateDirectory("Docs/Validation");
            File.WriteAllText("Docs/Validation/TestTools.json", JsonUtility.ToJson(report, true));
            Debug.Log("Test tools validation: " + report.checks.Count(c => c.passed) + "/" + report.checks.Count);
        }
    }
}
