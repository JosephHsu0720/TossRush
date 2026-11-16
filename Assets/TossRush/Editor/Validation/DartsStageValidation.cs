using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TossRush.Editor
{
    // Runs against the real player loop, colliders, pooled projectiles and stage transitions.
    public static class DartsStageValidation
    {
        [Serializable] public sealed class CheckResult { public string name; public bool passed; public string detail; }
        [Serializable] public sealed class Report { public bool passed; public int startFrame, endFrame; public List<CheckResult> checks = new List<CheckResult>(); }
        static GameDirector game;
        static Report report;
        static int step;
        static double due;
        static float originalDuration;
        static int originalStart;
        static bool originalRepeat;
        static Projectile missed;
        static int regionTest, previousRegion;
        static Vector3 previousAim;

        public static void Begin()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
            EditorApplication.update -= Tick;
            game = UnityEngine.Object.FindFirstObjectByType<GameDirector>();
            originalDuration = game.roundDuration; originalStart = game.startingStageIndex; originalRepeat = game.repeatStartingStage;
            report = new Report { startFrame = Time.frameCount };
            step = 0; due = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
        }

        static void Check(string name, bool passed, string detail)
            => report.checks.Add(new CheckResult { name = name, passed = passed, detail = detail });

        static void Reset(int index = 5)
        {
            game.startingStageIndex = index; game.repeatStartingStage = false; game.roundDuration = 60;
            game.StartRound(); game.thrower.SetCursor(false);
        }

        static void LaunchNear(float horizontal, bool fromBack = false)
        {
            Vector3 target = game.ActiveStage.gate.transform.position + Vector3.right * horizontal;
            Vector3 direction = fromBack ? Vector3.back : Vector3.forward;
            Vector3 start = target - direction;
            game.Launch(start, fromBack ? direction * 10 : VelocityTo(start, target));
        }

        static void Select(int index) => game.ActiveStage.Dartboard.SelectRegion(index);

        static void LaunchBoardPoint(Vector2 point)
        {
            Vector3 target = game.ActiveStage.movingTarget.TransformPoint(new Vector3(point.x, point.y, -.10f));
            Vector3 start = target + Vector3.back;
            game.Launch(start, VelocityTo(start, target));
        }

        static void ValidateRegions()
        {
            var dartboard = game.ActiveStage.Dartboard;
            int first = dartboard.CurrentRegionIndex;
            Check("First entry chooses a region and matching outline", first >= 0 && dartboard.CurrentRegion != null
                && dartboard.outlineFilter.sharedMesh == dartboard.CurrentRegion.outline, "First=" + first + " " + dartboard.CurrentRegion.label);
            bool valid = true;
            foreach (var region in dartboard.layout.regions)
                valid &= region.Contains(region.AimPoint, game.ActiveStage.data.projectileRadius);
            Check("All 82 regions fit the dart tip", valid && dartboard.layout.regions.Length == 82, "Regions=" + dartboard.layout.regions.Length);
            var seen = new HashSet<int>(); bool noRepeats = true, aligned = true;
            int last = first;
            for (int i = 0; i < 2048; i++)
            {
                game.ActiveStage.ResetStage();
                seen.Add(dartboard.CurrentRegionIndex);
                noRepeats &= dartboard.CurrentRegionIndex != last;
                aligned &= dartboard.outlineFilter.sharedMesh == dartboard.CurrentRegion.outline
                    && dartboard.maskFilter.sharedMesh == dartboard.CurrentRegion.mask
                    && Vector2.Distance(dartboard.CurrentRegion.AimPoint, (Vector2)game.ActiveStage.gate.transform.localPosition) < .0001f;
                last = dartboard.CurrentRegionIndex;
            }
            Check("Re-entry samples all regions without consecutive repeats", noRepeats && seen.Count == 82, "Unique=" + seen.Count + "/82 across 2048 entries");
            Check("Outline mask and scoring plane follow every selection", aligned, "2048 selections");
            float baseline = game.ActiveStage.gate.transform.position.z - (-6 + Mathf.Cos(game.thrower.restingPitch * Mathf.Deg2Rad) * .5f);
            float current = game.ActiveStage.gate.transform.position.z - game.thrower.LaunchPosition.z;
            Check("Throw distance reduced another 10 percent to 72 percent of original", Mathf.Abs(current / baseline - .72f) < .0001f,
                "Baseline=" + baseline + "m, current=" + current + "m, ratio=" + current / baseline);
        }

        // Solve against the same discrete drag/gravity integration as the trajectory preview.
        static Vector3 VelocityTo(Vector3 start, Vector3 target)
        {
            var data = game.ActiveStage.data;
            Vector3 velocity = (target - start).normalized * data.launchSpeed;
            for (int iteration = 0; iteration < 14; iteration++)
            {
                Vector3 pos = start, vel = velocity, previous = start;
                float time = 0;
                for (int frame = 0; frame < 100; frame++)
                {
                    previous = pos;
                    ProjectileFlight.Step(ref pos, ref vel, data.physicsProfile, Vector3.zero, data.gravityScale, Time.fixedDeltaTime);
                    time += Time.fixedDeltaTime;
                    Vector3 tip = pos + vel.normalized * .235f;
                    if (tip.z < target.z) continue;
                    Vector3 priorTip = previous + vel.normalized * .235f;
                    float fraction = Mathf.Clamp01((target.z - priorTip.z) / (tip.z - priorTip.z));
                    Vector3 crossing = Vector3.Lerp(priorTip, tip, fraction);
                    velocity = (velocity + (target - crossing) / Mathf.Max(.02f, time)).normalized * data.launchSpeed;
                    break;
                }
            }
            return velocity;
        }

        static void AimFromPlayer()
        {
            for (int i = 0; i < 5; i++)
                game.thrower.view.transform.rotation = Quaternion.LookRotation(VelocityTo(game.thrower.LaunchPosition, game.ActiveStage.gate.transform.position));
        }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < due) return;
            try
            {
                switch (step++)
                {
                    case 0:
                        Reset(); ValidateRegions();
                        Check("Six stages and held dart", game.stages.Length == 6 && game.thrower.heldItems.Length == 6
                            && game.thrower.heldItems[5].activeSelf, "Stages=" + game.stages.Length + ", held=" + game.thrower.heldItems.Length);
                        AimFromPlayer();
                        Check("Full distance throw accepted", game.thrower.TryThrow(), "Origin=" + game.thrower.LaunchPosition);
                        break;
                    case 1:
                        Check("Selected region scores and wraps to room", game.Hits == 1 && game.Score == 250 && game.LastWasClean
                            && game.ActiveStage.data.name == "Corner", "Score=" + game.Score + ", stage=" + game.ActiveStage.data.name);
                        Check("Other stages restore the camera position", Mathf.Abs(game.thrower.view.transform.localPosition.z + 6) < .0001f,
                            "Camera z=" + game.thrower.view.transform.localPosition.z);
                        Reset(); Select(4); LaunchBoardPoint(Vector2.zero);
                        break;
                    case 2:
                        missed = game.Projectiles.FirstOrDefault(p => p.InFlight);
                        Check("Unselected bullseye does not score and dart sticks", game.Hits == 0 && missed && missed.CollisionCount > 0
                            && missed.Body.constraints == RigidbodyConstraints.FreezeAll, "Hits=" + game.Hits);
                        Reset(); Select(1); LaunchNear(0);
                        break;
                    case 3:
                        Check("Selected outer bull scores with reused dart", game.Hits == 1 && game.Score == 250, "Score=" + game.Score);
                        Reset(); Select(5); LaunchNear(0);
                        break;
                    case 4:
                        Check("Selected double scores", game.Hits == 1, "Hits=" + game.Hits);
                        Reset(); Select(3); LaunchNear(0);
                        break;
                    case 5:
                        Check("Selected triple scores", game.Hits == 1, "Hits=" + game.Hits);
                        Reset(); Select(2); LaunchBoardPoint(game.ActiveStage.Dartboard.layout.regions[6].AimPoint);
                        break;
                    case 6:
                        Check("Adjacent unselected sector does not score", game.Hits == 0 && game.Score == 0, "Hits=" + game.Hits);
                        Reset(); Select(0); LaunchNear(.16f);
                        break;
                    case 7:
                        Check("Bullseye scores when selected", game.Hits == 1, "Hits=" + game.Hits);
                        Reset(); Select(0); LaunchNear(.24f);
                        break;
                    case 8:
                        Check("Unselected outer bull does not score", game.Hits == 0 && game.Score == 0, "Hits=" + game.Hits);
                        Reset(); LaunchNear(0, true);
                        break;
                    case 9:
                        Check("Back of board does not score", game.Hits == 0, "Hits=" + game.Hits);
                        Reset(); LaunchBoardPoint(new Vector2(1.8f, 0));
                        break;
                    case 10:
                        Check("Wall miss does not score", game.Hits == 0 && game.Projectiles.Any(p => p.InFlight && p.CollisionCount > 0), "Hits=" + game.Hits);
                        Reset(); game.repeatStartingStage = true;
                        previousRegion = game.ActiveStage.Dartboard.CurrentRegionIndex;
                        previousAim = game.ActiveStage.gate.transform.position;
                        LaunchNear(0);
                        break;
                    case 11:
                        Check("Fixed stage clear chooses a different target", game.ActiveStage.data.name == "Darts" && game.Hits == 1
                            && game.ActiveStage.Dartboard.CurrentRegionIndex != previousRegion, "Hits=" + game.Hits + ", old=" + previousRegion
                            + ", new=" + game.ActiveStage.Dartboard.CurrentRegionIndex);
                        Vector3 start = previousAim + Vector3.back;
                        game.Launch(start, VelocityTo(start, previousAim));
                        break;
                    case 12:
                        Check("Previous target no longer scores", game.Hits == 1, "Hits=" + game.Hits);
                        Reset(4);
                        game.Launch(game.thrower.LaunchPosition, Vector3.forward);
                        var shot = game.Projectiles.First(p => p.InFlight);
                        shot.MarkScored(); game.RegisterHit(shot);
                        break;
                    case 13:
                        Check("Mailbox advances to a randomly selected bar target", game.ActiveStage.data.name == "Darts"
                            && game.ActiveStage.Dartboard.CurrentRegionIndex >= 0, "Stage=" + game.ActiveStage.data.name);
                        LaunchNear(0);
                        break;
                    case 14:
                        Check("Bar clear wraps without duplicate scoring", game.ActiveStage.data.name == "Corner" && game.Hits == 2,
                            "Stage=" + game.ActiveStage.data.name + ", hits=" + game.Hits);
                        Reset(); game.roundDuration = .05f; game.StartRound(); game.thrower.SetCursor(false);
                        AimFromPlayer(); game.thrower.TryThrow();
                        break;
                    case 15:
                        Check("Timeout rejects late dart", !game.IsPlaying && game.Hits == 0 && game.Score == 0, "Playing=" + game.IsPlaying);
                        Check("Player loop advanced", Time.frameCount > report.startFrame + 20, "Frames=" + (Time.frameCount - report.startFrame));
                        regionTest = 0; Reset(); Select(regionTest); AimFromPlayer(); game.thrower.TryThrow();
                        break;
                    case 16:
                        Check("Player throw hits region " + regionTest + " " + game.stages[5].Dartboard.layout.regions[regionTest].label,
                            game.Hits == 1 && game.Score == 250, "Hits=" + game.Hits + ", score=" + game.Score);
                        regionTest++;
                        if (regionTest == game.stages[5].Dartboard.layout.regions.Length) { Finish(); return; }
                        Reset(); Select(regionTest); AimFromPlayer(); game.thrower.TryThrow(); step = 16;
                        break;
                }
                due = EditorApplication.timeSinceStartup + (step == 16 ? .6 : .85);
            }
            catch (Exception error)
            {
                Check("Validation exception", false, error.ToString()); Finish();
            }
        }
        static void Finish()
        {
            EditorApplication.update -= Tick;
            game.roundDuration = originalDuration; game.startingStageIndex = originalStart; game.repeatStartingStage = originalRepeat;
            report.endFrame = Time.frameCount; report.passed = report.checks.All(c => c.passed);
            Directory.CreateDirectory("Docs/Validation");
            File.WriteAllText("Docs/Validation/DartsStageChecks.json", JsonUtility.ToJson(report, true));
            Debug.Log("Darts validation: " + report.checks.Count(c => c.passed) + "/" + report.checks.Count);
        }
    }
}
