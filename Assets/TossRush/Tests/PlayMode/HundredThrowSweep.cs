#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TossRush.Tests
{
    public sealed partial class PrototypeTests
    {
        static readonly int[] SweepCases = Enumerable.Range(0, 100).ToArray();

        [UnityTest]
        public IEnumerator GameAudio_SoftThrowsAndImpactsDuckMusic_ThenRestoreWithoutRestarting()
        {
            var audio = game.gameAudio;
            Assert.That(game.hitClip.name, Is.EqualTo("confirmation_001"));
            Assert.That(audio.start.name, Is.EqualTo("open_001"));
            Assert.That(audio.finish.name, Is.EqualTo("close_001"));
            Assert.That(audio.reward.name, Is.EqualTo("pluck_001"));
            Assert.That(audio.musicVolume, Is.EqualTo(.12f).Within(.001f));
            Assert.That(audio.effectsVolume, Is.EqualTo(.8f).Within(.001f));
            // Opt in only for this envelope test; authored gameplay keeps these cues off.
            audio.playThrowSounds = true;
            game.StartRound();
            Assert.That(game.thrower.TryThrow(), Is.True);
            Assert.That(audio.effectsSource.isPlaying, Is.True);
            Assert.That(audio.musicSource.volume, Is.EqualTo(audio.duckedMusicVolume).Within(.001f));
            int before = audio.musicSource.timeSamples;
            // This checks the audio envelope; later physical contacts have their own tests.
            game.Projectiles.Single(p => p.InFlight).Retire();
            yield return new WaitForSeconds(1.1f);
            Assert.That(audio.musicSource.volume, Is.EqualTo(audio.musicVolume).Within(.001f));
            Assert.That(audio.musicSource.timeSamples, Is.GreaterThan(before));
            audio.playCollisionSounds = true;
            audio.Impact(3);
            Assert.That(audio.musicSource.volume, Is.EqualTo(audio.duckedMusicVolume).Within(.001f));
            Assert.That(audio.effectsSource.isPlaying, Is.True);
            yield return new WaitForSeconds(1.1f);
            Assert.That(audio.musicSource.volume, Is.EqualTo(audio.musicVolume).Within(.001f));
            Assert.That(audio.musicSource.isPlaying, Is.True);
            audio.playCollisionSounds = false;
            audio.playThrowSounds = false;
        }

        [UnityTest]
        public IEnumerator GameAudio_MusicContinuesAcrossGoalAndRestart_WithoutDuplicateSources()
        {
            var audio = game.gameAudio;
            Assert.That(audio, Is.Not.Null);
            Assert.That(audio.musicSource, Is.Not.SameAs(audio.effectsSource));
            Assert.That(audio.musicSource.clip, Is.SameAs(audio.music));
            Assert.That(audio.musicSource.loop, Is.True);
            int sourceId = audio.musicSource.GetInstanceID();
            yield return new WaitForSeconds(.25f);
            int before = audio.musicSource.timeSamples;
            game.StartRound();
            yield return new WaitForSeconds(.25f);
            Assert.That(audio.musicSource.timeSamples, Is.GreaterThan(before), "Starting a round must not restart music.");
            before = audio.musicSource.timeSamples;
            var gate = game.ActiveStage.gate;
            Vector3 normal = gate.transform.TransformDirection(gate.entryNormal).normalized;
            Assert.That(game.Launch(gate.transform.position + normal * .6f, -normal * 8), Is.True);
            yield return WaitUntil(() => game.Hits == 1 && game.CanThrow);
            Assert.That(audio.musicSource.timeSamples, Is.GreaterThan(before), "Goal transitions must not restart music.");
            before = audio.musicSource.timeSamples;
            game.StartRound();
            yield return new WaitForSeconds(.25f);
            Assert.That(audio.musicSource.timeSamples, Is.GreaterThan(before), "Replay must keep music continuous.");
            Assert.That(audio.musicSource.GetInstanceID(), Is.EqualTo(sourceId));
            Assert.That(game.GetComponentsInChildren<AudioSource>(true).Length, Is.EqualTo(2));
            Assert.That(audio.impact && audio.penalty && audio.start && audio.finish, Is.True);
        }

        [UnityTest]
        public IEnumerator HundredThrowSweep([ValueSource(nameof(SweepCases))] int id)
        {
            string[] names = { "Corner", "Street", "Basketball", "Carnival", "Doorstep" };
            int stageIndex = id / 20, variant = id % 20;
            string stageName = names[stageIndex];
            // Resolve by authored order, so a renamed data asset cannot silently select another stage.
            Random.InitState(91600 + id);
            game.startingStageIndex = stageIndex;
            game.StartRound();
            yield return null;
            var data = game.ActiveStage.data;
            var gate = game.ActiveStage.gate;
            Vector3 normal = gate.transform.TransformDirection(gate.entryNormal).normalized;
            Projectile shot = null;
            string kind = "", verdict = "failed";
            int expected = -1;
            float yaw = 0, pitch = 0;
            Vector3 launch = Vector3.zero, velocity = Vector3.zero, last = Vector3.zero;
            int contacts = 0, pending = 0;
            try
            {
                if (variant < 4 || (variant >= 12 && variant <= 14))
                {
                    kind = variant < 4 ? "player_aimed" : "player_near_edge";
                    AimAtGate();
                    var euler = game.thrower.view.transform.localEulerAngles;
                    yaw = Mathf.DeltaAngle(0, euler.y);
                    pitch = Mathf.DeltaAngle(0, euler.x);
                    if (variant >= 12) { yaw += (variant - 13) * 2; pitch += 1.2f; }
                    game.thrower.view.transform.localRotation = Quaternion.Euler(pitch, yaw, 0);
                    // Pedestrians and bin lids can physically obstruct the mathematically aimed street arc.
                    if (variant < 4 && stageIndex != 1) expected = 1;
                }
                else if (variant < 12)
                {
                    kind = "player_angle_limit";
                    Vector2[] angles = { new Vector2(-38,-25), new Vector2(38,-25), new Vector2(-38,30), new Vector2(38,30),
                        new Vector2(0,-25), new Vector2(0,30), new Vector2(-38,6), new Vector2(38,6) };
                    yaw = angles[variant - 4].x; pitch = angles[variant - 4].y;
                    game.thrower.view.transform.localRotation = Quaternion.Euler(pitch, yaw, 0);
                }

                if (variant < 15)
                {
                    launch = game.thrower.LaunchPosition; velocity = game.thrower.LaunchVelocity;
                    Assert.That(game.thrower.TryThrow(), Is.True);
                    Assert.That(game.thrower.TryThrow(), Is.False, "Same-frame click must not duplicate a throw.");
                }
                else
                {
                    kind = new[] { "gate_front", "gate_reverse", "gate_outside", "gate_front_fast", "deadline" }[variant - 15];
                    launch = gate.transform.position + normal * .6f;
                    velocity = -normal * (variant == 18 ? 34 : 8);
                    expected = 1;
                    if (variant == 16) { launch = gate.transform.position - normal * .3f; velocity = normal * 8; expected = 0; }
                    // Clear all three street bins, not just the central opening.
                    if (variant == 17) { launch += gate.transform.right * 5; expected = 0; }
                    if (variant == 19)
                    {
                        // Use one real throw, then let the deadline retire it before it reaches the goal.
                        game.roundDuration = .12f; game.StartRound();
                        launch = game.thrower.LaunchPosition; velocity = Vector3.up * 20; expected = 0;
                    }
                    Assert.That(game.Launch(launch, velocity), Is.True);
                }
                shot = game.Projectiles.Single(p => p.InFlight);
                Assert.That(game.Throws, Is.EqualTo(1));
                // Inspect only the initial reverse crossing. A ball that later falls back
                // through the opening from above is a legitimate goal, covered separately.
                double end = Time.timeAsDouble + (variant == 16 ? .14 : 4.5);
                while (shot.InFlight && game.Hits == 0 && game.IsPlaying && Time.timeAsDouble < end)
                {
                    last = shot.Body.position; contacts = shot.CollisionCount; pending = shot.PendingCollisionPoints;
                    Assert.That(float.IsNaN(last.x) || float.IsInfinity(last.x) || float.IsNaN(last.y) || float.IsInfinity(last.y) || float.IsNaN(last.z) || float.IsInfinity(last.z), Is.False);
                    Assert.That(shot.transform.lossyScale.x, Is.GreaterThan(0));
                    yield return null;
                }
                contacts = shot.CollisionCount;
                Assert.That(game.Hits, Is.InRange(0, 1), "One throw must score at most once.");
                if (expected >= 0) Assert.That(game.Hits, Is.EqualTo(expected), kind + " " + data.name + " contacts=" + contacts + " last=" + last);
                if (game.Hits == 1)
                {
                    Assert.That(game.LastAward, Is.EqualTo(data.points + game.LastBonus + shot.ReboundCount * data.pointsPerPropCollision));
                    Assert.That(game.LastWasClean, Is.EqualTo(contacts == 0));
                }
                if (variant == 19)
                {
                    Assert.That(game.IsPlaying, Is.False);
                    Assert.That(game.hud.resultPanel.activeSelf, Is.True);
                    Assert.That(game.thrower.TryThrow(), Is.False);
                    game.hud.restartButton.onClick.Invoke();
                    Assert.That(game.IsPlaying, Is.True);
                    Assert.That(game.Throws, Is.Zero);
                }
                Assert.That(game.gameAudio, Is.Not.Null);
                Assert.That(game.gameAudio.musicSource.isPlaying, Is.True);
                Assert.That(game.gameAudio.musicSource.loop, Is.True);
                verdict = "passed";
            }
            finally
            {
                Directory.CreateDirectory("Docs/Validation");
                string path = "Docs/Validation/HundredThrowSweep.csv";
                if (!File.Exists(path)) File.WriteAllText(path, "id,stage,method,yaw,pitch,expectedHits,hits,contacts,pending,score,verdict,launchX,launchY,launchZ,velocityX,velocityY,velocityZ,lastX,lastY,lastZ\n");
                var c = System.Globalization.CultureInfo.InvariantCulture;
                File.AppendAllText(path, string.Join(",", new[] { id.ToString(), stageName, kind, yaw.ToString(c), pitch.ToString(c), expected.ToString(), game.Hits.ToString(), contacts.ToString(), pending.ToString(), game.Score.ToString(), verdict,
                    launch.x.ToString(c), launch.y.ToString(c), launch.z.ToString(c), velocity.x.ToString(c), velocity.y.ToString(c), velocity.z.ToString(c), last.x.ToString(c), last.y.ToString(c), last.z.ToString(c) }) + "\n");
            }
        }
    }
}
#endif
