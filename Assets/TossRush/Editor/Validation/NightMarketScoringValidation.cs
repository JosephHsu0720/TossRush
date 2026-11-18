using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TossRush.Editor
{
    // Controlled poses isolate bounce-out/cancel boundaries; real PhysX drops
    // prove that every authored prize can still retain and reward a real ring.
    public static class NightMarketScoringValidation
    {
        [Serializable] public sealed class Check { public string name; public bool passed; public string detail; }
        [Serializable] public sealed class Report { public bool passed, complete; public int startFrame, endFrame; public List<Check> checks = new List<Check>(); }
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static readonly FieldInfo Previous = typeof(Projectile).GetField("<PreviousPosition>k__BackingField", Private);
        static readonly FieldInfo Rotation = typeof(Projectile).GetField("<PreviousScoringRotation>k__BackingField", Private);
        static GameDirector game;
        static SpecialModeTestAccess specialAccess;
        static RingTarget[] targets;
        static RingTarget target;
        static Projectile shot;
        static Report report;
        static IEnumerator<double> sequence;
        static double due;
        static Quaternion tilt;
        static AudioSource[] audio;
        static bool[] audioMute;
        static bool originalAssist, running;

        public static void Begin()
        {
            if (!EditorApplication.isPlaying || running) throw new InvalidOperationException("Enter Play Mode first; do not overlap validations.");
            game = UnityEngine.Object.FindFirstObjectByType<GameDirector>();
            specialAccess = new SpecialModeTestAccess();
            targets = game.specialStage.GetComponentsInChildren<RingTarget>(true).OrderBy(t => t.points).ThenBy(t => t.transform.position.x).ToArray();
            target = targets.First(t => t.points == 1000);
            tilt = Quaternion.Euler(game.specialStage.data.projectileEulerAngles);
            originalAssist = game.thrower.ringAim.AimAssistEnabled;
            game.thrower.ringAim.AimAssistEnabled = false;
            audio = UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            audioMute = audio.Select(a => a.mute).ToArray(); foreach (var source in audio) source.mute = true;
            report = new Report { startFrame = Time.frameCount };
            sequence = Run(); due = EditorApplication.timeSinceStartup;
            running = true; EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += OnPlayMode;
            AssemblyReloadEvents.beforeAssemblyReload += Reload;
        }
        static void Add(string name, bool passed, string detail = "") => report.checks.Add(new Check { name = name, passed = passed, detail = detail });
        static void Start()
        {
            Release(); game.StartSpecialRound(); game.thrower.SetCursor(false);
            typeof(GameDirector).GetField("<Best>k__BackingField", Private).SetValue(game, int.MaxValue);
        }
        static Projectile Launch(Vector3 position, Vector3 velocity)
        {
            var active = new HashSet<Projectile>(game.Projectiles.Where(p => p.InFlight));
            if (!game.Launch(position, velocity)) throw new InvalidOperationException("Launch rejected.");
            return game.Projectiles.First(p => p.InFlight && !active.Contains(p));
        }
        static void Segment(float fromX, float fromY, float toX, float toY, Quaternion rotation)
        {
            Vector3 from = target.CapturePosition + new Vector3(fromX, fromY, 0);
            Vector3 to = target.CapturePosition + new Vector3(toX, toY, 0);
            if (!shot)
            {
                shot = Launch(from, Vector3.zero);
                shot.enabled = false;
                shot.Body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                shot.Body.isKinematic = true;
            }
            Previous.SetValue(shot, from); Rotation.SetValue(shot, rotation);
            shot.Body.position = to; shot.Body.rotation = rotation;
            shot.transform.SetPositionAndRotation(to, rotation);
            target.TryScore(shot);
        }
        static void Release()
        {
            if (!shot) return;
            shot.Body.isKinematic = false; shot.Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            shot.enabled = true; shot.Retire(); shot = null;
        }
        static bool Unclaimed() => game.Score == 0 && game.Hits == 0 && !target.IsClaimed && NightMarketValidation.HasOriginalAppearance(target);
        static IEnumerator<double> Run()
        {
            Start(); Segment(0, .15f, 0, -.12f, tilt);
            game.RegisterRingHit(shot, target);
            Add("Entry and direct registration cannot award before settlement", Unclaimed() && !shot.Scored);
            yield return .2;
            Add("Enclosed ring remains pending before the full hold duration", Unclaimed());
            Segment(0, -.12f, 0, .2f, tilt);
            yield return .5;
            Add("Ring bouncing upward out of the prize earns nothing", Unclaimed() && !shot.Scored);
            Segment(0, .2f, 0, -.12f, tilt);
            yield return .2;
            Add("Re-entry starts a fresh hold instead of retaining old progress", Unclaimed());
            yield return .3;
            Add("Retained re-entry scores exactly once", game.Score == target.points && game.Hits == 1 && shot.Scored);
            Add("Only confirmed captures turn dark and remain physical", target.IsClaimed && NightMarketValidation.HasClaimedAppearance(target));
            var duplicate = Launch(target.CapturePosition + Vector3.up * .3f, Vector3.down * 2);
            game.RegisterRingHit(duplicate, target);
            yield return 1.25;
            Add("A second real ring cannot claim the same dark prize", game.Score == target.points && game.Hits == 1 && !duplicate.Scored);

            Start(); Segment(0, .15f, 0, -.12f, tilt);
            yield return .2;
            Segment(0, -.12f, .4f, -.12f, tilt);
            yield return .5;
            Add("Sliding out sideways cancels pending capture", Unclaimed());

            Start(); Segment(0, .4f, 0, -.8f, tilt);
            yield return .5;
            Add("A fast sweep through the entire band without retention earns nothing", Unclaimed());

            Start(); Segment(.18f, .04f, .18f, -.005f, tilt);
            Add("Actual cap-edge geometry remains enclosed", target.EnclosedAtPose(shot.Body.position, tilt, shot.GetComponent<RingFlight>()));
            yield return .5;
            Add("Shallow cap overlap without deep insertion cannot settle", Unclaimed());

            Start(); Segment(.215f, .05f, .215f, -.005f, tilt);
            yield return .5;
            Add("Rim overlap cannot reward", Unclaimed());

            Start(); Segment(0, -.1f, 0, -.12f, tilt);
            yield return .5;
            Add("Originating below the cap has no valid top approach", Unclaimed());

            Start(); Segment(.4f, -.12f, 0, -.12f, Quaternion.identity);
            yield return .5;
            Add("Sideways insertion without top approach is rejected", Unclaimed());

            Start(); Segment(0, -.2f, 0, -.12f, tilt);
            yield return .5;
            Add("Upward insertion is rejected", Unclaimed());

            Start(); Segment(0, .15f, 0, -.12f, tilt);
            yield return .2;
            int previousId = shot.GetInstanceID();
            Release(); Segment(0, -.1f, 0, -.12f, tilt);
            yield return .5;
            Add("Pool reuse clears approach and pending settlement", previousId == shot.GetInstanceID() && Unclaimed());

            Start(); Segment(0, .15f, 0, -.12f, tilt);
            yield return .2;
            Release();
            yield return .5;
            Add("Retired pending ring cannot score", Unclaimed());

            Start(); Segment(0, .15f, 0, -.12f, tilt);
            yield return .2;
            typeof(GameDirector).GetField("roundEnd", Private).SetValue(game, Time.realtimeSinceStartupAsDouble - .001);
            yield return .4;
            Add("The 20-second deadline rejects unconfirmed captures", Unclaimed() && !game.IsPlaying);

            Start(); Segment(0, .15f, 0, -.12f, tilt);
            yield return .2;
            Start();
            yield return .5;
            Add("A new round clears pending capture", Unclaimed());
            Add("Restart restores all 46 prizes and original colors", targets.All(t => !t.IsClaimed && t.gameObject.activeInHierarchy
                && NightMarketValidation.HasOriginalAppearance(t)));

            Start(); Segment(.65f, .05f, .65f, -.04f, tilt);
            yield return .08;
            Segment(.65f, -.04f, .10f, -.40f, tilt);
            yield return .5;
            Add("A genuine lower-body roll-in still scores after settling", game.Score == target.points && target.IsClaimed);

            Start(); shot = Launch(target.CapturePosition + Vector3.up * .30f, Vector3.down * 2);
            yield return .16;
            Add("Real moving enclosure is not immediately rewarded", Unclaimed() && !shot.Scored);
            shot.Body.linearVelocity = Vector3.up * 5 + Vector3.right * 2;
            yield return 1;
            Add("A real PhysX rebound escaping the target receives zero", Unclaimed() && !shot.Scored
                && !target.EnclosedAtPose(shot.Body.position, shot.Body.rotation, shot.GetComponent<RingFlight>()));

            for (int i = 0; i < targets.Length; i++)
            {
                Start(); var item = targets[i];
                shot = Launch(item.CapturePosition + Vector3.up * .30f, Vector3.down * 2);
                yield return .16;
                Add("Physical entry waits for settlement " + (i + 1) + "/46", game.Score == 0 && !item.IsClaimed);
                yield return 1.15;
                Add("Retained physical drop " + (i + 1) + "/46 - " + item.name, game.Score == item.points && item.IsClaimed
                    && shot.Scored && game.Hits == 1 && NightMarketValidation.HasClaimedAppearance(item)
                    && item.EnclosedAtPose(shot.Body.position, shot.Body.rotation, shot.GetComponent<RingFlight>()),
                    "Points=" + item.points + "; score=" + game.Score + "; speed=" + shot.Body.linearVelocity.magnitude
                    + "; local height=" + (shot.Body.position.y - item.transform.position.y));
            }
        }
        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < due) return;
            try
            {
                if (!sequence.MoveNext()) { Finish(null); return; }
                due = EditorApplication.timeSinceStartup + sequence.Current;
            }
            catch (Exception exception) { Add("Validation exception", false, exception.ToString()); Finish(exception.ToString()); }
        }
        static void Finish(string failure)
        {
            if (!running) return; running = false;
            EditorApplication.update -= Tick; EditorApplication.playModeStateChanged -= OnPlayMode;
            AssemblyReloadEvents.beforeAssemblyReload -= Reload;
            sequence?.Dispose(); Release();
            if (game) { game.ReturnToMenu(); game.thrower.ringAim.AimAssistEnabled = originalAssist; }
            specialAccess?.Dispose(); specialAccess = null;
            for (int i = 0; i < audio.Length; i++) if (audio[i]) audio[i].mute = audioMute[i];
            report.complete = failure == null;
            report.endFrame = Time.frameCount; report.passed = report.complete && report.checks.All(c => c.passed);
            Directory.CreateDirectory("Docs/Validation");
            File.WriteAllText("Docs/Validation/NightMarketScoring.json", JsonUtility.ToJson(report,true));
            Debug.Log("Ring scoring validation: " + report.checks.Count(c => c.passed) + "/" + report.checks.Count);
        }
        static void Reload() => Finish("Script reload");
        static void OnPlayMode(PlayModeStateChange state) { if (state == PlayModeStateChange.ExitingPlayMode) Finish("Play Mode ended"); }
    }
}
