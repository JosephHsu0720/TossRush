using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TossRush.Editor
{
    // Observational play simulation. Uses the real wall-clock deadline, controller,
    // projectile pool, PhysX contacts and target rewards. Never grants test rewards.
    public static class NightMarketPlayerSimulation
    {
        [Serializable] public sealed class RoundResult
        {
            public int round, seed, score, throws, hits;
            public double wallSeconds, physicsSeconds;
            public int[] claimedByRow;
        }
        [Serializable] public sealed class Summary
        {
            public string model = "Trajectory-assisted, high-value-first simulated player; not measured human players";
            public string timing = "Real 20-second wall-clock rounds, normal time scale and physics step";
            public int requestedRounds, completedRounds, seed = 20261117, maximumAvailableScore;
            public float aimErrorStdDegrees = .65f, roundBiasStdDegrees = .15f;
            public float initialAimSeconds = 1f, minimumAimSeconds = .85f, maximumAimSeconds = 1.15f;
            public double meanScore, medianScore, sampleStdDev, mean95CiLow, mean95CiHigh;
            public double meanThrows, meanHits, hitRate, percentile10, percentile90;
            public int minimumScore, maximumScore;
            public bool complete;
            public string failure;
            public List<RoundResult> rounds = new List<RoundResult>();
        }
        static readonly FieldInfo BestField = typeof(GameDirector).GetField("<Best>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        static GameDirector game;
        static Summary summary;
        static RingTarget[] targets;
        static readonly Dictionary<RingTarget, Vector2> calibration = new Dictionary<RingTarget, Vector2>();
        static readonly Dictionary<RingTarget, double> reserved = new Dictionary<RingTarget, double>();
        static System.Random random;
        static UnityEngine.Random.State previousRandom;
        static bool running, roundStarted, previousThrowerEnabled, previousAudioPause;
        static float previousTimeScale, previousFixedStep, pitchBias, yawBias;
        static Vector3 cameraPosition, originOffset;
        static Quaternion cameraParentRotation;
        static double roundStart, physicsStart, due;
        static Vector2 plannedAngles, currentAngles;
        static RingTarget plannedTarget;
        static float plannedFlight;
        static int previousSpecialBest;
        static bool hadSpecialBest;

        public static void Begin(int rounds = 30, int seed = 20261117)
        {
            if (running) throw new InvalidOperationException("Simulation is already running.");
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
            game = UnityEngine.Object.FindFirstObjectByType<GameDirector>();
            if (!game || !game.specialStage) throw new InvalidOperationException("Night Market stage is missing.");
            summary = new Summary { requestedRounds = rounds, seed = seed };
            targets = game.specialStage.GetComponentsInChildren<RingTarget>(true);
            summary.maximumAvailableScore = targets.Sum(t => t.points);
            previousThrowerEnabled = game.thrower.enabled;
            previousAudioPause = AudioListener.pause;
            previousTimeScale = Time.timeScale; previousFixedStep = Time.fixedDeltaTime;
            previousRandom = UnityEngine.Random.state;
            specialAccess = new SpecialModeTestAccess();
            hadSpecialBest = PlayerPrefs.HasKey(GameDirector.SpecialBestKey);
            previousSpecialBest = PlayerPrefs.GetInt(GameDirector.SpecialBestKey, 0);
            game.ReturnToMenu();
            game.thrower.enabled = false;
            AudioListener.pause = true;
            Time.timeScale = 1;
            // Calibration uses the same launch geometry without spending a round's clock.
            cameraPosition = game.thrower.view.transform.position;
            cameraParentRotation = game.thrower.view.transform.parent.rotation;
            originOffset = game.thrower.view.transform.InverseTransformPoint(game.thrower.throwOrigin.position);
            calibration.Clear();
            foreach (var target in targets) calibration[target] = FindAim(target, Vector3.zero, out _);
            running = true; roundStarted = false; due = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += OnPlayMode;
            AssemblyReloadEvents.beforeAssemblyReload += Interrupted;
            WriteReport();
        }

        public static string Status()
        {
            if (summary == null) return "No simulation has run.";
            return JsonUtility.ToJson(new StatusInfo { running = running, completed = summary.rounds.Count,
                requested = summary.requestedRounds, currentScore = game && game.IsPlaying ? game.Score : 0,
                remaining = game ? game.Remaining : 0, mean = summary.rounds.Count > 0 ? summary.rounds.Average(r => r.score) : 0 });
        }
        [Serializable] sealed class StatusInfo { public bool running; public int completed, requested, currentScore; public float remaining; public double mean; }

        static void StartRound()
        {
            int seed = summary.seed + summary.rounds.Count;
            random = new System.Random(seed);
            UnityEngine.Random.InitState(seed);
            game.StartSpecialRound();
            // Prevent simulated scores from replacing the user's saved record.
            BestField.SetValue(game, int.MaxValue);
            game.thrower.SetCursor(false);
            roundStart = Time.realtimeSinceStartupAsDouble; physicsStart = Time.fixedTimeAsDouble;
            pitchBias = Gaussian() * summary.roundBiasStdDegrees;
            yawBias = Gaussian() * summary.roundBiasStdDegrees;
            currentAngles = new Vector2(game.thrower.restingPitch, 0);
            reserved.Clear();
            Plan(summary.initialAimSeconds);
            roundStarted = true;
        }

        static void Tick()
        {
            if (!running) return;
            try
            {
                if (!roundStarted)
                {
                    if (EditorApplication.timeSinceStartup >= due) StartRound();
                    return;
                }
                if (!game.IsPlaying)
                {
                    var result = new RoundResult { round = summary.rounds.Count + 1, seed = summary.seed + summary.rounds.Count,
                        score = game.Score, throws = game.Throws, hits = game.Hits,
                        wallSeconds = Time.realtimeSinceStartupAsDouble - roundStart,
                        physicsSeconds = Time.fixedTimeAsDouble - physicsStart,
                        claimedByRow = new[] {20,50,100,250,500,1000}.Select(p => targets.Count(t => t.points == p && t.IsClaimed)).ToArray() };
                    if (result.score != targets.Where(t => t.IsClaimed).Sum(t => t.points) || result.score > summary.maximumAvailableScore)
                        throw new InvalidOperationException("Score does not match uniquely claimed prizes.");
                    summary.rounds.Add(result);
                    WriteReport();
                    if (summary.rounds.Count >= summary.requestedRounds) { Finish(null); return; }
                    roundStarted = false; due = EditorApplication.timeSinceStartup + .15;
                    return;
                }
                // Move at a plausible mouse-turn rate while the aim interval elapses.
                currentAngles.x = Mathf.MoveTowards(currentAngles.x, plannedAngles.x, Time.unscaledDeltaTime * 120);
                currentAngles.y = Mathf.MoveTowards(currentAngles.y, plannedAngles.y, Time.unscaledDeltaTime * 120);
                game.thrower.view.transform.localRotation = Quaternion.Euler(currentAngles.x, currentAngles.y, 0);
                if (Time.realtimeSinceStartupAsDouble < due || !game.CanThrow || !plannedTarget) return;
                if (plannedTarget.IsClaimed) { Plan(.25f); return; }
                if (Vector2.Distance(currentAngles, plannedAngles) > .1f) return;
                if (game.thrower.TryThrow()) reserved[plannedTarget] = Time.realtimeSinceStartupAsDouble + plannedFlight + .25;
                Plan(Mathf.Lerp(summary.minimumAimSeconds, summary.maximumAimSeconds, (float)random.NextDouble()));
            }
            catch (Exception exception) { Finish(exception.ToString()); }
        }

        static void Plan(float aimSeconds)
        {
            double now = Time.realtimeSinceStartupAsDouble;
            plannedTarget = targets.Where(t => !t.IsClaimed && (!reserved.TryGetValue(t, out var until) || until <= now))
                .OrderByDescending(t => t.points).ThenBy(t => Mathf.Abs(t.CapturePosition.x - cameraPosition.x)).FirstOrDefault();
            if (!plannedTarget)
            {
                plannedAngles = currentAngles; due = now + .1; return;
            }
            var ideal = FindAim(plannedTarget, game.ActiveStage.CurrentWind, out plannedFlight);
            plannedAngles = new Vector2(Mathf.Clamp(ideal.x + pitchBias + Gaussian() * summary.aimErrorStdDegrees, -25, 30),
                Mathf.Clamp(ideal.y + yawBias + Gaussian() * summary.aimErrorStdDegrees, -38, 38));
            due = now + aimSeconds;
        }

        static Vector2 FindAim(RingTarget target, Vector3 wind, out float flight)
        {
            Vector3 goal = target.CapturePosition + Vector3.forward * .05f;
            float yaw = Mathf.Atan2(goal.x - cameraPosition.x, goal.z - cameraPosition.z) * Mathf.Rad2Deg;
            Vector2 best = new Vector2(5, yaw); float error = float.PositiveInfinity;
            if (calibration.TryGetValue(target, out var cached)) best = cached;
            else
            {
                for (float p = -25; p <= 30; p += 1)
                {
                    if (!Predict(new Vector2(p,yaw), target, wind, out var point, out _)) continue;
                    float distance = new Vector2(point.x-goal.x, point.z-goal.z).sqrMagnitude;
                    if (distance < error) { error = distance; best = new Vector2(p,yaw); }
                }
            }
            for (int iteration = 0; iteration < 6; iteration++)
            {
                if (!Predict(best,target,wind,out var point,out _)
                    || !Predict(best+new Vector2(.05f,0),target,wind,out var pp,out _)
                    || !Predict(best+new Vector2(0,.05f),target,wind,out var py,out _)) break;
                float ex=point.x-goal.x, ez=point.z-goal.z;
                float a=(pp.x-point.x)/.05f,b=(py.x-point.x)/.05f,c=(pp.z-point.z)/.05f,d=(py.z-point.z)/.05f;
                float det=a*d-b*c;
                if (Mathf.Abs(det)<.000001f) break;
                best.x=Mathf.Clamp(best.x+Mathf.Clamp((-ex*d+ez*b)/det,-3,3),-25,30);
                best.y=Mathf.Clamp(best.y+Mathf.Clamp((ex*c-ez*a)/det,-3,3),-38,38);
            }
            Predict(best,target,wind,out _,out flight);
            return best;
        }

        static bool Predict(Vector2 angles, RingTarget target, Vector3 wind, out Vector3 crossing, out float flight)
        {
            StageData data = game.specialStage.data;
            Quaternion rotation = cameraParentRotation * Quaternion.Euler(angles.x, angles.y, 0);
            Vector3 position = cameraPosition + rotation * originOffset;
            Vector3 velocity = Quaternion.AngleAxis(-data.throwLoft,rotation*Vector3.right) * (rotation*Vector3.forward)*data.launchSpeed;
            Vector3 heading=Vector3.ProjectOnPlane(velocity,Vector3.up);
            Vector3 normal=Quaternion.LookRotation(heading,Vector3.up)*Quaternion.Euler(data.projectileEulerAngles)*Vector3.up;
            flight=0;
            for(int i=0;i<200;i++)
            {
                Vector3 before=position;
                ProjectileFlight.Step(ref position,ref velocity,data.physicsProfile,wind*data.windInfluence,data.gravityScale,Time.fixedDeltaTime);
                flight+=Time.fixedDeltaTime;
                float from=Vector3.Dot(before-target.CapturePosition,normal),to=Vector3.Dot(position-target.CapturePosition,normal);
                if(from>0&&to<=0&&velocity.y<0) { crossing=Vector3.Lerp(before,position,from/(from-to)); return true; }
            }
            crossing=position; return false;
        }

        static float Gaussian() => (float)(Math.Sqrt(-2*Math.Log(Math.Max(.0000001,random.NextDouble()))) * Math.Cos(2*Math.PI*random.NextDouble()));
        static double Percentile(double[] values, double p)
        {
            double index=(values.Length-1)*p; int low=(int)index,high=Math.Min(values.Length-1,low+1);
            return values[low]+(values[high]-values[low])*(index-low);
        }
        static void WriteReport()
        {
            summary.completedRounds=summary.rounds.Count;
            if(summary.rounds.Count>0)
            {
                double[] scores=summary.rounds.Select(r=>(double)r.score).OrderBy(s=>s).ToArray();
                summary.meanScore=scores.Average(); summary.medianScore=Percentile(scores,.5);
                summary.minimumScore=(int)scores.First(); summary.maximumScore=(int)scores.Last();
                summary.percentile10=Percentile(scores,.1); summary.percentile90=Percentile(scores,.9);
                summary.sampleStdDev=scores.Length>1?Math.Sqrt(scores.Sum(s=>(s-summary.meanScore)*(s-summary.meanScore))/(scores.Length-1)):0;
                double margin=2.045*summary.sampleStdDev/Math.Sqrt(scores.Length);
                summary.mean95CiLow=summary.meanScore-margin; summary.mean95CiHigh=summary.meanScore+margin;
                summary.meanThrows=summary.rounds.Average(r=>r.throws); summary.meanHits=summary.rounds.Average(r=>r.hits);
                summary.hitRate=summary.rounds.Sum(r=>r.hits)/(double)Math.Max(1,summary.rounds.Sum(r=>r.throws));
            }
            Directory.CreateDirectory("Docs/Validation");
            File.WriteAllText("Docs/Validation/NightMarketPlayerSimulation.json",JsonUtility.ToJson(summary,true));
            var lines=new List<string>{"round,seed,score,throws,hits,wall_seconds,physics_seconds,row20,row50,row100,row250,row500,row1000"};
            lines.AddRange(summary.rounds.Select(r=>string.Join(",",new[]{r.round.ToString(),r.seed.ToString(),r.score.ToString(),r.throws.ToString(),r.hits.ToString(),
                r.wallSeconds.ToString("F3",CultureInfo.InvariantCulture),r.physicsSeconds.ToString("F3",CultureInfo.InvariantCulture)}.Concat(r.claimedByRow.Select(n=>n.ToString())))));
            File.WriteAllLines("Docs/Validation/NightMarketPlayerSimulation.csv",lines);
        }
        static SpecialModeTestAccess specialAccess;
        static void Finish(string failure)
        {
            if(!running)return;
            running=false;
            EditorApplication.update-=Tick; EditorApplication.playModeStateChanged-=OnPlayMode;
            AssemblyReloadEvents.beforeAssemblyReload-=Interrupted;
            summary.failure=failure; summary.complete=failure==null&&summary.rounds.Count==summary.requestedRounds;
            if(game) { game.ReturnToMenu(); game.thrower.enabled=previousThrowerEnabled; }
            AudioListener.pause=previousAudioPause; Time.timeScale=previousTimeScale; Time.fixedDeltaTime=previousFixedStep;
            UnityEngine.Random.state=previousRandom;
            // Defensive restoration in case an unexpected reward bypassed the record guard.
            if(PlayerPrefs.HasKey(GameDirector.SpecialBestKey)!=hadSpecialBest||PlayerPrefs.GetInt(GameDirector.SpecialBestKey,0)!=previousSpecialBest)
            {
                if(hadSpecialBest)PlayerPrefs.SetInt(GameDirector.SpecialBestKey,previousSpecialBest);else PlayerPrefs.DeleteKey(GameDirector.SpecialBestKey);
                PlayerPrefs.Save();
            }
            WriteReport(); Debug.Log("Night Market player simulation finished: "+summary.rounds.Count+" rounds, mean="+summary.meanScore+", failure="+failure);
            specialAccess?.Dispose(); specialAccess = null;
        }
        public static void Stop() => Finish("Stopped before all requested rounds completed.");
        static void Interrupted() => Finish("Script reload interrupted simulation.");
        static void OnPlayMode(PlayModeStateChange state) { if(state==PlayModeStateChange.ExitingPlayMode)Finish("Play Mode ended during simulation."); }
    }
}
