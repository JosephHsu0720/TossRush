using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using TossRush;

public static class ValidateJamRelease
{
    [Serializable] public sealed class Check { public string name, detail; public bool passed; }
    [Serializable] public sealed class Report { public string utc; public bool passed; public int startFrame, endFrame; public double fullRoundSeconds; public List<Check> checks = new List<Check>(); }
    static GameDirector game;
    static Report report;
    static double due, roundStart;
    static int phase, savedBest, savedStart;
    static bool savedBestExists, savedRepeat;
    static float savedScale, savedDuration;

    public static void Begin()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        game = UnityEngine.Object.FindFirstObjectByType<GameDirector>();
        if (!game) throw new InvalidOperationException("Open the TossRush scene first.");
        savedBest = PlayerPrefs.GetInt(GameDirector.BestKey, 0);
        savedBestExists = PlayerPrefs.HasKey(GameDirector.BestKey);
        savedStart = game.startingStageIndex; savedRepeat = game.repeatStartingStage;
        savedScale = Time.timeScale; savedDuration = game.roundDuration;
        report = new Report { utc = DateTime.UtcNow.ToString("o"), startFrame = Time.frameCount };
        Add("Release scene has six complete stages", game.stages.Length == 6 && game.stages.All(s => s && s.data && s.gate), "Count=" + game.stages.Length);
        Add("Default round is 20 seconds and begins at first stage", game.roundDuration == 20 && savedStart == 0 && !savedRepeat, "Duration=" + game.roundDuration);
        game.startingStageIndex = 0; game.repeatStartingStage = false;
        game.StartRound(); game.thrower.SetCursor(false);
        due = EditorApplication.timeSinceStartup + .35;
        phase = 0;
        EditorApplication.update += Tick;
    }

    static void Add(string name, bool passed, string detail) => report.checks.Add(new Check { name = name, passed = passed, detail = detail });
    static void Tick()
    {
        if (EditorApplication.timeSinceStartup < due) return;
        try
        {
            switch (phase++)
            {
                case 0:
                    Add("Player loop advances", Time.frameCount > report.startFrame + 2, "Frames=" + (Time.frameCount - report.startFrame));
                    Add("Released cursor does not pause countdown", game.Remaining < 19.9f && game.Remaining > 18, "Remaining=" + game.Remaining);
                    game.roundDuration = .2f; game.StartRound(); game.thrower.SetCursor(false);
                    Time.timeScale = 0;
                    due = EditorApplication.timeSinceStartup + .35;
                    break;
                case 1:
                    Add("Time scale zero does not extend wall-clock deadline", !game.CanScore && !game.CanThrow && game.Remaining == 0, "Playing=" + game.IsPlaying);
                    Time.timeScale = savedScale;
                    game.roundDuration = 20; game.StartRound(); game.thrower.SetCursor(false);
                    // Suspend the player loop itself, as happens in a hidden browser tab.
                    game.roundDuration = .15f; game.StartRound(); game.thrower.SetCursor(false);
                    System.Threading.Thread.Sleep(220);
                    Add("Expired deadline rejects throws before the next Update", !game.CanThrow && !game.CanScore && !game.thrower.TryThrow(), "Remaining=" + game.Remaining);
                    game.roundDuration = 20; game.StartRound(); game.thrower.SetCursor(false);
                    game.Launch(game.thrower.LaunchPosition, game.thrower.LaunchVelocity);
                    var shot = game.Projectiles.First(p => p.InFlight);
                    shot.MarkScored(); game.RegisterHit(shot);
                    Add("Valid registered hit enters transition", game.IsTransitioning && game.Hits == 1 && game.Score > 0, "Controlled scoring setup, not a player aim test.");
                    due = EditorApplication.timeSinceStartup + .5;
                    break;
                case 2:
                    Add("Transition shares the same deadline", game.ActiveStage == game.stages[1] && game.Remaining < 19.9f && game.Remaining > 18, "Remaining=" + game.Remaining);
                    game.StartRound(); game.thrower.SetCursor(false);
                    Add("Replay resets score, throws, hits and unlocked progress", game.Score == 0 && game.Throws == 0 && game.Hits == 0 && game.stages.All(s => s.ClearsThisRound == 0), "All six stages reset.");
                    roundStart = Time.realtimeSinceStartupAsDouble;
                    due = EditorApplication.timeSinceStartup + 20.1;
                    break;
                case 3:
                    report.fullRoundSeconds = Time.realtimeSinceStartupAsDouble - roundStart;
                    Add("Full 20-second round finishes", !game.IsPlaying && !game.CanThrow && !game.CanScore && game.Remaining == 0, "Observed after " + report.fullRoundSeconds.ToString("F3") + " seconds.");
                    Add("Result screen is visible and all projectiles retired", game.hud.resultPanel.activeSelf && game.Projectiles.All(p => !p.InFlight), "Result=" + game.hud.resultPanel.activeSelf);
                    Add("Late throw is rejected without changing score", !game.thrower.TryThrow() && game.Throws == 0 && game.Score == 0, "No late score/throw.");
                    Finish();
                    break;
            }
        }
        catch (Exception error) { Add("Validation exception", false, error.ToString()); Finish(); }
    }

    static void Finish()
    {
        EditorApplication.update -= Tick;
        Time.timeScale = savedScale;
        game.roundDuration = savedDuration; game.startingStageIndex = savedStart; game.repeatStartingStage = savedRepeat;
        if (savedBestExists) PlayerPrefs.SetInt(GameDirector.BestKey, savedBest); else PlayerPrefs.DeleteKey(GameDirector.BestKey);
        PlayerPrefs.Save();
        typeof(GameDirector).GetProperty("Best").SetValue(game, savedBest);
        report.endFrame = Time.frameCount; report.passed = report.checks.All(c => c.passed);
        Directory.CreateDirectory("Docs/Release");
        File.WriteAllText("Docs/Release/JamPlayMode.json", Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
        Debug.Log("Jam release checks: " + report.checks.Count(c => c.passed) + "/" + report.checks.Count);
    }
}
