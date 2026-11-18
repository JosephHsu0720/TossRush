using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TossRush.Editor
{
    // Same imperfect camera aim, real controller/contacts, with and without assistance.
    public static class NightMarketAimValidation
    {
        [Serializable] public sealed class ShotResult
        {
            public string target;
            public int points, score;
            public bool assistedMode, green, actualHit;
            public float pitchOffset, yawOffset, correctionDegrees, guideMilliseconds;
        }
        [Serializable] public sealed class Report
        {
            public bool passed, complete;
            public int baselineHits, assistedHits, assistedGreenShots;
            public List<ShotResult> shots = new List<ShotResult>();
            public string failure;
        }
        static GameDirector game;
        static SpecialModeTestAccess specialAccess;
        static Report report;
        static readonly MethodInfo Solve = typeof(RingAimGuidance).GetMethod("Solve", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo Best = typeof(GameDirector).GetField("<Best>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        static List<(RingTarget target,Vector2 offset)> cases;
        static int index, phase;
        static double due;
        static Vector2 aim;
        static ShotResult current;
        static Vector3 savedTilt;
        static float savedCorrection;
        static bool savedAudio;
        static bool savedAssistEnabled;
        static bool running;

        public static void Begin()
        {
            if (!EditorApplication.isPlaying || running) throw new InvalidOperationException("Enter Play Mode; one validation at a time.");
            game = UnityEngine.Object.FindFirstObjectByType<GameDirector>();
            specialAccess = new SpecialModeTestAccess();
            savedTilt=game.specialStage.data.projectileEulerAngles;
            savedCorrection=game.thrower.ringAim.maximumCorrectionDegrees;
            savedAssistEnabled=game.thrower.ringAim.AimAssistEnabled;
            savedAudio=AudioListener.pause; AudioListener.pause=true;
            var targets=game.specialStage.GetComponentsInChildren<RingTarget>(true);
            cases=new List<(RingTarget,Vector2)>();
            foreach(int value in new[]{20,50,100,250,500,1000})
                cases.Add((targets.Where(t=>t.points==value).OrderBy(t=>Mathf.Abs(t.transform.position.x)).First(),new Vector2(.7f,.7f)));
            var gold=targets.Where(t=>t.points==1000).OrderBy(t=>Mathf.Abs(t.transform.position.x)).First();
            var outer=targets.Where(t=>t.points==1000).OrderByDescending(t=>t.transform.position.x).First();
            foreach(var target in new[]{gold,outer})
                foreach(var offset in new[]{new Vector2(1.2f,0),new Vector2(-1.2f,0),new Vector2(0,1.2f),new Vector2(0,-1.2f)})
                    cases.Add((target,offset));
            report=new Report(); index=phase=0; due=EditorApplication.timeSinceStartup;
            running=true; EditorApplication.update+=Tick;
        }

        static void Start()
        {
            UnityEngine.Random.InitState(4400+index);
            game.specialStage.data.projectileEulerAngles=savedTilt;
            game.StartSpecialRound(); game.thrower.SetCursor(false);
            Best.SetValue(game,int.MaxValue);
        }
        static Vector2 Ideal(RingTarget target)
        {
            for(int i=0;i<4;i++)
            {
                var velocity=(Vector3)Solve.Invoke(game.thrower.ringAim,new object[]{game.ActiveStage,target,game.thrower.LaunchPosition});
                Vector3 local=game.thrower.view.transform.parent.InverseTransformDirection(velocity.normalized);
                var angles=new Vector2(game.ActiveStage.data.throwLoft-Mathf.Asin(local.y)*Mathf.Rad2Deg,Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg);
                game.thrower.view.transform.localRotation=Quaternion.Euler(angles.x,angles.y,0);
            }
            var e=game.thrower.view.transform.localEulerAngles;
            return new Vector2(Mathf.DeltaAngle(0,e.x),Mathf.DeltaAngle(0,e.y));
        }
        static void Launch(bool enabled)
        {
            var item=cases[index];
            if(!enabled) aim=Ideal(item.target)+item.offset;
            aim.x=Mathf.Clamp(aim.x,-25,30); aim.y=Mathf.Clamp(aim.y,-38,38);
            game.thrower.view.transform.localRotation=Quaternion.Euler(aim.x,aim.y,0);
            game.ActiveStage.data.projectileEulerAngles=enabled?savedTilt:new Vector3(-8,0,0);
            game.thrower.ringAim.maximumCorrectionDegrees=enabled?savedCorrection:0;
            game.thrower.ringAim.AimAssistEnabled=enabled;
            var watch=System.Diagnostics.Stopwatch.StartNew(); game.thrower.ringAim.Refresh(true); watch.Stop();
            current=new ShotResult{target=item.target.name,points=item.target.points,assistedMode=enabled,
                pitchOffset=item.offset.x,yawOffset=item.offset.y,green=game.thrower.ringAim.ReadyTarget==item.target,
                correctionDegrees=game.thrower.ringAim.CorrectionDegrees,guideMilliseconds=(float)watch.Elapsed.TotalMilliseconds};
            if(!game.thrower.TryThrow())throw new InvalidOperationException("Throw rejected.");
        }
        static void Tick()
        {
            if(EditorApplication.timeSinceStartup<due)return;
            try
            {
                if(phase==0){Start();Launch(false);phase=1;due=EditorApplication.timeSinceStartup+1.8;return;}
                if(phase==1)
                {
                    current.score=game.Score; current.actualHit=cases[index].target.IsClaimed; report.shots.Add(current);
                    Start(); Launch(true); phase=2;due=EditorApplication.timeSinceStartup+1.8;return;
                }
                current.score=game.Score;current.actualHit=cases[index].target.IsClaimed;report.shots.Add(current);
                index++;phase=0;due=EditorApplication.timeSinceStartup+.05;
                if(index>=cases.Count)Finish(null);
            }
            catch(Exception exception){Finish(exception.ToString());}
        }
        static void Finish(string failure)
        {
            running=false;EditorApplication.update-=Tick;
            game.specialStage.data.projectileEulerAngles=savedTilt;
            game.thrower.ringAim.maximumCorrectionDegrees=savedCorrection; AudioListener.pause=savedAudio;
            game.thrower.ringAim.AimAssistEnabled=savedAssistEnabled;
            game.ReturnToMenu();
            specialAccess?.Dispose(); specialAccess = null;
            report.failure=failure;report.complete=failure==null&&index==cases.Count;
            report.baselineHits=report.shots.Count(s=>!s.assistedMode&&s.actualHit);
            report.assistedHits=report.shots.Count(s=>s.assistedMode&&s.actualHit);
            report.assistedGreenShots=report.shots.Count(s=>s.assistedMode&&s.green);
            report.passed=report.complete&&report.assistedHits>report.baselineHits
                &&report.shots.Where(s=>s.assistedMode&&s.green).All(s=>s.actualHit&&s.score==s.points)
                &&report.shots.All(s=>s.correctionDegrees<=savedCorrection+.01f);
            Directory.CreateDirectory("Docs/Validation");
            File.WriteAllText("Docs/Validation/NightMarketAimAssist.json",JsonUtility.ToJson(report,true));
            Debug.Log("Ring aim validation: baseline="+report.baselineHits+", assisted="+report.assistedHits+", green="+report.assistedGreenShots+", passed="+report.passed);
        }
    }
}
