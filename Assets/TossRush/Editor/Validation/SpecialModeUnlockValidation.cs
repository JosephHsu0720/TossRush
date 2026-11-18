using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace TossRush.Editor
{
    public static class SpecialModeUnlockValidation
    {
        [Serializable] public sealed class Check { public string name; public bool passed; public string detail; }
        [Serializable] public sealed class Report { public bool complete, passed; public int frames; public List<Check> checks = new List<Check>(); }
        const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        static readonly string[] Keys = { GameDirector.BestKey, GameDirector.SpecialBestKey, GameDirector.SpecialUnlockKey };
        static bool[] hadKey;
        static int[] saved;
        static GameDirector game;
        static Report report;
        static IEnumerator<double> sequence;
        static bool running, savedAudio, savedRepeat;
        static int savedStart, frame;
        static double due;
        static TMP_Text Label => game.hud.specialButton.GetComponentInChildren<TMP_Text>(true);
        public static void Begin()
        {
            if (!EditorApplication.isPlaying || running) throw new InvalidOperationException("Enter Play Mode and run one validation at a time.");
            game = UnityEngine.Object.FindFirstObjectByType<GameDirector>();
            hadKey = Keys.Select(PlayerPrefs.HasKey).ToArray(); saved = Keys.Select(k => PlayerPrefs.GetInt(k)).ToArray();
            savedAudio = AudioListener.pause; AudioListener.pause = true;
            savedStart = game.startingStageIndex; savedRepeat = game.repeatStartingStage;
            game.startingStageIndex = 0; game.repeatStartingStage = false;
            report = new Report(); frame = Time.frameCount;
            sequence = Run(); due = EditorApplication.timeSinceStartup;
            running = true; EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += OnPlayMode;
            AssemblyReloadEvents.beforeAssemblyReload += Reload;
        }
        static void Add(string name, bool passed, string detail = "") => report.checks.Add(new Check { name = name, passed = passed, detail = detail });
        static void Score(int value) => typeof(GameDirector).GetField("<Score>k__BackingField", Private).SetValue(game, value);
        static void Fresh(int normalBest, int ringBest = 0)
        {
            game.ReturnToMenu();
            PlayerPrefs.DeleteKey(GameDirector.SpecialUnlockKey);
            PlayerPrefs.SetInt(GameDirector.BestKey, normalBest);
            PlayerPrefs.SetInt(GameDirector.SpecialBestKey, ringBest);
            game.ReturnToMenu();
        }
        // Set up the score boundary, then award through the real normal-hit path.
        static void HitFor(int points)
        {
            var data = game.ActiveStage.data; int previousPoints = data.points, previousBonus = data.cleanShotBonus;
            try
            {
                data.points = points; data.cleanShotBonus = 0;
                var active = new HashSet<Projectile>(game.Projectiles.Where(s => s.InFlight));
                if (!game.Launch(game.thrower.LaunchPosition, Vector3.zero)) throw new InvalidOperationException("Normal throw rejected.");
                var shot = game.Projectiles.First(s => s.InFlight && !active.Contains(s));
                shot.MarkScored(); game.RegisterHit(shot);
            }
            finally { data.points = previousPoints; data.cleanShotBonus = previousBonus; }
        }
        static IEnumerator<double> Run()
        {
            Fresh(0, 5000);
            Add("Fresh player is locked even with an old special-mode record", !game.SpecialModeUnlocked && !game.hud.specialButton.interactable);
            game.hud.specialButton.onClick.Invoke();
            Add("Locked button callback cannot bypass mode entry", !game.IsPlaying && game.hud.menuPanel.activeSelf);
            game.StartSpecialRound();
            Add("Direct special entry is also guarded", !game.IsPlaying && !game.IsSpecialMode);
            Add("Existing menu has one EventSystem and a raycastable Canvas",
                UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length == 1
                && game.hud.GetComponentInParent<UnityEngine.UI.GraphicRaycaster>());
            Fresh(800);
            yield return .2;
            Label.ForceMeshUpdate();
            Add("Locked button shows single-round normal best and the 1200 requirement",
                Label.text.Contains("0800 / 1200") && Label.text.Contains("LET'S TOSS") && !Label.isTextOverflowing
                && Label.textInfo.lineCount == 2, Label.text);
            Add("Locked button is dark but the condition text remains readable", !game.hud.specialButton.interactable
                && game.hud.specialButton.colors.disabledColor.maxColorComponent < .3f && Label.color.maxColorComponent > .7f);
            Directory.CreateDirectory("Docs/Validation");
            ScreenCapture.CaptureScreenshot("Docs/Validation/SpecialModeLocked.png");
            yield return .2;

            Fresh(1199);
            Add("1199 best remains locked and displays exact progress", !game.SpecialModeUnlocked
                && !game.hud.specialButton.interactable && Label.text.Contains("1199 / 1200"));
            game.hud.startButton.onClick.Invoke(); game.thrower.SetCursor(false);
            Add("Let's Toss still starts normal mode while special is locked", game.IsPlaying && !game.IsSpecialMode);
            Score(1198); HitFor(1);
            Add("A real normal reward reaching 1199 does not unlock", game.Score == 1199 && !game.SpecialModeUnlocked);
            yield return .55;
            HitFor(1);
            Add("A real normal reward reaching exactly 1200 unlocks immediately", game.Score == 1200 && game.SpecialModeUnlocked
                && PlayerPrefs.GetInt(GameDirector.SpecialUnlockKey) == 1);
            Score(100); game.ReturnToMenu();
            Add("Unlock survives later score loss and leaving before the result", game.SpecialModeUnlocked
                && PlayerPrefs.GetInt(GameDirector.BestKey) == 1199);
            Add("Unlocked button restores normal entry text and interaction", game.hud.specialButton.interactable
                && Label.text == "SPECIAL MODE / RING TOSS" && !Label.text.Contains("TO UNLOCK"));
            yield return .2;
            Label.ForceMeshUpdate();
            Add("Unlocked title fits its existing button", !Label.isTextOverflowing);
            ScreenCapture.CaptureScreenshot("Docs/Validation/SpecialModeUnlocked.png");
            yield return .2;
            game.hud.specialButton.onClick.Invoke(); game.thrower.SetCursor(false);
            Add("Unlocked menu button enters the 20-second ring-toss round", game.IsPlaying && game.IsSpecialMode
                && game.ActiveStage == game.specialStage && game.CurrentRoundDuration == 20);
            game.ReturnToMenu(); PlayerPrefs.SetInt(GameDirector.SpecialBestKey, 2000); game.ClearHighScore();
            Add("Clearing a score does not revoke a permanent unlock", game.SpecialModeUnlocked
                && PlayerPrefs.GetInt(GameDirector.BestKey) == 0 && PlayerPrefs.GetInt(GameDirector.SpecialBestKey) == 0
                && PlayerPrefs.GetInt(GameDirector.SpecialUnlockKey) == 1);

            Fresh(1200);
            Add("Existing normal best of 1200 grants access", game.SpecialModeUnlocked && game.hud.specialButton.interactable);
            game.StartSpecialRound(); game.thrower.SetCursor(false);
            Add("Old qualifying record is migrated to a saved permanent unlock", game.IsSpecialMode
                && PlayerPrefs.GetInt(GameDirector.SpecialUnlockKey) == 1);

            Fresh(0);
            game.StartNormalRound(); game.thrower.SetCursor(false); Score(1199); HitFor(2);
            Add("Crossing the threshold above 1200 also unlocks", game.Score == 1201 && game.SpecialModeUnlocked);

            Fresh(600);
            for (int i = 0; i < 2; i++)
            {
                game.StartNormalRound(); game.thrower.SetCursor(false); Score(600);
                typeof(GameDirector).GetMethod("FinishRound", Private).Invoke(game, null);
            }
            game.ReturnToMenu();
            Add("Two 600-point rounds are not added together for unlocking", !game.SpecialModeUnlocked
                && PlayerPrefs.GetInt(GameDirector.BestKey) == 600 && !game.hud.specialButton.interactable);
            yield return .1;
        }
        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < due) return;
            try
            {
                if (!sequence.MoveNext()) { Finish(true); return; }
                due = EditorApplication.timeSinceStartup + sequence.Current;
            }
            catch (Exception exception) { Add("Validation exception", false, exception.ToString()); Finish(false); }
        }
        static void Finish(bool complete)
        {
            if (!running) return; running = false;
            EditorApplication.update -= Tick; EditorApplication.playModeStateChanged -= OnPlayMode;
            AssemblyReloadEvents.beforeAssemblyReload -= Reload; sequence?.Dispose();
            game.ReturnToMenu();
            for (int i = 0; i < Keys.Length; i++)
                if (hadKey[i]) PlayerPrefs.SetInt(Keys[i], saved[i]); else PlayerPrefs.DeleteKey(Keys[i]);
            PlayerPrefs.Save(); AudioListener.pause = savedAudio;
            game.startingStageIndex = savedStart; game.repeatStartingStage = savedRepeat; game.ReturnToMenu();
            report.complete = complete; report.frames = Time.frameCount - frame;
            report.passed = complete && report.frames > 5 && report.checks.All(c => c.passed);
            Directory.CreateDirectory("Docs/Validation");
            File.WriteAllText("Docs/Validation/SpecialModeUnlock.json", JsonUtility.ToJson(report, true));
            Debug.Log("Special mode unlock validation: " + report.checks.Count(c => c.passed) + "/" + report.checks.Count);
        }
        static void Reload() => Finish(false);
        static void OnPlayMode(PlayModeStateChange state) { if (state == PlayModeStateChange.ExitingPlayMode) Finish(false); }
    }
}
