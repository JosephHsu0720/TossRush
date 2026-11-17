using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TossRush.Editor
{
    public static class StreetColorValidation
    {
        [Serializable] public sealed class Check { public string name, detail; public bool passed; }
        [Serializable] public sealed class Report { public bool passed; public int startFrame, endFrame; public List<Check> checks = new List<Check>(); }
        static GameDirector game;
        static Report report;
        static int test, originalStart;
        static bool waiting, originalRepeat;
        static float originalDuration;
        static double due;
        static Projectile shot;
        static ScoreGate gate;
        static TrashColor launchedColor;
        static int token;
        static StageView entryStage;
        static string entryLayout;
        static int rotationStartToken;

        public static void Begin()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
            game = UnityEngine.Object.FindFirstObjectByType<GameDirector>();
            originalStart = game.startingStageIndex; originalRepeat = game.repeatStartingStage; originalDuration = game.roundDuration;
            report = new Report { startFrame = Time.frameCount };
            test = 0; waiting = false; due = 0;
            rotationStartToken = 0;
            game.repeatStartingStage = false; game.roundDuration = 120;
            Add("Only Street requires colour matching", game.stages.All(s => s.data.matchTrashColors == (s.data.name == "Street")), "Other stage scoring stays unrestricted.");
            for (int i = 0; i < game.stages.Length; i++)
            {
                if (game.stages[i].data.matchTrashColors) continue;
                game.startingStageIndex = i;
                game.StartRound();
                game.Launch(Vector3.up * 10, Vector3.zero);
                var probe = game.Projectiles.First(s => s.InFlight);
                game.RegisterTargetEntry(probe, game.ActiveStage.gate);
                int expected = game.ActiveStage.data.points + game.ActiveStage.data.cleanShotBonus;
                Add("Shared entry preserves " + game.ActiveStage.data.name, game.Hits == 1 && game.Score == expected && game.IsTransitioning,
                    "Score=" + game.Score + ", expected=" + expected);
            }
            game.startingStageIndex = Array.FindIndex(game.stages, s => s.data.name == "Street");
            game.StartRound();
            var bins = game.ActiveStage.movingTarget.GetComponentsInChildren<ScoreGate>();
            Add("Three distinct bin colours", bins.Length == 3 && bins.Select(g => g.trashColor).Distinct().Count() == 3
                && bins.All(g => g.trashColor != TrashColor.None), string.Join(", ", bins.Select(g => g.trashColor)));
            Add("Bin body and lid match scoring colour", bins.All(g =>
            {
                var marker = g.GetComponentInParent<TargetMarker>();
                var surfaces = marker.GetComponentsInChildren<MeshRenderer>().Where(r => r.GetComponentInParent<TargetMarker>() == marker
                    && (r.name == "Wheeled Bin Body" || r.name == "Roll Surface")).ToArray();
                return surfaces.Length == 2 && surfaces.All(r => r.sharedMaterial.name == "Bin" + g.trashColor);
            }), "Each bin's body and lid must use its own colour, including nested bin instances.");
            ValidateVisitRandomization();
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        static void Add(string name, bool passed, string detail) => report.checks.Add(new Check { name = name, passed = passed, detail = detail });

        static bool VisualMatches(TrashColorVisual visual, TrashColor color)
        {
            Material expected = color == TrashColor.Red ? visual.red : color == TrashColor.Green ? visual.green : visual.blue;
            return visual.surfaces.All(r => r.sharedMaterial == expected);
        }

        static string Layout(StageView stage) => string.Join(",", stage.movingTarget.GetComponentsInChildren<ScoreGate>()
            .OrderBy(g => g.transform.position.x).Select(g => g.trashColor));

        static void ValidateVisitRandomization()
        {
            var bagColors = new HashSet<TrashColor>();
            var layouts = new HashSet<string>();
            bool stable = true, visualsMatch = true, permutations = true, wrongKeepsVisit = true;
            for (int visit = 0; visit < 128; visit++)
            {
                game.StartRound();
                var stage = game.ActiveStage;
                TrashColor color = game.NextTrashColor;
                string layout = Layout(stage);
                bagColors.Add(color); layouts.Add(layout);
                var bins = stage.movingTarget.GetComponentsInChildren<ScoreGate>();
                permutations &= bins.Length == 3 && bins.Select(g => g.trashColor).Distinct().Count() == 3 && bins.All(g => g.trashColor != TrashColor.None);
                visualsMatch &= bins.All(g => VisualMatches(g.GetComponentInParent<TargetMarker>().GetComponent<TrashColorVisual>(), g.trashColor));
                int visitToken = game.StageToken;
                for (int attempt = 0; attempt < 4; attempt++)
                {
                    bool launched = game.Launch(Vector3.up * 10, Vector3.zero);
                    var bag = game.Projectiles.First(s => s.InFlight);
                    stable &= launched && bag.TrashColor == color && game.NextTrashColor == color && Layout(stage) == layout;
                    var held = game.thrower.heldItems[stage.data.heldItemIndex].GetComponentInChildren<TrashColorVisual>();
                    stable &= VisualMatches(held, color) && VisualMatches(bag.GetComponentInChildren<TrashColorVisual>(), color);
                    game.RegisterTargetEntry(bag, bins.First(g => g.trashColor != color));
                    wrongKeepsVisit &= game.Score == -50 * (attempt + 1) && game.StageToken == visitToken && game.NextTrashColor == color
                        && Layout(stage) == layout && !game.IsTransitioning;
                    bag.Retire();
                }
            }
            Add("New visits sample all three bag colours", bagColors.Count == 3, "128 entries; colours=" + bagColors.Count);
            Add("New visits sample all six bin orders", layouts.Count == 6, string.Join(" / ", layouts.OrderBy(s => s)));
            Add("Every shuffled layout has one bin of each colour", permutations, "128 entries");
            Add("Shuffled bin appearance matches scoring", visualsMatch, "All bodies and lids across 128 entries");
            Add("Repeated throws and pooled reuse keep the same colour and layout", stable, "512 throws across 128 entries");
            Add("Wrong bins preserve the current visit colour and layout", wrongKeepsVisit, "512 wrong entries; -50 each");

            game.StartRound();
            TrashColor beforeColor = game.NextTrashColor;
            string beforeLayout = Layout(game.ActiveStage);
            for (int i = 0; i < game.poolSizePerStage; i++) game.Launch(Vector3.up * 10, Vector3.zero);
            Add("Full projectile pool does not reroll", !game.Launch(Vector3.up * 10, Vector3.zero)
                && game.NextTrashColor == beforeColor && Layout(game.ActiveStage) == beforeLayout, "Rejected throw keeps the current assignment");
            game.StartRound();
        }

        static void Tick()
        {
            if (!EditorApplication.isPlaying) { EditorApplication.update -= Tick; return; }
            if (EditorApplication.timeSinceStartup < due) return;
            try
            {
                if (test > 12) { ValidateRotation(); return; }
                if (!waiting)
                {
                    // Nine colour combinations plus outside, upward, and high-speed interior entries.
                    TrashColor wanted = (TrashColor)(test < 9 ? test / 3 + 1 : 1);
                    for (int i = 0; i < 256; i++) { game.StartRound(); if (game.NextTrashColor == wanted) break; }
                    game.thrower.SetCursor(false);
                    launchedColor = game.NextTrashColor;
                    entryStage = game.ActiveStage;
                    entryLayout = Layout(entryStage);
                    gate = game.ActiveStage.movingTarget.GetComponentsInChildren<ScoreGate>()
                        .First(g => g.trashColor == (TrashColor)(test < 9 ? test % 3 + 1 : test == 12 ? 2 : 1));
                    var held = game.thrower.heldItems[game.ActiveStage.data.heldItemIndex].GetComponentInChildren<TrashColorVisual>();
                    Add("Held bag colour " + test, launchedColor == wanted && held && VisualMatches(held, launchedColor), launchedColor.ToString());
                    Vector3 start = gate.transform.position + Vector3.up * .8f;
                    Vector3 velocity = Vector3.down * 4;
                    if (test == 9) start += Vector3.back * 15;
                    if (test == 10) { start = gate.transform.position - Vector3.up * .2f; velocity = Vector3.up * 7; }
                    if (test >= 11) { start = gate.transform.position + Vector3.up * .4f; velocity = Vector3.down * 28; }
                    game.Launch(start, velocity);
                    shot = game.Projectiles.First(s => s.InFlight);
                    token = game.StageToken;
                    var visual = shot.GetComponentInChildren<TrashColorVisual>();
                    Add("Flight colour preserved " + test, shot.TrashColor == launchedColor && visual && VisualMatches(visual, launchedColor), launchedColor.ToString());
                    if (launchedColor != gate.trashColor) game.RegisterCollision(shot, start, 2);
                    var nextHeld = game.thrower.heldItems[game.ActiveStage.data.heldItemIndex].GetComponentInChildren<TrashColorVisual>();
                    Add("Throws retain entry colour and bin order " + test, game.NextTrashColor == launchedColor
                        && VisualMatches(nextHeld, launchedColor) && Layout(entryStage) == entryLayout, game.NextTrashColor + " / " + entryLayout);
                    waiting = true;
                    due = EditorApplication.timeSinceStartup + (test == 10 ? .06 : .7);
                    return;
                }
                bool match = launchedColor == gate.trashColor;
                if (test == 9 || test == 10)
                {
                    Add(test == 9 ? "Outside opening does not score" : "Upward entry does not score", game.Score == 0 && game.Hits == 0 && !shot.Scored, "Score=" + game.Score);
                }
                else if (match)
                {
                    Add("Matching entry " + test, game.Score == 350 && game.Hits == 1 && game.LastBonus == 150, "Score=" + game.Score + ", hits=" + game.Hits + ", bonus=" + game.LastBonus);
                }
                else
                {
                    Add("Wrong entry " + test, game.Score == -50 && game.Hits == 0 && game.LastBonus == 0
                        && game.StageToken == token && !game.IsTransitioning && shot.Scored, "Score=" + game.Score + ", pending=" + shot.PendingCollisionPoints + ", hits=" + game.Hits);
                    game.RegisterTargetEntry(shot, gate);
                    game.RegisterTargetEntry(shot, game.ActiveStage.gate);
                    Add("Wrong bag resolves only once " + test, game.Score == -50 && game.Hits == 0, "Score=" + game.Score);
                }
                if (!match || test == 9 || test == 10)
                    Add("Failed attempt keeps colour and bin order " + test, game.NextTrashColor == launchedColor && Layout(entryStage) == entryLayout,
                        game.NextTrashColor + " / " + Layout(entryStage));
                waiting = false;
                test++;
            }
            catch (Exception exception) { Add("Validation exception", false, exception.ToString()); Finish(); }
        }

        static void ValidateRotation()
        {
            if (rotationStartToken == 0)
            {
                game.StartRound(); game.thrower.SetCursor(false);
                rotationStartToken = game.StageToken;
            }
            if (game.IsTransitioning) return;
            if (game.StageToken > rotationStartToken && game.ActiveStage.data.matchTrashColors)
            {
                var bins = game.ActiveStage.movingTarget.GetComponentsInChildren<ScoreGate>();
                Add("Normal rotation prepares a fresh Street visit", game.NextTrashColor != TrashColor.None
                    && bins.Select(g => g.trashColor).Distinct().Count() == 3
                    && bins.All(g => VisualMatches(g.GetComponentInParent<TargetMarker>().GetComponent<TrashColorVisual>(), g.trashColor)),
                    "Stage token=" + rotationStartToken + " -> " + game.StageToken + "; bag=" + game.NextTrashColor + "; bins=" + Layout(game.ActiveStage));
                Finish();
                return;
            }
            var target = game.ActiveStage.data.matchTrashColors
                ? game.ActiveStage.movingTarget.GetComponentsInChildren<ScoreGate>().First(g => g.trashColor == game.NextTrashColor)
                : game.ActiveStage.gate;
            game.Launch(Vector3.up * 10, Vector3.zero);
            game.RegisterTargetEntry(game.Projectiles.First(s => s.InFlight), target);
        }

        static void Finish()
        {
            EditorApplication.update -= Tick;
            report.endFrame = Time.frameCount;
            Add("Real player loop advances", report.endFrame > report.startFrame + 20, report.startFrame + " -> " + report.endFrame);
            report.passed = report.checks.All(c => c.passed);
            Directory.CreateDirectory("Docs/Validation");
            File.WriteAllText("Docs/Validation/StreetColorPlayMode.json", JsonUtility.ToJson(report, true));
            game.startingStageIndex = originalStart; game.repeatStartingStage = originalRepeat; game.roundDuration = originalDuration;
            Debug.Log("Street colour validation: " + (report.passed ? "PASS" : "FAIL") + ", " + report.checks.Count + " checks.");
        }
    }
}
