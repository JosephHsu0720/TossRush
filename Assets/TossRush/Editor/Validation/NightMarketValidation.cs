using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TossRush.Editor
{
    // Exercises the live player loop and pooled bodies. Direct geometry checks cover
    // the boundaries that would be unreliable to reproduce with a timed mouse throw.
    public static class NightMarketValidation
    {
        [Serializable] public sealed class CheckResult { public string name; public bool passed; public string detail; }
        [Serializable] public sealed class Report
        {
            public bool passed;
            public int startFrame, endFrame;
            public List<CheckResult> checks = new List<CheckResult>();
        }

        static GameDirector game;
        static SpecialModeTestAccess specialAccess;
        static Report report;
        static RingTarget[] rows;
        static RingTarget[] arcTargets;
        static Projectile shot;
        static int step, row, expected, initialToken, arcIndex;
        static float lastFlightTime;
        static double due;
        static float originalDuration;
        static int originalStart;
        static bool originalRepeat;
        static Vector3 windBefore, remoteVelocity;
        static bool hadNormalBest, hadSpecialBest;
        static int normalBest, specialBest;

        public static void Begin()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
            EditorApplication.update -= Tick;
            game = UnityEngine.Object.FindFirstObjectByType<GameDirector>();
            if (!game || !game.specialStage) throw new InvalidOperationException("Author the Night Market stage first.");
            specialAccess = new SpecialModeTestAccess();
            originalDuration = game.roundDuration;
            originalStart = game.startingStageIndex;
            originalRepeat = game.repeatStartingStage;
            hadNormalBest = PlayerPrefs.HasKey(GameDirector.BestKey);
            normalBest = PlayerPrefs.GetInt(GameDirector.BestKey);
            hadSpecialBest = PlayerPrefs.HasKey(GameDirector.SpecialBestKey);
            specialBest = PlayerPrefs.GetInt(GameDirector.SpecialBestKey);
            report = new Report { startFrame = Time.frameCount };
            step = row = expected = arcIndex = 0;
            due = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
        }

        static void Check(string name, bool passed, string detail)
            => report.checks.Add(new CheckResult { name = name, passed = passed, detail = detail });

        public static bool HasClaimedAppearance(RingTarget target)
        {
            if (!target.gameObject.activeInHierarchy) return false;
            bool checkedColor = false;
            var block = new MaterialPropertyBlock();
            foreach (var surface in target.GetComponentsInChildren<Renderer>(true))
            {
                var materials = surface.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (!materials[i] || !materials[i].HasProperty("_BaseColor")) continue;
                    if (!surface.enabled || !surface.gameObject.activeInHierarchy) return false;
                    checkedColor = true; surface.GetPropertyBlock(block, i);
                    Color original = materials[i].GetColor("_BaseColor"), actual = block.GetColor("_BaseColor");
                    if (!block.HasProperty("_BaseColor") || actual.maxColorComponent >= original.maxColorComponent * .5f
                        || Mathf.Abs(actual.a - original.a) > .001f) return false;
                }
            }
            return checkedColor && target.GetComponentsInChildren<Collider>(true).All(c => c.enabled && c.gameObject.activeInHierarchy);
        }

        public static bool HasOriginalAppearance(RingTarget target)
        {
            var block = new MaterialPropertyBlock();
            foreach (var surface in target.GetComponentsInChildren<Renderer>(true))
            {
                var materials = surface.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (!materials[i] || !materials[i].HasProperty("_BaseColor")) continue;
                    surface.GetPropertyBlock(block, i);
                    if (block.HasProperty("_BaseColor") && block.GetColor("_BaseColor") != materials[i].GetColor("_BaseColor")) return false;
                }
            }
            return true;
        }

        static void StartSpecial()
        {
            game.StartSpecialRound();
            game.thrower.SetCursor(false);
        }

        static Projectile Launch(Vector3 position, Vector3 velocity)
        {
            var previous = new HashSet<Projectile>(game.Projectiles.Where(p => p.InFlight));
            if (!game.Launch(position, velocity)) throw new InvalidOperationException("Test launch rejected.");
            return game.Projectiles.First(p => p.InFlight && !previous.Contains(p));
        }

        static void Drop(RingTarget target, float offset = 0)
            => shot = Launch(target.CapturePosition + Vector3.up * .36f + Vector3.right * offset, Vector3.down * 1.5f);

        static void ValidateLayout()
        {
            var targets = game.specialStage.GetComponentsInChildren<RingTarget>(true);
            int[] points = { 20, 50, 100, 250, 500, 1000 };
            int[] counts = { 11, 11, 7, 7, 5, 5 };
            float[] spacing = { 1, 1, 2, 2, 3, 3 };
            rows = points.Select(p => targets.Where(t => t.points == p)
                .OrderBy(t => Mathf.Abs(t.transform.position.x - game.thrower.LaunchPosition.x)).First()).ToArray();
            bool valid = targets.Select(t => t.points).Distinct().OrderBy(p => p).SequenceEqual(points);
            bool countValid = targets.Length == counts.Sum(), rising = true, supported = true, visible = true;
            Physics.SyncTransforms();
            for (int i = 0; i < points.Length; i++)
            {
                var line = targets.Where(t => t.points == points[i]).OrderBy(t => t.transform.position.x).ToArray();
                float z = line[0].transform.position.z;
                valid &= line.Length >= 2 && line.All(t => Mathf.Abs(t.transform.position.z - z) < .001f);
                countValid &= line.Length == counts[i];
                for (int j = 1; j < line.Length; j++)
                    valid &= Mathf.Abs(line[j].transform.position.x - line[j - 1].transform.position.x - .58f * spacing[i]) < .005f;
                if (i > 0)
                {
                    valid &= z > rows[i - 1].transform.position.z;
                    rising &= Mathf.Abs(rows[i].transform.position.y - rows[i - 1].transform.position.y - .45f) < .001f;
                }
                foreach (var target in line)
                {
                    var plinth = target.transform.Find("Prize plinth").GetComponent<Collider>();
                    Vector3 bottom = new Vector3(plinth.bounds.center.x, plinth.bounds.min.y, plinth.bounds.center.z);
                    var support = Physics.RaycastAll(bottom + Vector3.up * .02f, Vector3.down, .06f, ~((1 << 8) | (1 << 9)), QueryTriggerInteraction.Ignore)
                        .Where(h => !h.collider.transform.IsChildOf(target.transform)).OrderBy(h => h.distance).FirstOrDefault();
                    supported &= support.collider && Mathf.Abs(support.point.y - bottom.y) < .012f;
                }
                Vector3 viewport = game.thrower.view.WorldToViewportPoint(rows[i].CapturePosition);
                Vector3 ray = rows[i].CapturePosition - game.thrower.view.transform.position;
                bool clear = !Physics.Raycast(game.thrower.view.transform.position, ray.normalized, out var obstruction,
                    ray.magnitude, ~((1 << 8) | (1 << 9)), QueryTriggerInteraction.Ignore)
                    || obstruction.collider.GetComponentInParent<RingTarget>() == rows[i];
                visible &= viewport.z > 0 && viewport.x > 0 && viewport.x < 1 && viewport.y > 0 && viewport.y < 1 && clear;
            }
            Check("Six distance-ordered rows use the requested points and ring-diameter spacing", valid,
                "Targets=" + targets.Length + "; points=" + string.Join("/", points));
            Check("Every row has more targets, totalling 46 prizes", countValid, "Counts=" + string.Join("/", counts));
            Check("Rear prize rows stand progressively higher", rising, "Base heights=" + string.Join("/", rows.Select(t => t.transform.position.y.ToString("0.00"))));
            Check("Every prize plinth rests on a solid support", supported, "Checked=" + targets.Length);
            Check("All six centre prizes are visible from the resting camera", visible, "Viewport bounds and unobstructed capture sections");
            arcTargets = rows.Concat(new[] { targets.Where(t => t.points == 1000).OrderByDescending(t => Mathf.Abs(t.transform.position.x)).First() }).ToArray();
            Check("Special entry uses a separate stage and a 20-second clock", game.IsSpecialMode
                && game.ActiveStage == game.specialStage && game.CurrentRoundDuration == 20 && game.Remaining > 19,
                "Remaining=" + game.Remaining + "; regular stages=" + game.stages.Length);
            var ring = game.ActiveStage.data.projectilePrefab.GetComponent<RingFlight>();
            var spheres = ring.GetComponentsInChildren<SphereCollider>(true);
            bool hole = spheres.Length >= 16 && spheres.All(c =>
            {
                Vector3 center = ring.transform.InverseTransformPoint(c.transform.TransformPoint(c.center));
                float radial = new Vector2(center.x, center.z).magnitude;
                return radial - c.radius * c.transform.lossyScale.x >= ring.innerRadius - .003f;
            });
            Check("Physical ring colliders leave the centre open", hole,
                "Sphere colliders=" + spheres.Length + "; inner radius=" + ring.innerRadius);
            Check("Special scoring excludes regular clean/rebound bonuses", game.ActiveStage.data.cleanShotBonus == 0
                && game.ActiveStage.data.pointsPerPropCollision == 0, "Clean=" + game.ActiveStage.data.cleanShotBonus);
            var held = game.thrower.heldItems[game.ActiveStage.data.heldItemIndex].transform;
            Check("Held ring front is slightly higher than its back", held.TransformPoint(Vector3.forward * .26f).y
                > held.TransformPoint(Vector3.back * .26f).y + .005f, "Held angles=" + held.localEulerAngles);
        }

        static void ValidateGeometry()
        {
            bool Cross(Vector3 before, Vector3 after, Quaternion rotation = default)
            {
                if (rotation == default) rotation = Quaternion.identity;
                return RingTarget.ContainsDownwardCrossing(before, rotation, after, rotation,
                    Vector3.zero, Vector3.zero, .23f, .07f);
            }
            Check("Swept centred descent captures a target", Cross(Vector3.up * 2, Vector3.down * 2), "Four metre sweep");
            Check("Upward and side entries are rejected", !Cross(Vector3.down, Vector3.up)
                && !Cross(new Vector3(-1, 0, 0), new Vector3(1, 0, 0)), "Direction boundaries");
            Check("Side overlap without full hole clearance is rejected", !Cross(new Vector3(.2f, 1, 0), new Vector3(.2f, -1, 0)),
                "Offset .2 > inner-minus-neck .16");
            Check("A vertical ring is rejected", !Cross(Vector3.up, Vector3.down, Quaternion.Euler(90, 0, 0)), "Ninety degree tilt");
            Check("A ring starting below the capture section is rejected", !Cross(Vector3.down * .1f, Vector3.down), "Below-to-below");
        }

        // Aim through the actual controller: rotating the camera moves its child throw
        // origin. The solver includes that movement, drag and wind on each trial.
        static bool PredictAim(float pitch, float yaw, RingTarget target, out Vector3 crossing, out float flightTime)
        {
            var data = game.ActiveStage.data;
            game.thrower.view.transform.localRotation = Quaternion.Euler(pitch, yaw, 0);
            Vector3 position = game.thrower.LaunchPosition, velocity = game.thrower.LaunchVelocity;
            Vector3 spin = data.physicsProfile ? data.physicsProfile.initialSpin : Vector3.zero;
            flightTime = 0;
            for (int i = 0; i < 200; i++)
            {
                Vector3 previous = position;
                ProjectileFlight.Step(ref position, ref velocity, ref spin, data.physicsProfile,
                    game.ActiveStage.CurrentWind * data.windInfluence, data.gravityScale, Time.fixedDeltaTime);
                flightTime += Time.fixedDeltaTime;
                if (velocity.y < 0 && previous.y >= target.CapturePosition.y && position.y <= target.CapturePosition.y)
                {
                    float fraction = (previous.y - target.CapturePosition.y) / (previous.y - position.y);
                    crossing = Vector3.Lerp(previous, position, fraction);
                    return true;
                }
            }
            crossing = position;
            return false;
        }

        static void LaunchArc(RingTarget target)
        {
            // A shallow ring arc needs room for its front tube to clear a bottle cap.
            // Aim within the rear half of the valid capture area, keeping the whole
            // neck inside the hole; the actual scored throw remains the verdict.
            Vector3 aimPoint = target.CapturePosition + (target.points == 100 ? Vector3.forward * .08f : Vector3.zero);
            Vector3 local = game.thrower.view.transform.parent.InverseTransformDirection(aimPoint - game.thrower.view.transform.position);
            float initialYaw = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -38, 38);
            float bestPitch = 0, bestYaw = initialYaw, bestError = float.PositiveInfinity;
            for (float pitch = -25; pitch <= 30; pitch += .2f)
            {
                float yaw = initialYaw;
                for (int adjust = 0; adjust < 3; adjust++)
                {
                    if (!PredictAim(pitch, yaw, target, out var crossing, out var flightTime)) break;
                    Vector3 offset = crossing - aimPoint;
                    float error = new Vector2(offset.x, offset.z).magnitude;
                    if (error < bestError) { bestError = error; bestPitch = pitch; bestYaw = yaw; lastFlightTime = flightTime; }
                    Vector3 from = crossing - game.thrower.LaunchPosition, to = aimPoint - game.thrower.LaunchPosition;
                    from.y = to.y = 0;
                    yaw = Mathf.Clamp(yaw + Vector3.SignedAngle(from, to, Vector3.up), -38, 38);
                }
            }
            // Refine the best coarse sample without changing any legal input limits.
            float pitchCentre = bestPitch, yawCentre = bestYaw;
            for (float pitch = Mathf.Max(-25, pitchCentre - .25f); pitch <= Mathf.Min(30, pitchCentre + .25f); pitch += .025f)
            for (float yaw = Mathf.Max(-38, yawCentre - .1f); yaw <= Mathf.Min(38, yawCentre + .1f); yaw += .025f)
            {
                if (!PredictAim(pitch, yaw, target, out var crossing, out var flightTime)) continue;
                Vector3 offset = crossing - aimPoint;
                float error = new Vector2(offset.x, offset.z).magnitude;
                if (error < bestError) { bestError = error; bestPitch = pitch; bestYaw = yaw; lastFlightTime = flightTime; }
            }
            bool crosses = PredictAim(bestPitch, bestYaw, target, out var predictedCapture, out lastFlightTime);
            float captureOffset = Vector3.Distance(predictedCapture, target.CapturePosition);
            float holeClearance = game.ActiveStage.data.projectilePrefab.GetComponent<RingFlight>().WorldInnerRadius - target.WorldCaptureRadius;
            var previous = new HashSet<Projectile>(game.Projectiles.Where(p => p.InFlight));
            bool thrown = game.thrower.TryThrow();
            shot = game.Projectiles.FirstOrDefault(p => p.InFlight && !previous.Contains(p));
            Check("Legal controller throw reaches the predicted capture section for " + target.name, thrown && shot
                && crosses && bestError < .06f && captureOffset < holeClearance
                && Mathf.Abs(game.thrower.LaunchVelocity.magnitude - game.ActiveStage.data.launchSpeed) < .001f,
                "Pitch=" + bestPitch + "; yaw=" + bestYaw + "; predicted error=" + bestError + "; capture offset=" + captureOffset
                + "; hole clearance=" + holeClearance + "; origin=" + game.thrower.LaunchPosition);
        }

        static void ExpireNow() => typeof(GameDirector).GetField("roundEnd", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(game, Time.realtimeSinceStartupAsDouble - .001);

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < due) return;
            try
            {
                double delay = 1.25;
                switch (step++)
                {
                    case 0:
                        game.ReturnToMenu();
                        game.roundDuration = 37;
                        game.hud.specialButton.onClick.Invoke(); game.thrower.SetCursor(false);
                        ValidateLayout(); ValidateGeometry();
                        initialToken = game.StageToken;
                        windBefore = game.ActiveStage.CurrentWind;
                        Drop(rows[0]);
                        break;
                    case 1:
                        expected += rows[row].points;
                        Check("Falling ring scores row " + (row + 1) + " exactly once", game.Hits == row + 1 && game.Score == expected,
                            "Score=" + game.Score + "; expected=" + expected + "; hits=" + game.Hits);
                        Check("Row " + (row + 1) + " hit stays in the special stage", game.ActiveStage == game.specialStage
                            && !game.IsTransitioning && game.StageToken == initialToken, "Token=" + game.StageToken);
                        int before = game.Score;
                        game.RegisterRingHit(shot, rows[row]);
                        Check("Resolved row " + (row + 1) + " cannot score again", before == game.Score, "Score=" + game.Score);
                        Check("Captured row " + (row + 1) + " stays visible and dark with colliders intact", rows[row].IsClaimed
                            && HasClaimedAppearance(rows[row]), "Claimed=" + rows[row].IsClaimed);
                        var secondRing = Launch(rows[row].CapturePosition + Vector3.up * .36f, Vector3.down);
                        game.RegisterRingHit(secondRing, rows[row]);
                        Check("A second ring cannot claim row " + (row + 1) + " again", game.Score == before
                            && !secondRing.Scored && !rows[row].TryScore(secondRing), "Score=" + game.Score);
                        secondRing.Retire();
                        row++;
                        if (row < rows.Length) { Drop(rows[row]); step = 1; }
                        else
                        {
                            Check("Wind varies slowly during the round", (game.ActiveStage.CurrentWind - windBefore).magnitude > .001f
                                && (game.ActiveStage.CurrentWind - windBefore).magnitude < 2,
                                "Before=" + windBefore + "; after=" + game.ActiveStage.CurrentWind);
                            StartSpecial(); Drop(rows[0], .29f);
                        }
                        break;
                    case 2:
                        Check("A physical off-centre miss earns no points", game.Score == 0 && game.Hits == 0, "Score=" + game.Score);
                        shot.Retire();
                        shot.Launch(game.ActiveStage.data, game.StageToken - 1, rows[0].CapturePosition + Vector3.up * .3f,
                            Vector3.down * 5, game.ActiveStage.CurrentWind);
                        game.RegisterRingHit(shot, rows[0]);
                        Check("An old stage token is rejected", game.Score == 0 && !rows[0].TryScore(shot), "Shot token=" + shot.StageToken);
                        StartSpecial(); LaunchArc(arcTargets[0]); delay = Math.Max(1.25, lastFlightTime + 1.25);
                        break;
                    case 3:
                        // The old neck-crossing solver predicts entry, not whether
                        // the ring survives the ensuing bounce under the new rule.
                        bool retained = shot && shot.InFlight && arcTargets[arcIndex].EnclosedAtPose(
                            shot.Body.position, shot.Body.rotation, shot.GetComponent<RingFlight>());
                        Check("Controller throw rewards retention and rejects escape for " + arcTargets[arcIndex].name,
                            retained ? game.Score == arcTargets[arcIndex].points && game.Hits == 1
                                : game.Score == 0 && game.Hits == 0,
                            "Retained=" + retained + "; score=" + game.Score + "; collisions=" + (shot ? shot.CollisionCount : -1));
                        arcIndex++;
                        if (arcIndex < arcTargets.Length)
                        {
                            StartSpecial(); LaunchArc(arcTargets[arcIndex]);
                            delay = Math.Max(1.25, lastFlightTime + 1.25); step = 3;
                        }
                        break;
                    case 4:
                        game.hud.restartButton.onClick.Invoke(); game.thrower.SetCursor(false);
                        Check("Replay keeps special mode and clears the previous score", game.IsSpecialMode && game.Score == 0
                            && game.Hits == 0 && game.Throws == 0 && game.Remaining > 19, "Remaining=" + game.Remaining);
                        var restored = game.specialStage.GetComponentsInChildren<RingTarget>(true);
                        Check("Replay restores every prize's original color and eligibility", restored.Length == 46
                            && restored.All(target => !target.IsClaimed && target.gameObject.activeInHierarchy && HasOriginalAppearance(target)), "Restored=" + restored.Length);
                        shot = Launch(new Vector3(100, 10, 100), Vector3.zero);
                        remoteVelocity = shot.Body.linearVelocity;
                        windBefore = game.ActiveStage.CurrentWind;
                        break;
                    case 5:
                        Check("Live wind changes an airborne ring's horizontal velocity", new Vector2(shot.Body.linearVelocity.x,
                            shot.Body.linearVelocity.z).magnitude > .00001f && shot.Body.linearVelocity != remoteVelocity,
                            "Wind=" + windBefore + "; velocity=" + shot.Body.linearVelocity);
                        StartSpecial();
                        shot = Launch(rows[0].CapturePosition + Vector3.up * .3f, Vector3.down);
                        ExpireNow();
                        game.RegisterRingHit(shot, rows[0]);
                        Check("The wall-clock deadline rejects late ring rewards immediately", !game.CanScore && game.Score == 0, "Score=" + game.Score);
                        break;
                    case 6:
                        Check("Timeout shows a result and retires every projectile", !game.IsPlaying && game.hud.resultPanel.activeSelf
                            && game.Projectiles.All(p => !p.InFlight), "Playing=" + game.IsPlaying);
                        game.hud.menuButton.onClick.Invoke();
                        Check("Result menu button restores the main menu", game.hud.menuPanel.activeSelf && !game.IsPlaying,
                            "Menu=" + game.hud.menuPanel.activeSelf);
                        game.startingStageIndex = 0; game.repeatStartingStage = false;
                        game.hud.startButton.onClick.Invoke(); game.thrower.SetCursor(false);
                        Check("Normal entry retains all six original stages", !game.IsSpecialMode && game.stages.Length == 6
                            && game.ActiveStage == game.stages[0] && !game.specialStage.gameObject.activeSelf,
                            "Stage=" + game.ActiveStage.name);
                        shot = Launch(game.thrower.LaunchPosition, Vector3.forward);
                        shot.MarkScored(); game.RegisterHit(shot); delay = .6;
                        break;
                    case 7:
                        Check("Normal successful shots still advance to the next stage", game.ActiveStage == game.stages[1]
                            && game.Hits == 1 && !game.IsTransitioning, "Stage=" + game.ActiveStage.name);
                        Check("Player loop advances throughout validation", Time.frameCount > report.startFrame + 30,
                            "Frames=" + (Time.frameCount - report.startFrame));
                        Finish(); return;
                }
                due = EditorApplication.timeSinceStartup + delay;
            }
            catch (Exception exception)
            {
                Check("Validation exception", false, exception.ToString()); Finish();
            }
        }

        static void RestoreBest(string key, bool existed, int value)
        {
            if (existed) PlayerPrefs.SetInt(key, value);
            else PlayerPrefs.DeleteKey(key);
        }

        static void Finish()
        {
            EditorApplication.update -= Tick;
            if (game)
            {
                game.ReturnToMenu();
                game.roundDuration = originalDuration;
                game.startingStageIndex = originalStart;
                game.repeatStartingStage = originalRepeat;
            }
            RestoreBest(GameDirector.BestKey, hadNormalBest, normalBest);
            RestoreBest(GameDirector.SpecialBestKey, hadSpecialBest, specialBest);
            specialAccess?.Dispose(); specialAccess = null;
            PlayerPrefs.Save();
            report.endFrame = Time.frameCount;
            report.passed = report.checks.All(c => c.passed);
            Directory.CreateDirectory("Docs/Validation");
            File.WriteAllText("Docs/Validation/NightMarketPlayMode.json", JsonUtility.ToJson(report, true));
            Debug.Log("Night Market validation: " + report.checks.Count(c => c.passed) + "/" + report.checks.Count);
        }
    }
}

