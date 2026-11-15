#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TossRush.Tests
{
    public sealed partial class PrototypeTests
    {
        GameDirector game;
        int savedBest;
        bool hadBest;
        Random.State savedRandom;
        PhysicsMaterial testReboundSurface;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            hadBest=PlayerPrefs.HasKey("TossRush.Best.v1");
            savedBest=PlayerPrefs.GetInt("TossRush.Best.v1",0);
            savedRandom=Random.state;
            Random.InitState(73021);
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/TossRush/Scenes/TossRush.unity",new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            game=Object.FindFirstObjectByType<GameDirector>();
            Assert.That(game,Is.Not.Null);
            game.repeatStartingStage=false;
            game.roundDuration=20;
            game.startingStageIndex=0;
        }

        [TearDown]
        public void TearDown()
        {
            if(testReboundSurface)Object.DestroyImmediate(testReboundSurface);
            if(hadBest)PlayerPrefs.SetInt("TossRush.Best.v1",savedBest);
            else PlayerPrefs.DeleteKey("TossRush.Best.v1");
            PlayerPrefs.Save();
            Random.state=savedRandom;
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        }

        static IEnumerator WaitUntil(System.Func<bool> condition,float seconds=3,string context="",System.Func<string> observed=null)
        {
            double end=Time.timeAsDouble+seconds;
            while(!condition()&&Time.timeAsDouble<end)yield return null;
            Assert.That(condition(),Is.True,"Expected state did not arrive within "+seconds+" seconds. "+context+(observed==null?"":" "+observed()));
        }

        IEnumerator ReachCourt()
        {
            StartStage("Basketball");
            yield return null;
            Assert.That(game.Score,Is.Zero);
        }

        StageView Stage(string name) => game.stages.Single(s=>s.data.name==name);
        void StartStage(string name) { game.startingStageIndex=System.Array.IndexOf(game.stages,Stage(name));game.StartRound(); }

        // Find a player-reachable aim, then verify the result with real Rigidbody simulation.
        float FlightError(float yaw,float elevation)
        {
            var stage=game.ActiveStage;
            var data=stage.data;
            float cameraPitch=data.throwLoft-elevation;
            if(cameraPitch < -25 || cameraPitch > 30)return float.MaxValue;
            game.thrower.view.transform.localRotation=Quaternion.Euler(cameraPitch,yaw,0);
            Vector3 position=game.thrower.LaunchPosition,velocity=game.thrower.LaunchVelocity;
            Vector3 spin=data.physicsProfile.initialSpin;
            Vector3 normal=stage.gate.transform.TransformDirection(stage.gate.entryNormal);
            float before=Vector3.Dot(position-stage.PredictedGatePosition(0),normal);
            for(int i=1;i<150;i++)
            {
                Vector3 previous=position;
                ProjectileFlight.Step(ref position,ref velocity,ref spin,data.physicsProfile,stage.CurrentWind*data.windInfluence,data.gravityScale,Time.fixedDeltaTime);
                float time=i*Time.fixedDeltaTime;
                float after=Vector3.Dot(position-stage.PredictedGatePosition(time),normal);
                if(before>0&&after<=0)
                {
                    // A basketball must clear the near rim with its full radius.
                    if(data.name=="Basketball" && normal.y>0.9f && -velocity.y < new Vector2(velocity.x,velocity.z).magnitude*0.65f)
                        return float.MaxValue;
                    float f=before/(before-after);
                    Vector3 crossing=Vector3.Lerp(previous,position,f);
                    Vector3 target=stage.PredictedGatePosition((i-1+f)*Time.fixedDeltaTime);
                    return Vector3.ProjectOnPlane(crossing-target,normal).magnitude;
                }
                before=after;
            }
            return float.MaxValue;
        }

        void AimAtGate()
        {
            float best=float.MaxValue,bestYaw=0,bestElevation=20;
            // Straight newspaper throws follow both yaw and pitch without an added loft.
            bool horizontal=game.ActiveStage.data.horizontalThrow;
            for(float yaw=-24;yaw<=24;yaw+=2)
            for(float elevation=horizontal?-30:6;elevation<=(horizontal?25:68);elevation+=2)
            {
                float error=FlightError(yaw,elevation);
                if(error<best){best=error;bestYaw=yaw;bestElevation=elevation;}
            }
            foreach(float step in new[]{0.5f,0.1f})
            {
                float centerYaw=bestYaw,centerElevation=bestElevation;
                for(int x=-3;x<=3;x++)for(int y=-3;y<=3;y++)
                {
                    float yaw=centerYaw+x*step,elevation=centerElevation+y*step;
                    float error=FlightError(yaw,elevation);
                    if(error<best){best=error;bestYaw=yaw;bestElevation=elevation;}
                }
            }
            Assert.That(best,Is.LessThan(0.07f),"Target must be reachable within the player's aiming limits: "+game.ActiveStage.data.name);
            game.thrower.view.transform.localRotation=Quaternion.Euler(game.ActiveStage.data.throwLoft-bestElevation,bestYaw,0);
        }

        IEnumerator ReachCarnival()
        {
            StartStage("Carnival");
            yield return null;
            Assert.That(game.Score,Is.Zero);
        }

        [UnityTest]
        public IEnumerator FullTwentySecondRound_StaysOnMisses_StopsAtDeadline_AndRestarts()
        {
            Assert.That(game.hud.menuPanel.activeSelf,Is.True);
            game.hud.startButton.onClick.Invoke();
            double start=Time.timeAsDouble;
            var visited=new HashSet<StageView>();
            float last=game.Remaining;
            while(game.IsPlaying)
            {
                visited.Add(game.ActiveStage);
                Assert.That(game.Remaining,Is.LessThanOrEqualTo(last+0.001f));
                last=game.Remaining;
                yield return null;
            }
            Assert.That(Time.timeAsDouble-start,Is.InRange(20,20.3));
            Assert.That(visited.Count,Is.EqualTo(1),"Missing a target must never skip the stage.");
            Assert.That(game.hud.resultPanel.activeSelf,Is.True);
            Assert.That(game.CanThrow,Is.False);
            Assert.That(game.thrower.TryThrow(),Is.False);
            int result=game.Score;
            game.RegisterHit(game.Projectiles[0]);
            Assert.That(game.Score,Is.EqualTo(result),"Late hits must not alter the result.");
            Assert.That(game.Projectiles.Any(p=>p.InFlight),Is.False);
            game.hud.restartButton.onClick.Invoke();
            Assert.That(game.IsPlaying,Is.True);
            Assert.That(game.Remaining,Is.InRange(19.9f,20));
            Assert.That(game.Score,Is.Zero);
            Assert.That(game.Hits,Is.Zero);
            Assert.That(game.Throws,Is.Zero);
            Assert.That(game.ActiveStage,Is.SameAs(game.stages[0]));
        }

        [UnityTest]
        public IEnumerator NormalPaperThrow_ScoresOnce_AndClearsOldProjectiles()
        {
            game.StartRound();
            AimAtGate();
            Assert.That(game.thrower.TryThrow(),Is.True);
            Assert.That(game.thrower.TryThrow(),Is.False,"Throw interval must prevent duplicate input.");
            int token=game.StageToken;
            yield return WaitUntil(()=>game.Score>0);
            Assert.That(game.Score,Is.EqualTo(250));
            Assert.That(game.Hits,Is.EqualTo(1));
            Assert.That(game.LastBonus,Is.EqualTo(150));
            Assert.That(game.LastWasClean,Is.True);
            Assert.That(game.hud.cleanShotEffect.label.text,Does.Contain("+150"));
            Assert.That(game.CanThrow,Is.False,"A transition blocks throwing and scoring.");
            yield return WaitUntil(()=>game.StageToken>token&&game.CanThrow);
            Assert.That(game.Projectiles.Any(p=>p.InFlight),Is.False);
            game.RegisterHit(game.Projectiles[0]);
            Assert.That(game.Score,Is.EqualTo(250),"A stale stage token must be rejected.");
        }

        [UnityTest]
        public IEnumerator Crosswind_AcceleratesBasketball_AndCompensatedThrowScores()
        {
            yield return ReachCourt();
            AimAtGate();
            Assert.That(game.thrower.TryThrow(),Is.True);
            var shot=game.Projectiles.First(p=>p.InFlight);
            float x=shot.Body.position.x;
            float vx=shot.Body.linearVelocity.x;
            float windSign=Mathf.Sign(game.ActiveStage.CurrentWind.x);
            double launchedAt=Time.timeAsDouble;
            yield return new WaitForSeconds(0.25f);
            float elapsed=(float)(Time.timeAsDouble-launchedAt);
            Assert.That((shot.Body.position.x-x-vx*elapsed)*windSign,Is.GreaterThan(0.01f));
            Assert.That((shot.Body.linearVelocity.x-vx)*windSign,Is.GreaterThan(0.1f));
            yield return WaitUntil(()=>game.Score==300);
            Assert.That(game.Hits,Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Hoop_RejectsUpwardAndOutsideCrossings_AcceptsDownwardEntry()
        {
            yield return ReachCourt();
            Vector3 center=game.ActiveStage.gate.transform.position;
            game.Launch(center-Vector3.up*0.5f,Vector3.up*7);
            yield return new WaitForSeconds(0.3f);
            Assert.That(game.Score,Is.Zero,"Passing up through a hoop must not score.");
            foreach(var shot in game.Projectiles)shot.Retire();
            game.Launch(center+Vector3.right*1.2f+Vector3.up*0.4f,Vector3.down*3);
            yield return new WaitForSeconds(0.25f);
            Assert.That(game.Score,Is.Zero,"Crossing the plane outside the opening must not score.");
            game.Launch(center+Vector3.up*0.5f,Vector3.down*3);
            yield return WaitUntil(()=>game.Score==300);
        }

        [UnityTest]
        public IEnumerator Hoop_NearRimEntriesFromFourOffsets_ScoreOnceAfterEntering()
        {
            var data=Stage("Basketball").data; bool random=data.randomize; Vector3 wind=data.wind;
            try
            {
                data.randomize=false;data.wind=Vector3.zero;
                bool observedRimContact=false;
                foreach(float x in new[]{.54f,.58f,.62f,.66f})
                {
                    StartStage("Basketball");
                    Vector3 center=game.ActiveStage.gate.transform.position;
                    game.Launch(center+new Vector3(x,.30f,0),new Vector3(-.1f,-.5f,0));
                    var shot=game.Projectiles.First(p=>p.InFlight);
                    yield return WaitUntil(()=>game.Hits==1,2,"Basketball rim roll x="+x);
                    observedRimContact|=shot.CollisionCount>0;
                    Assert.That(game.LastWasClean,Is.EqualTo(shot.CollisionCount==0));
                    int expected=150+(shot.CollisionCount==0?150:shot.ReboundCount*30);
                    Assert.That(game.Score,Is.EqualTo(expected));
                    game.RegisterHit(shot);
                    Assert.That(game.Score,Is.EqualTo(expected),"One rim roll must not score twice.");
                }
                Assert.That(observedRimContact,Is.True,"This set must include a real rim contact, not only clean drops.");
            }
            finally {data.randomize=random;data.wind=wind;}
        }

        [UnityTest]
        public IEnumerator Street_FrontRimSlide_ScoresInAllThreeBins_WithCompressedBag()
        {
            var data=Stage("Street").data;bool random=data.randomize;Vector3 wind=data.wind;
            try
            {
                data.randomize=false;data.wind=Vector3.zero;
                for(int i=0;i<3;i++)
                {
                    StartStage("Street");
                    var gate=game.ActiveStage.movingTarget.GetComponentsInChildren<ScoreGate>().OrderBy(g=>g.transform.position.x).ElementAt(i);
                    Vector3 center=gate.transform.position;
                    game.Launch(center+new Vector3(0,.32f,-.4f),new Vector3(0,-.5f,.4f));
                    var shot=game.Projectiles.First(p=>p.InFlight);
                    yield return WaitUntil(()=>shot.CollisionCount>0||game.Hits>0,1);
                    Assert.That(shot.CollisionCount,Is.GreaterThan(0));
                    yield return WaitUntil(()=>game.Hits==1,2,"Street front rim slide bin="+i);
                    Assert.That(game.LastWasClean,Is.False);
                    Assert.That(game.Score,Is.EqualTo(200+shot.ReboundCount*30));
                    game.RegisterHit(shot);
                    Assert.That(game.Hits,Is.EqualTo(1));
                }
            }
            finally{data.randomize=random;data.wind=wind;}
        }

        [UnityTest]
        public IEnumerator Hoop_PendingEntryDoesNotCarryIntoReusedShot_OrAllowSideEntryBelowRim()
        {
            var data=Stage("Basketball").data;bool random=data.randomize;Vector3 wind=data.wind;
            try
            {
                data.randomize=false;data.wind=Vector3.zero;
                StartStage("Basketball");
                Vector3 center=game.ActiveStage.gate.transform.position;
                game.Launch(center+new Vector3(.60f,.30f,0),new Vector3(-.1f,-.5f,0));
                var shot=game.Projectiles.First(p=>p.InFlight);
                yield return WaitUntil(()=>game.Hits==0&&shot.ScoringPosition.y<center.y,1);
                shot.Retire();
                game.Launch(center+new Vector3(.8f,-.3f,0),Vector3.left*5);
                Assert.That(game.Projectiles.First(p=>p.InFlight),Is.SameAs(shot));
                yield return new WaitForSeconds(.35f);
                Assert.That(game.Hits,Is.Zero,"A new shot below the rim cannot inherit a previous rim-entry candidate.");
                Assert.That(game.Score,Is.Zero);
            }
            finally{data.randomize=random;data.wind=wind;}
        }

        [UnityTest]
        public IEnumerator Carnival_MovesAndRejectsReverseEntry_ThenScoresFromFront()
        {
            yield return ReachCarnival();
            var gate=game.ActiveStage.gate.transform;
            float x=gate.position.x;
            game.Launch(gate.position+Vector3.forward*0.45f,Vector3.back*8);
            yield return new WaitForSeconds(0.15f);
            Assert.That(game.Score,Is.Zero,"Back-to-front entry must not score.");
            Assert.That(Mathf.Abs(gate.position.x-x),Is.GreaterThan(0.03f));
            foreach(var shot in game.Projectiles)shot.Retire();
            game.Launch(gate.position+Vector3.back*0.6f,Vector3.forward*8);
            yield return WaitUntil(()=>game.Score==350);
            Assert.That(game.Hits,Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ProjectilePool_IsBounded_AndRestartResetsEveryShot()
        {
            game.StartRound();
            int count=Object.FindObjectsByType<Projectile>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length;
            int accepted=0;
            for(int i=0;i<100;i++)if(game.Launch(new Vector3(-4,2,-4),Vector3.up*2))accepted++;
            Assert.That(accepted,Is.EqualTo(game.poolSizePerStage));
            Assert.That(Object.FindObjectsByType<Projectile>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length,Is.EqualTo(count));
            game.StartRound();
            yield return null;
            Assert.That(game.Projectiles.Any(p=>p.InFlight),Is.False);
            Assert.That(game.Throws,Is.Zero);
        }

        [UnityTest]
        public IEnumerator StageVariations_StayInBounds_AndCameraKeepsLayoutVisible()
        {
            var homes=game.stages.Select(s=>s.movingTarget.localPosition).ToArray();
            game.StartRound();
            Assert.That(game.thrower.view.transform.localPosition.y,Is.EqualTo(1.65f).Within(0.01f));
            Assert.That(game.thrower.view.transform.localEulerAngles.x,Is.EqualTo(6).Within(0.01f));
            Assert.That(game.thrower.view.fieldOfView,Is.GreaterThanOrEqualTo(65));
            for(int index=0;index<game.stages.Length;index++)
            {
                var stage=game.stages[index];var data=stage.data;
                if(!data.randomize)
                {
                    stage.ResetStage();
                    Assert.That(stage.CurrentTargetCenter,Is.EqualTo(homes[index]));
                    Assert.That(stage.CurrentWind,Is.EqualTo(data.wind));
                    continue;
                }
                Vector3 originalWind=data.wind;
                bool left=false,right=false;var centers=new HashSet<Vector3>();
                for(int i=0;i<64;i++)
                {
                    stage.ResetStage();
                    Vector3 offset=stage.CurrentTargetCenter-homes[index];
                    centers.Add(stage.CurrentTargetCenter);
                    Assert.That(offset.x,Is.InRange(data.horizontalOffsetRange.x,data.horizontalOffsetRange.y));
                    Assert.That(offset.z,Is.InRange(data.depthOffsetRange.x,data.depthOffsetRange.y));
                    Assert.That(stage.CurrentWind.magnitude,Is.InRange(data.windSpeedRange.x-0.001f,data.windSpeedRange.y+0.001f));
                    left|=stage.CurrentWind.x<0;right|=stage.CurrentWind.x>0;
                }
                if(data.windSpeedRange.y>0)Assert.That(left&&right,Is.True,"Wind must vary between both directions.");
                else Assert.That(stage.CurrentWind,Is.EqualTo(Vector3.zero));
                Assert.That(centers.Count,Is.GreaterThan(1));
                Assert.That(data.wind,Is.EqualTo(originalWind),"Play-mode variation must not modify a shared data asset.");
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator PhysicsProfiles_ShareGravity_ButPaperRespondsMoreToWind()
        {
            var paper=Stage("Corner").data.physicsProfile;
            var ball=Stage("Basketball").data.physicsProfile;
            var bag=Stage("Carnival").data.physicsProfile;
            Assert.That(ProjectileFlight.Acceleration(paper,Vector3.zero,Vector3.zero,1),Is.EqualTo(Physics.gravity));
            Assert.That(ProjectileFlight.Acceleration(ball,Vector3.zero,Vector3.zero,1),Is.EqualTo(Physics.gravity));
            Assert.That(ProjectileFlight.Acceleration(bag,Vector3.zero,Vector3.zero,1),Is.EqualTo(Physics.gravity));
            float paperWind=ProjectileFlight.Acceleration(paper,Vector3.zero,Vector3.right*5,1).x;
            float ballWind=ProjectileFlight.Acceleration(ball,Vector3.zero,Vector3.right*5,1).x;
            Assert.That(paperWind,Is.GreaterThan(ballWind*2));
            Assert.That(paper.mass,Is.LessThan(bag.mass));
            Assert.That(bag.mass,Is.LessThan(ball.mass));
            yield return null;
        }

        [UnityTest]
        public IEnumerator PredictedFlight_MatchesRigidbodyWithAirResistance()
        {
            game.StartRound();
            var data=game.ActiveStage.data;
            Vector3 position=new Vector3(4,3,-4),velocity=new Vector3(0,3,8);
            game.Launch(position,velocity);
            var shot=game.Projectiles.First(p=>p.InFlight);
            float fixedStart=Time.fixedTime;
            yield return new WaitForSeconds(0.4f);
            int steps=Mathf.RoundToInt((Time.fixedTime-fixedStart)/Time.fixedDeltaTime);
            for(int i=0;i<steps;i++)ProjectileFlight.Step(ref position,ref velocity,data.physicsProfile,game.ActiveStage.CurrentWind*data.windInfluence,data.gravityScale,Time.fixedDeltaTime);
            Assert.That(Vector3.Distance(shot.Body.position,position),Is.LessThan(0.025f));
            Assert.That(Vector3.Distance(shot.Body.linearVelocity,velocity),Is.LessThan(0.025f));
            Assert.That(shot.Body.mass,Is.EqualTo(data.physicsProfile.mass));
        }

        [UnityTest]
        public IEnumerator SurfaceMaterials_BasketballBouncesMoreThanPaper()
        {
            game.StartRound();
            var shots=new Projectile[2];
            var sources=new[]{Stage("Corner"),Stage("Basketball")};
            for(int i=0;i<2;i++)
            {
                shots[i]=game.Projectiles[System.Array.IndexOf(game.stages,sources[i])*game.poolSizePerStage];
                shots[i].Launch(sources[i].data,game.StageToken,new Vector3(-4+i*1.5f,1.4f,-4),Vector3.zero,Vector3.zero);
            }
            float[] rebound=new float[2];
            double end=Time.timeAsDouble+1.5f;
            while(Time.timeAsDouble<end)
            {
                for(int i=0;i<2;i++)if(shots[i].Body.linearVelocity.y>0.15f)
                    rebound[i]=Mathf.Max(rebound[i],shots[i].Body.position.y-sources[i].data.projectileRadius);
                yield return null;
            }
            Assert.That(rebound[1],Is.GreaterThan(0.35f),"Rubber basketball must visibly rebound.");
            Assert.That(rebound[1],Is.GreaterThan(rebound[0]+0.2f));
        }

        [UnityTest]
        public IEnumerator RandomizedStages_CanBeScoredWithPlayerThrow_FromSeveralSeeds()
        {
            foreach(int seed in new[]{11,127,950,3001})
            {
                Random.InitState(seed);game.StartRound();
                for(int index=0;index<game.stages.Length;index++)
                {
                    Assert.That(game.ActiveStage,Is.SameAs(game.stages[index]));
                    AimAtGate();
                    Assert.That(game.thrower.TryThrow(),Is.True);
                    int targetHits=game.Hits+1;
                    var launchedStage=game.ActiveStage;
                    var shot=game.Projectiles.First(p=>p.InFlight);
                    yield return WaitUntil(()=>game.Hits==targetHits&&game.CanThrow,3,"Seed="+seed+", stage="+launchedStage.data.name,
                        ()=>"hits="+game.Hits+", contacts="+shot.CollisionCount+", radius="+shot.ScoringRadius+", offsets="+string.Join(";",launchedStage.movingTarget.GetComponentsInChildren<ScoreGate>().Select(g=>(shot.ScoringPosition-g.transform.position).ToString())));
                }
            }
        }

        [UnityTest]
        public IEnumerator CornerCleanShot_Awards150Bonus_AndUsesPaperPool()
        {
            StartStage("Corner");
            Assert.That(game.stages.Length,Is.EqualTo(5));
            Assert.That(game.Projectiles.Count,Is.EqualTo(game.poolSizePerStage*5));
            Assert.That(game.thrower.heldItems[0].activeSelf,Is.True);
            var center=game.ActiveStage.gate.transform.position;
            game.Launch(center+Vector3.up*0.5f,Vector3.down*3);
            yield return WaitUntil(()=>game.Hits==1);
            Assert.That(game.Score,Is.EqualTo(250));
            Assert.That(game.LastBonus,Is.EqualTo(150));
            Assert.That(game.hud.cleanShotEffect.label.text,Does.Contain("+150"));
        }

        [UnityTest]
        public IEnumerator Corner_RimRollIn_AwardsBaseAndContactsOnce_WithoutCleanBonus()
        {
            foreach (var entry in new[] { new Vector2(.7f, .25f), new Vector2(.7f, .5f), new Vector2(.75f, .5f) })
            {
                Random.InitState(73021);
                StartStage("Corner");
                var gate = game.ActiveStage.gate;
                Vector3 center = gate.transform.position;
                Assert.That(game.Launch(center + Vector3.right * entry.x + Vector3.up * .25f, Vector3.left * entry.y), Is.True);
                var shot = game.Projectiles.First(p => p.InFlight);
                yield return WaitUntil(() => shot.CollisionCount > 0 || game.Hits > 0, 1);
                Assert.That(shot.CollisionCount, Is.GreaterThan(0), "Expected rim contact for " + entry + "; score=" + game.Score);
                Assert.That(game.Hits, Is.Zero, "Touching the rim alone must not count as a goal.");
                Assert.That(game.Score, Is.Zero);
                Assert.That(shot.PendingCollisionPoints, Is.EqualTo(shot.ReboundCount * 30));
                Assert.That(shot.PropCollisionCount, Is.Zero, "The rim is part of the target.");
                yield return WaitUntil(() => game.Hits == 1, 2);
                int expected = 100 + shot.ReboundCount * 30;
                Assert.That(game.Score, Is.EqualTo(expected), "Paper rolling fully inside the bin must score.");
                Assert.That(game.LastWasClean, Is.False);
                Assert.That(game.LastBonus, Is.Zero);
                game.RegisterHit(shot);
                Assert.That(game.Score, Is.EqualTo(expected), "The same paper must score only once.");
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Corner_RejectsUpwardOutsideAndBelowRimSideEntry()
        {
            StartStage("Corner");
            Vector3 center = game.ActiveStage.gate.transform.position;
            game.Launch(center - Vector3.up * .3f, Vector3.up * 5);
            yield return new WaitForSeconds(.1f);
            Assert.That(game.Hits, Is.Zero, "Upward entry must not count as a goal.");
            foreach (var shot in game.Projectiles) shot.Retire();
            game.Launch(center + Vector3.right * .9f + Vector3.up * .25f, Vector3.zero);
            yield return new WaitForSeconds(.7f);
            Assert.That(game.Hits, Is.Zero, "Falling outside the bin must not count as a goal.");
            foreach (var shot in game.Projectiles) shot.Retire();
            // A shot already below the rim has not entered through the opening.
            game.Launch(center - Vector3.up * .15f, Vector3.down);
            yield return new WaitForSeconds(.15f);
            Assert.That(game.Hits, Is.Zero, "Being inside below the rim alone must not count as a goal.");
        }

        IEnumerator ContactWall(Projectile shot=null)
        {
            if(!testReboundSurface)
            {
                testReboundSurface=new PhysicsMaterial("Test rebound surface") { bounciness=1, bounceCombine=PhysicsMaterialCombine.Maximum };
                var wall=new GameObject("Test rebound wall");
                wall.transform.position=new Vector3(20.9f,5,-3);
                var collider=wall.AddComponent<BoxCollider>();collider.size=new Vector3(.1f,3,3);collider.sharedMaterial=testReboundSurface;
            }
            if(!shot)
            {
                Assert.That(game.Launch(new Vector3(20.4f,5,-3),Vector3.right*8),Is.True);
                shot=game.Projectiles.First(p=>p.InFlight);
            }
            else
            {
                // Leave the contact manifold before starting a second real contact episode.
                shot.Body.position=new Vector3(20,5,-3);shot.Body.linearVelocity=Vector3.zero;
                yield return new WaitForSeconds(0.06f);
                shot.Body.linearVelocity=Vector3.right*8;
            }
            int previous=shot.CollisionCount;
            yield return WaitUntil(()=>shot.CollisionCount>previous,1);
        }

        void DropExistingShotThroughGate(Projectile shot)
        {
            // Isolate scoring of an existing collision history after observing real contact events.
            shot.Body.position=game.ActiveStage.gate.transform.position+Vector3.up*0.55f;
            shot.Body.linearVelocity=Vector3.down*3;
        }

        [UnityTest]
        public IEnumerator CornerTwoContacts_Bank60OnGoal_AndReuseClearsCollisionHistory()
        {
            StartStage("Corner");
            yield return ContactWall();
            var shot=game.Projectiles.First(p=>p.InFlight);
            yield return ContactWall(shot);
            Assert.That(shot.CollisionCount,Is.EqualTo(2));
            Assert.That(shot.PropCollisionCount,Is.EqualTo(2));
            Assert.That(shot.PendingCollisionPoints,Is.EqualTo(60));
            Assert.That(game.Score,Is.Zero,"Contacts stay pending until this shot scores.");
            DropExistingShotThroughGate(shot);
            yield return WaitUntil(()=>game.Hits==1);
            Assert.That(game.LastWasClean,Is.False);
            Assert.That(game.LastBonus,Is.Zero,"Contact cancels the clean shot bonus.");
            Assert.That(game.Score,Is.EqualTo(160));
            Assert.That(game.LastAward,Is.EqualTo(160));
            Assert.That(shot.PendingCollisionPoints,Is.Zero,"Banked points must be consumed.");
            game.StartRound();
            game.Launch(new Vector3(0,2,-3),Vector3.up);
            shot=game.Projectiles.First(p=>p.InFlight);
            Assert.That(shot.CollisionCount,Is.Zero);
            Assert.That(shot.PropCollisionCount,Is.Zero);
            Assert.That(shot.PendingCollisionPoints,Is.Zero);
        }

        [UnityTest]
        public IEnumerator CollisionPoints_BelongOnlyToScoringShot_AndMissesAreDiscarded()
        {
            StartStage("Corner");
            yield return ContactWall();
            var missed=game.Projectiles.First(p=>p.InFlight);
            Assert.That(missed.PendingCollisionPoints,Is.GreaterThanOrEqualTo(30));
            // Keep another throw alive: its contacts must not be banked by a clean goal.
            Assert.That(game.Launch(new Vector3(0,4,-3),Vector3.up),Is.True);
            var clean=game.Projectiles.First(p=>p.InFlight&&p!=missed);
            Assert.That(clean.PendingCollisionPoints,Is.Zero);
            DropExistingShotThroughGate(clean);
            yield return WaitUntil(()=>game.Hits==1);
            Assert.That(game.Score,Is.EqualTo(250));
            Assert.That(game.LastAward,Is.EqualTo(250));
            Assert.That(missed.PendingCollisionPoints,Is.Zero,"Stage transition discards other throws.");
            game.RegisterHit(clean);
            Assert.That(game.Score,Is.EqualTo(250),"The award cannot be banked twice.");
        }

        [UnityTest]
        public IEnumerator CollisionPoints_ExpireWithoutScoring_AndCannotSurviveRoundEnd()
        {
            StartStage("Corner");
            yield return ContactWall();
            var shot=game.Projectiles.First(p=>p.InFlight);
            Assert.That(shot.PendingCollisionPoints,Is.GreaterThanOrEqualTo(30));
            shot.Body.position=new Vector3(0,-5,-3);
            yield return WaitUntil(()=>!shot.InFlight,1);
            Assert.That(shot.PendingCollisionPoints,Is.Zero);
            Assert.That(game.Score,Is.Zero);
            game.roundDuration=.8f;
            game.StartRound();
            yield return ContactWall();
            shot=game.Projectiles.First(p=>p.InFlight);
            Assert.That(shot.PendingCollisionPoints,Is.GreaterThanOrEqualTo(30));
            yield return WaitUntil(()=>!game.IsPlaying,2);
            Assert.That(game.Score,Is.Zero);
            Assert.That(game.Projectiles.All(p=>p.PendingCollisionPoints==0),Is.True);
        }

        [UnityTest]
        public IEnumerator OrdinaryCollision_CancelsCleanBonus()
        {
            StartStage("Basketball");
            yield return ContactWall();
            var shot=game.Projectiles.First(p=>p.InFlight);
            DropExistingShotThroughGate(shot);
            yield return WaitUntil(()=>game.Hits==1);
            Assert.That(game.Score,Is.EqualTo(180));
            Assert.That(game.LastBonus,Is.Zero);
            Assert.That(game.LastWasClean,Is.False);
        }

        [UnityTest]
        public IEnumerator TargetRimContact_Banks30OnGoal_AndCancelsCleanBonus()
        {
            Random.InitState(73021);
            StartStage("Corner");
            var center=game.ActiveStage.gate.transform.position;
            game.Launch(center+Vector3.right*.7f+Vector3.up*.25f,Vector3.left*.25f);
            var shot=game.Projectiles.First(p=>p.InFlight);
            yield return WaitUntil(()=>shot.CollisionCount>0,1);
            Assert.That(shot.PropCollisionCount,Is.Zero);
            Assert.That(game.Score,Is.Zero);
            Assert.That(shot.PendingCollisionPoints,Is.EqualTo(shot.ReboundCount*30));
            DropExistingShotThroughGate(shot);
            yield return WaitUntil(()=>game.Hits==1);
            Assert.That(game.Score,Is.EqualTo(100+shot.ReboundCount*30));
            Assert.That(game.LastBonus,Is.Zero);
        }

        [UnityTest]
        public IEnumerator PhysicsProps_MoveOnImpact_AndRestartRestoresPose()
        {
            StartStage("Corner");
            var prop=game.ActiveStage.GetComponentsInChildren<PhysicsProp>().First(p=>p.name.StartsWith("ToyBall"));
            Vector3 home=prop.transform.position;
            game.Launch(home+Vector3.back*0.6f,Vector3.forward*10);
            var shot=game.Projectiles.First(p=>p.InFlight);
            yield return WaitUntil(()=>shot.CollisionCount>0,1);
            yield return new WaitForSeconds(0.2f);
            Assert.That(Vector3.Distance(home,prop.transform.position),Is.GreaterThan(0.04f));
            game.StartRound();
            Assert.That(Vector3.Distance(home,prop.transform.position),Is.LessThan(0.01f));
            Assert.That(prop.GetComponent<Rigidbody>().linearVelocity.magnitude,Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator HighScoreIndicator_PersistsRecord_AndEditorToolClearsOnlyScore()
        {
            Assert.That(UnityEditor.EditorApplication.ExecuteMenuItem("TossRush/Clear High Score"),Is.True);
            Assert.That(PlayerPrefs.HasKey(GameDirector.BestKey),Is.False);
            Assert.That(game.Best,Is.Zero);
            game.roundDuration=1.1f;game.StartRound();
            var center=game.ActiveStage.gate.transform.position;
            game.Launch(center+Vector3.up*0.5f,Vector3.down*3);
            yield return WaitUntil(()=>game.Score>0);
            yield return null;
            Assert.That(game.hud.highScoreText.text,Does.Contain("NEW HIGH SCORE"));
            yield return WaitUntil(()=>!game.IsPlaying,2);
            Assert.That(PlayerPrefs.GetInt(GameDirector.BestKey),Is.EqualTo(game.Score));
            Assert.That(game.hud.resultText.text,Does.Contain("NEW HIGH SCORE!"));
            UnityEditor.EditorApplication.ExecuteMenuItem("TossRush/Clear High Score");
            Assert.That(game.Best,Is.Zero);
            Assert.That(PlayerPrefs.HasKey(GameDirector.BestKey),Is.False);
        }

        [UnityTest]
        public IEnumerator TargetsHaveOutline_AndHUDLeavesCenterClear()
        {
            foreach(var stage in game.stages)
            {
                Assert.That(stage.movingTarget.GetComponent<TargetMarker>(),Is.Not.Null);
                var outlines=stage.movingTarget.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name=="Outline").ToArray();
                Assert.That(outlines.Length,Is.GreaterThan(0));
                Assert.That(outlines.All(r=>r.sharedMaterial.shader.name=="TossRush/Target Outline"),Is.True);
                Assert.That(outlines.Any(r=>r.GetComponent<Collider>()),Is.False);
            }
            Assert.That(game.hud.stageText.transform.parent.name,Is.EqualTo("TopBar"));
            Assert.That(game.hud.playPanel.transform.Find("StageInfo"),Is.Null);
            Assert.That(game.hud.playPanel.transform.Find("StageTrack"),Is.Null);
            Assert.That(game.hud.cleanShotEffect.transform.parent,Is.EqualTo(game.hud.transform));
            Assert.That(game.hud.cleanShotEffect.transform.GetSiblingIndex(),Is.GreaterThan(game.hud.transition.transform.GetSiblingIndex()),"Clean shot FX must remain visible above the transition mask.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator StageOrder_UsesFiveProjectileTypes_AndHasNoBackdropTitles()
        {
            Assert.That(game.stages.Select(s=>s.data.name),Is.EqualTo(new[]{"Corner","Street","Basketball","Carnival","Doorstep"}));
            Assert.That(game.Projectiles.Count,Is.EqualTo(game.poolSizePerStage*5));
            Assert.That(game.stages.SelectMany(s=>s.GetComponentsInChildren<TMPro.TMP_Text>(true)),Is.Empty);
            var street=Stage("Street");
            Assert.That(street.data.points,Is.EqualTo(200));
            Assert.That(game.stages.All(s=>s.data.cleanShotBonus==150),Is.True);
            Assert.That(street.gate.openingHalfExtents.x,Is.GreaterThan(.5f));
            Assert.That(street.movingTarget.Find("Open Lid/Roll Surface").GetComponent<Collider>(),Is.Not.Null);
            Assert.That(game.hud.windIndicator,Is.Not.Null);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TrashBag_SpinsAndCurves_AndPredictionMatchesActualFlight()
        {
            StartStage("Street");
            var data=game.ActiveStage.data;
            Vector3 start=new Vector3(4,4,-4),position=start,velocity=new Vector3(0,2,8),spin=data.physicsProfile.initialSpin;
            Assert.That(game.Launch(position,velocity),Is.True);
            var shot=game.Projectiles.First(p=>p.InFlight);Quaternion rotation=shot.Body.rotation;
            float fixedStart=Time.fixedTime;
            yield return new WaitForSeconds(.4f);
            int steps=Mathf.RoundToInt((Time.fixedTime-fixedStart)/Time.fixedDeltaTime);
            for(int i=0;i<steps;i++)ProjectileFlight.Step(ref position,ref velocity,ref spin,data.physicsProfile,game.ActiveStage.CurrentWind,data.gravityScale,Time.fixedDeltaTime);
            Assert.That(shot.CollisionCount,Is.Zero);
            Assert.That(shot.Body.position.x-start.x,Is.GreaterThan(.2f),"Horizontal spin should produce a clearly visible sideways curve.");
            Assert.That(Quaternion.Angle(rotation,shot.Body.rotation),Is.GreaterThan(15));
            Assert.That(Vector3.Distance(position,shot.Body.position),Is.LessThan(.06f));
            Assert.That(Vector3.Distance(velocity,shot.Body.linearVelocity),Is.LessThan(.06f));
        }

        [UnityTest]
        public IEnumerator Street_LidContactRollsIntoOpening_AndAwardsBasePlusContactsOnce()
        {
            StartStage("Street");
            Transform lid=game.ActiveStage.movingTarget.Find("Open Lid");
            Vector3 outward=-lid.up;
            Vector3 start=lid.TransformPoint(new Vector3(0,-.065f,-.4f))+outward*(game.ActiveStage.data.projectileRadius+.03f);
            Assert.That(game.Launch(start,Vector3.down*.25f),Is.True);
            var shot=game.Projectiles.First(p=>p.InFlight);
            yield return WaitUntil(()=>shot.CollisionCount>0,1.5f);
            Assert.That(game.Hits,Is.Zero,"Contacting the lid alone cannot count as a goal.");
            Assert.That(game.Score,Is.Zero);
            Assert.That(shot.PendingCollisionPoints,Is.EqualTo(shot.ReboundCount*30));
            Assert.That(shot.PropCollisionCount,Is.Zero,"The lid is part of the target.");
            yield return WaitUntil(()=>game.Hits==1,3);
            int expected=200+shot.ReboundCount*30;
            Assert.That(game.Score,Is.EqualTo(expected));
            Assert.That(game.LastBonus,Is.Zero);
            Assert.That(game.LastWasClean,Is.False);
            game.RegisterHit(shot);
            Assert.That(game.Score,Is.EqualTo(expected));
        }

        [UnityTest]
        public IEnumerator StreetOpening_RejectsUpwardAndOutsideEntry_ThenAcceptsDirectDrop()
        {
            StartStage("Street");
            Vector3 center=game.ActiveStage.gate.transform.position;
            game.Launch(center-Vector3.up*.6f,Vector3.up*7);
            yield return new WaitForSeconds(.2f);
            Assert.That(game.Hits,Is.Zero);
            foreach(var shot in game.Projectiles)shot.Retire();
            game.Launch(center+Vector3.right*1.2f+Vector3.up*.5f,Vector3.down*3);
            yield return new WaitForSeconds(.3f);
            Assert.That(game.Hits,Is.Zero);
            foreach(var shot in game.Projectiles)shot.Retire();
            game.Launch(center+Vector3.up*.6f,Vector3.down*3);
            yield return WaitUntil(()=>game.Hits==1);
            Assert.That(game.LastAward,Is.EqualTo(350));
        }

        [UnityTest]
        public IEnumerator Traffic_MovesInOppositeDirections_AndRestartRestoresIt()
        {
            StartStage("Street");
            var cars=game.ActiveStage.GetComponentsInChildren<TrafficVehicle>();
            Assert.That(cars.Length,Is.EqualTo(4));
            Assert.That(cars.Count(c=>c.direction>0),Is.EqualTo(2));
            Assert.That(cars.Count(c=>c.direction<0),Is.EqualTo(2));
            var homes=cars.Select(c=>c.transform.localPosition).ToArray();
            yield return new WaitForSeconds(.25f);
            for(int i=0;i<cars.Length;i++)Assert.That((cars[i].transform.localPosition.x-homes[i].x)*cars[i].direction,Is.GreaterThan(.3f));
            game.StartRound();
            for(int i=0;i<cars.Length;i++)Assert.That(Vector3.Distance(cars[i].transform.localPosition,homes[i]),Is.LessThan(.01f));
        }

        [UnityTest]
        public IEnumerator Street_AllThreeBinsAcceptDrops_AndGapDoesNotCountAsGoal()
        {
            StartStage("Street");
            var gates=game.ActiveStage.movingTarget.GetComponentsInChildren<ScoreGate>(true);
            Assert.That(gates.Length,Is.EqualTo(3));
            var sorted=gates.OrderBy(g=>g.transform.position.x).ToArray();
            for(int i=1;i<3;i++)
            {
                float spacing=sorted[i].transform.position.x-sorted[i-1].transform.position.x;
                Assert.That(spacing,Is.EqualTo(2.2f).Within(.01f));
                Assert.That(spacing-1.52f,Is.GreaterThan(game.ActiveStage.data.projectileRadius*2));
            }
            for(int i=0;i<3;i++)
            {
                StartStage("Street");
                Vector3 center=sorted[i].transform.position;
                game.Launch(center+Vector3.up*.7f,Vector3.down*2);
                yield return WaitUntil(()=>game.Hits==1);
                Assert.That(game.Score,Is.EqualTo(350));
                Assert.That(game.ActiveStage.data.name,Is.EqualTo("Street"));
            }
            StartStage("Street");
            Vector3 gap=(sorted[0].transform.position+sorted[1].transform.position)*.5f;
            game.Launch(gap+Vector3.up*.7f,Vector3.down*2);
            yield return new WaitForSeconds(.6f);
            Assert.That(game.Hits,Is.Zero,"Between two bins is not an opening.");
        }

        [UnityTest]
        public IEnumerator Street_BagStartsSideways_AndSpinsAroundWorldUp()
        {
            StartStage("Street");
            var held=game.thrower.heldItems[game.ActiveStage.data.heldItemIndex].transform;
            Assert.That(Quaternion.Angle(held.localRotation,Quaternion.Euler(0,0,90)),Is.LessThan(.1f));
            Quaternion start=held.rotation;
            yield return new WaitForSeconds(.2f);
            Quaternion delta=held.rotation*Quaternion.Inverse(start);
            delta.ToAngleAxis(out float angle,out Vector3 axis);
            Assert.That(angle,Is.GreaterThan(20));
            Assert.That(Mathf.Abs(Vector3.Dot(axis,Vector3.up)),Is.GreaterThan(.999f));
            game.Launch(new Vector3(4,3,-4),Vector3.forward);
            var shot=game.Projectiles.First(p=>p.InFlight);
            Assert.That(Quaternion.Angle(shot.Body.rotation,Quaternion.Euler(0,0,90)),Is.LessThan(.1f));
            Assert.That(shot.Body.angularVelocity.y,Is.GreaterThan(10));
            game.StartRound();
            Assert.That(Quaternion.Angle(held.localRotation,Quaternion.Euler(0,0,90)),Is.LessThan(.1f));
        }

        [UnityTest]
        public IEnumerator ThemedProps_ContactsStayPendingUntilGoal_AndPropsReset()
        {
            foreach(string name in new[]{"Corner","Basketball","Carnival","Doorstep"})
            {
                StartStage(name);
                var props=game.ActiveStage.transform.Find("Scene Props").GetComponentsInChildren<PhysicsProp>();
                Assert.That(props.Length,Is.GreaterThanOrEqualTo(2));
                Assert.That(props.All(p=>p.GetComponentsInChildren<Collider>().Any(c=>!c.isTrigger&&c.attachedRigidbody==p.GetComponent<Rigidbody>())&&!p.GetComponent<Rigidbody>().isKinematic),Is.True);
                var prop=props.First(p=>p.GetComponent<BoxCollider>());
                Vector3 home=prop.transform.position;
                game.Launch(home+Vector3.back*.65f,Vector3.forward*6);
                var shot=game.Projectiles.First(p=>p.InFlight);
                yield return WaitUntil(()=>shot.CollisionCount>0,1);
                Assert.That(game.Hits,Is.Zero);
                Assert.That(game.Score,Is.Zero,name);
                Assert.That(shot.PendingCollisionPoints,Is.EqualTo(shot.ReboundCount*30),name);
                if(shot.ReboundCount>0)
                    Assert.That(game.hud.collisionFeedback.labels.Any(l=>l.gameObject.activeInHierarchy&&l.text.Contains("+30")&&l.text.Contains("PENDING")&&!l.raycastTarget),Is.True);
                shot.Retire();
                Assert.That(shot.PendingCollisionPoints,Is.Zero,"A miss discards its pending points.");
                int before=game.Score;
                game.RegisterCollision(shot,home);
                Assert.That(shot.PendingCollisionPoints,Is.Zero);
                Assert.That(game.Score,Is.EqualTo(before),"Retired projectiles cannot award points.");
                prop.GetComponent<Rigidbody>().AddForce(Vector3.right*.6f,ForceMode.Impulse);
                yield return new WaitForSeconds(.15f);
                Assert.That(Vector3.Distance(home,prop.transform.position),Is.GreaterThan(.03f));
                game.StartRound();
                Assert.That(Vector3.Distance(home,prop.transform.position),Is.LessThan(.001f));
                Assert.That(game.hud.collisionFeedback.labels.All(l=>!l.gameObject.activeSelf),Is.True);
            }
        }

        [UnityTest]
        public IEnumerator CollisionPopup_ProjectsWorldPoint_UsesBoundedPool_AndExpires()
        {
            StartStage("Street");
            var feedback=game.hud.collisionFeedback;
            var view=game.thrower.view;
            Vector3 point=view.ViewportToWorldPoint(new Vector3(.35f,.45f,5));
            game.hud.ShowCollision(30,point,view,Color.cyan);
            var label=feedback.labels[0];
            var canvas=feedback.GetComponentInParent<Canvas>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)feedback.transform,view.WorldToScreenPoint(point),canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,out var expected);
            Assert.That(Vector2.Distance(expected,label.rectTransform.anchoredPosition),Is.LessThan(.1f));
            Assert.That(label.gameObject.activeInHierarchy,Is.True);
            int count=feedback.GetComponentsInChildren<TMPro.TMP_Text>(true).Length;
            for(int i=0;i<80;i++)game.hud.ShowCollision(30,point,view,Color.cyan);
            Assert.That(feedback.GetComponentsInChildren<TMPro.TMP_Text>(true).Length,Is.EqualTo(count));
            yield return new WaitForSeconds(feedback.duration+.05f);
            Assert.That(feedback.labels.All(l=>!l.gameObject.activeSelf),Is.True);
            game.hud.ShowCollision(30,view.transform.position-view.transform.forward,view,Color.cyan);
            Assert.That(feedback.labels.All(l=>!l.gameObject.activeSelf),Is.True,"Contacts behind the camera must not appear on screen.");
        }

        [UnityTest]
        public IEnumerator ReboundThreshold_RejectsBoundaryAndWeakContacts_InEveryStage()
        {
            foreach(var stage in game.stages)
            {
                StartStage(stage.data.name);
                game.Launch(new Vector3(0,4,-3),Vector3.up);
                var shot=game.Projectiles.First(p=>p.InFlight);
                float threshold=stage.data.minimumReboundSpeed;
                Assert.That(threshold,Is.EqualTo(stage.data.name=="Street"?.1f:1.5f));
                game.RegisterCollision(shot,shot.Body.position,0);
                game.RegisterCollision(shot,shot.Body.position,threshold);
                Assert.That(shot.PendingCollisionPoints,Is.Zero,"The threshold must be exceeded, not equalled.");
                game.RegisterCollision(shot,shot.Body.position,threshold+.01f);
                Assert.That(shot.PendingCollisionPoints,Is.EqualTo(30));
                Assert.That(game.Score,Is.Zero,"Qualified rebounds still wait for a goal.");
                shot.Retire();
                game.RegisterCollision(shot,Vector3.zero,10);
                Assert.That(shot.PendingCollisionPoints,Is.Zero);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator RealRebound_Qualifies_WhileGentleContactDoesNot()
        {
            StartStage("Corner");
            yield return ContactWall();
            var strong=game.Projectiles.First(p=>p.InFlight);
            Assert.That(strong.LastReboundSpeed,Is.GreaterThan(1.5f));
            Assert.That(strong.PendingCollisionPoints,Is.EqualTo(30));
            strong.Retire();
            game.Launch(new Vector3(20.4f,5,-3),Vector3.right);
            var gentle=game.Projectiles.First(p=>p.InFlight);
            yield return WaitUntil(()=>gentle.CollisionCount>0,1);
            Assert.That(gentle.LastReboundSpeed,Is.LessThanOrEqualTo(1.5f));
            Assert.That(gentle.PendingCollisionPoints,Is.Zero);
            DropExistingShotThroughGate(gentle);
            yield return WaitUntil(()=>game.Hits==1);
            Assert.That(game.LastWasClean,Is.False,"Any physical contact still cancels clean shot.");
            Assert.That(game.Score,Is.EqualTo(100),"A gentle contact earns neither clean nor rebound bonus.");
        }

        [UnityTest]
        public IEnumerator Doorstep_PlayerThrowFollowsPitchAndYaw_WithoutLoft_AndScoresWithNewspapers()
        {
            StartStage("Doorstep");
            Assert.That(game.ActiveStage.data.horizontalThrow,Is.True);
            Assert.That(game.ActiveStage.data.projectilePrefab.name,Is.EqualTo("NewspaperStack"));
            Assert.That(game.thrower.heldItems[4].activeSelf,Is.True);
            foreach(float pitch in new[]{-20f,6f,28f})
            {
                game.thrower.view.transform.localRotation=Quaternion.Euler(pitch,8,0);
                Assert.That(Vector3.Dot(game.thrower.LaunchVelocity.normalized,game.thrower.view.transform.forward),Is.GreaterThan(.99999f));
                Assert.That(game.thrower.LaunchVelocity.y,Is.EqualTo(-Mathf.Sin(pitch*Mathf.Deg2Rad)*34).Within(.001f));
                Assert.That(game.thrower.LaunchVelocity.magnitude,Is.EqualTo(34).Within(.001f));
            }
            game.StartRound();
            AimAtGate();
            Assert.That(game.thrower.TryThrow(),Is.True);
            yield return WaitUntil(()=>game.Hits==1);
            Assert.That(game.LastAward,Is.EqualTo(350));
            Assert.That(game.LastBonus,Is.EqualTo(150));
            yield return WaitUntil(()=>game.CanThrow);
            Assert.That(game.ActiveStage.data.name,Is.EqualTo("Corner"),"The fifth stage must wrap to the first.");
        }

        [UnityTest]
        public IEnumerator Mailbox_RejectsReverseAndOutsideEntry_AndAcceptsFrontEntry()
        {
            StartStage("Doorstep");
            Vector3 center=game.ActiveStage.gate.transform.position;
            Assert.That(game.ActiveStage.movingTarget.localPosition.z,Is.EqualTo(3.4f));
            Assert.That(Mathf.Abs(game.ActiveStage.movingTarget.localPosition.x),Is.InRange(2.25f,2.75f));
            Assert.That(center.y,Is.EqualTo(1.265f).Within(.001f));
            Assert.That(game.ActiveStage.gate.openingHalfExtents,Is.EqualTo(new Vector2(.41f,.145f)));
            game.Launch(new Vector3(4,4,-4),Vector3.zero);
            var paper=game.Projectiles.First(p=>p.InFlight);
            Vector3 normal=game.ActiveStage.gate.transform.TransformDirection(game.ActiveStage.gate.entryNormal);
            Assert.That(game.ActiveStage.gate.FitsOpening(Vector3.zero,paper,normal),Is.True);
            Assert.That(game.ActiveStage.gate.FitsOpening(Vector3.right*.2f,paper,normal),Is.False,"A paper edge outside the slot must be rejected.");
            Assert.That(game.ActiveStage.gate.FitsOpening(Vector3.up*.09f,paper,normal),Is.False,"Use the actual stack height for the narrow slot.");
            paper.Retire();
            game.Launch(center+Vector3.forward*.3f,Vector3.back*8);
            yield return new WaitForSeconds(.12f);
            Assert.That(game.Hits,Is.Zero);
            foreach(var shot in game.Projectiles)shot.Retire();
            game.Launch(center+Vector3.right*1.05f+Vector3.back*.5f,Vector3.forward*8);
            yield return new WaitForSeconds(.15f);
            Assert.That(game.Hits,Is.Zero);
            foreach(var shot in game.Projectiles)shot.Retire();
            game.Launch(center+Vector3.back*.6f,Vector3.forward*10);
            yield return WaitUntil(()=>game.Hits==1);
            Assert.That(game.LastAward,Is.EqualTo(350));
        }

        [UnityTest]
        public IEnumerator StreetCrowd_WalksAndAnimates_RestartsAndStopsWhenInactive()
        {
            StartStage("Street");
            var crowd=game.ActiveStage.GetComponentsInChildren<Pedestrian>();
            Assert.That(crowd.Length,Is.EqualTo(10));
            var homes=crowd.Select(p=>p.transform.localPosition).ToArray();
            Quaternion arm=crowd[0].leftArm.localRotation;
            yield return new WaitForSeconds(.2f);
            for(int i=0;i<crowd.Length;i++)
            {
                Assert.That(Mathf.Sign(crowd[i].transform.localPosition.x-homes[i].x),Is.EqualTo(Mathf.Sign(crowd[i].direction)));
                Assert.That(crowd[i].GetComponent<Rigidbody>().isKinematic,Is.True);
            }
            Assert.That(Quaternion.Angle(arm,crowd[0].leftArm.localRotation),Is.GreaterThan(5));
            game.StartRound();
            for(int i=0;i<crowd.Length;i++)Assert.That(Vector3.Distance(crowd[i].transform.localPosition,homes[i]),Is.LessThan(.001f));
            StartStage("Corner");
            yield return new WaitForSeconds(.15f);
            for(int i=0;i<crowd.Length;i++)Assert.That(Vector3.Distance(crowd[i].transform.localPosition,homes[i]),Is.LessThan(.001f));
        }

        [UnityTest]
        public IEnumerator SceneProps_MatchEachStage_AndStreetUsesCrowdInstead()
        {
            foreach(var stage in game.stages)
            {
                StartStage(stage.data.name);
                yield return new WaitForFixedUpdate();
                Assert.That(stage.transform.Find("Rebound Obstacles"),Is.Null);
                Assert.That(stage.transform.Find("Extra Physics Props"),Is.Null);
                var group=stage.transform.Find("Scene Props");
                if(stage.data.name=="Street")
                {
                    Assert.That(group,Is.Null);
                    Assert.That(stage.GetComponentsInChildren<Pedestrian>().All(p=>p.transform.localPosition.z<0&&p.transform.localPosition.z> -5),Is.True);
                    continue;
                }
                Assert.That(group,Is.Not.Null);
                string expected=stage.data.name=="Corner"?"Bookcase":stage.data.name=="Basketball"?"Basketball rack":stage.data.name=="Carnival"?"Prize booth":"Shoe rack";
                Assert.That(group.Find(expected),Is.Not.Null);
                var obstacles=group.GetComponentsInChildren<Collider>(true);
                Assert.That(obstacles.Length,Is.GreaterThanOrEqualTo(10));
                Assert.That(obstacles.Max(c=>c.bounds.size.y)-obstacles.Min(c=>c.bounds.size.y),Is.GreaterThan(1.5f));
                Assert.That(stage.data.cleanShotBonus,Is.EqualTo(150));
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator SidewalkCrowd_CrossesThrowingLane_AndPhysicallyBlocksTrashBag()
        {
            StartStage("Street");
            var pedestrian=game.ActiveStage.GetComponentsInChildren<Pedestrian>().First(p=>Mathf.Abs(p.transform.localPosition.x)<.1f);
            Vector3 point=pedestrian.transform.TransformPoint(new Vector3(0,1.1f,0));
            Assert.That(point.z,Is.InRange(-5,0));
            Assert.That(point.z,Is.LessThan(game.ActiveStage.gate.transform.position.z));
            Assert.That(game.Launch(point+Vector3.back*.8f,Vector3.forward*8),Is.True);
            var shot=game.Projectiles.First(p=>p.InFlight);
            yield return WaitUntil(()=>shot.CollisionCount>0,1);
            Assert.That(shot.PropCollisionCount,Is.GreaterThan(0));
            Assert.That(game.Hits,Is.Zero);
            Assert.That(game.Score,Is.EqualTo(-10));
            Assert.That(shot.PendingCollisionPoints,Is.Zero,"A pedestrian contact must never earn rebound points.");
            Assert.That(game.hud.collisionFeedback.labels.Any(l=>l.text=="-10"),Is.True);
        }

        [UnityTest]
        public IEnumerator PedestrianPenalty_RepeatedEpisodesDeductImmediately_AndMissesDoNotRefund()
        {
            StartStage("Street");
            var pedestrian=game.ActiveStage.GetComponentsInChildren<Pedestrian>().First(p=>Mathf.Abs(p.transform.localPosition.x)<.1f);
            Vector3 point=pedestrian.transform.TransformPoint(new Vector3(0,1.1f,0));
            game.Launch(point+Vector3.back*.8f,Vector3.forward*8);
            var shot=game.Projectiles.First(p=>p.InFlight);
            yield return WaitUntil(()=>game.Score<0,1);
            Assert.That(game.Score,Is.EqualTo(-10));
            shot.Body.linearVelocity=Vector3.zero;
            yield return new WaitForSeconds(.06f);
            Assert.That(game.Score,Is.EqualTo(-10),"Continuing contact must not deduct every physics frame.");
            shot.Body.position=point+Vector3.back*2;shot.Body.linearVelocity=Vector3.zero;
            yield return new WaitForSeconds(.06f);
            point=pedestrian.transform.TransformPoint(new Vector3(0,1.1f,0));
            shot.Body.position=point+Vector3.back*.8f;shot.Body.linearVelocity=Vector3.forward*8;
            yield return WaitUntil(()=>game.Score== -20,1);
            Assert.That(shot.PendingCollisionPoints,Is.Zero);
            shot.Retire();
            Assert.That(game.Score,Is.EqualTo(-20));
            game.RegisterPedestrianHit(shot,pedestrian,point);
            Assert.That(game.Score,Is.EqualTo(-20),"Retired projectiles cannot deduct again.");
            StartStage("Corner");
            game.Launch(new Vector3(20,5,-3),Vector3.up);
            game.RegisterPedestrianHit(game.Projectiles.First(p=>p.InFlight),pedestrian,point);
            Assert.That(game.Score,Is.Zero,"Other stages must not receive street penalties.");
        }

        [UnityTest]
        public IEnumerator Street_LidReboundMeetsLowerThreshold_AndBanksPointsOnlyOnGoal()
        {
            StartStage("Street");
            Transform lid=game.ActiveStage.movingTarget.Find("Open Lid");
            Vector3 outward=-lid.up;
            Vector3 start=lid.TransformPoint(new Vector3(0,-.065f,-.4f))+outward*(game.ActiveStage.data.projectileRadius+.08f);
            game.Launch(start,-outward*4);
            var shot=game.Projectiles.First(p=>p.InFlight);
            yield return WaitUntil(()=>shot.ReboundCount>0,1);
            Assert.That(shot.LastReboundSpeed,Is.GreaterThan(.1f));
            Assert.That(shot.PendingCollisionPoints,Is.GreaterThanOrEqualTo(30));
            Assert.That(game.Score,Is.Zero);
            DropExistingShotThroughGate(shot);
            yield return WaitUntil(()=>game.Hits==1);
            Assert.That(game.Score,Is.EqualTo(200+shot.ReboundCount*30));
        }

        [UnityTest]
        public IEnumerator SoftBag_CollapsesOnLid_LimitsRebound_AndRestoresOnReuse()
        {
            StartStage("Street");
            Transform lid=game.ActiveStage.movingTarget.Find("Open Lid");
            Vector3 outward=-lid.up;
            Vector3 start=lid.TransformPoint(new Vector3(0,-.065f,-.4f))+outward*(game.ActiveStage.data.projectileRadius+.08f);
            var source=game.ActiveStage.data.projectilePrefab.GetComponentInChildren<MeshFilter>().sharedMesh;
            Vector3[] sourceVertices=source.vertices;
            game.Launch(start,-outward*4);
            var shot=game.Projectiles.First(p=>p.InFlight);
            var soft=shot.GetComponent<SoftBagDeformation>();
            Assert.That(soft,Is.Not.Null);
            var mesh=shot.GetComponentInChildren<MeshFilter>().sharedMesh;
            Assert.That(mesh,Is.Not.SameAs(source),"Deform only the pooled instance, not the shared mesh asset.");
            yield return WaitUntil(()=>shot.CollisionCount>0,1);
            Assert.That(shot.LastReboundSpeed,Is.LessThanOrEqualTo(.221f));
            yield return new WaitForSeconds(.06f);
            Assert.That(soft.Compression,Is.GreaterThan(.2f));
            Assert.That(shot.GetComponent<SphereCollider>().radius,Is.LessThan(.28f));
            Assert.That(mesh.vertices.Zip(sourceVertices,(a,b)=>Vector3.Distance(a,b)).Max(),Is.GreaterThan(.02f));
            Assert.That(source.vertices,Is.EqualTo(sourceVertices));
            shot.Retire();
            Assert.That(soft.Compression,Is.Zero);
            Assert.That(shot.GetComponent<SphereCollider>().radius,Is.EqualTo(.28f).Within(.0001f));
            Assert.That(mesh.vertices,Is.EqualTo(sourceVertices));
            game.Launch(new Vector3(4,4,-4),Vector3.up);
            Assert.That(game.Projectiles.First(p=>p.InFlight),Is.SameAs(shot));
            Assert.That(soft.Compression,Is.Zero);
        }

        [UnityTest]
        public IEnumerator Carnival_ThrowsPlasticBottle_WithSpinPerpendicularToBottleAxis()
        {
            StartStage("Carnival");
            Assert.That(game.ActiveStage.data.projectilePrefab.name,Is.EqualTo("PlasticBottle"));
            Assert.That(game.thrower.heldItems[2].name,Is.EqualTo("Held Bottle"));
            Assert.That(game.thrower.heldItems[2].activeSelf,Is.True);
            game.Launch(new Vector3(4,4,-4),Vector3.forward*6);
            var shot=game.Projectiles.First(p=>p.InFlight);
            Assert.That(shot.GetComponent<CapsuleCollider>(),Is.Not.Null);
            Vector3 bottleAxis=shot.Body.rotation*Vector3.up;
            Assert.That(Mathf.Abs(Vector3.Dot(bottleAxis,shot.Body.angularVelocity.normalized)),Is.LessThan(.001f));
            Assert.That(shot.Body.angularVelocity.magnitude,Is.EqualTo(14).Within(.01f));
            Quaternion rotation=shot.Body.rotation;
            yield return new WaitForSeconds(.15f);
            Assert.That(shot.CollisionCount,Is.Zero);
            Assert.That(Quaternion.Angle(rotation,shot.Body.rotation),Is.GreaterThan(60));
        }

        [UnityTest]
        public IEnumerator ImportantTargets_VarySideways_KeepDepthAndAttachedBankAngles()
        {
            foreach(string name in new[]{"Street","Basketball","Doorstep"})
            {
                StartStage(name);
                var stage=game.ActiveStage;
                var poses=stage.movingTarget.GetComponentsInChildren<Collider>(true).Select(c=>new {collider=c,position=stage.movingTarget.InverseTransformPoint(c.transform.position),rotation=Quaternion.Inverse(stage.movingTarget.rotation)*c.transform.rotation}).ToArray();
                float previous=stage.CurrentTargetCenter.x,depth=stage.CurrentTargetCenter.z;
                for(int i=0;i<20;i++)
                {
                    stage.ResetStage();
                    Assert.That(Mathf.Abs(stage.CurrentTargetCenter.x-previous),Is.GreaterThanOrEqualTo(stage.data.minimumHorizontalChange-.0001f));
                    Assert.That(stage.CurrentTargetCenter.z,Is.EqualTo(depth).Within(.0001f));
                    foreach(var pose in poses)
                    {
                        Assert.That(Vector3.Distance(stage.movingTarget.InverseTransformPoint(pose.collider.transform.position),pose.position),Is.LessThan(.0001f));
                        Assert.That(Quaternion.Angle(Quaternion.Inverse(stage.movingTarget.rotation)*pose.collider.transform.rotation,pose.rotation),Is.LessThan(.01f));
                    }
                    previous=stage.CurrentTargetCenter.x;
                }
                if(name=="Doorstep")
                {
                    Assert.That(Mathf.Abs(stage.CurrentTargetCenter.x),Is.InRange(2.25f,2.75f));
                    Assert.That(stage.CurrentWind,Is.EqualTo(Vector3.zero));
                }
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator VariationBounds_KeepLidAndBackboardShots_AndMailboxPlayerThrowsReachable()
        {
            foreach(string name in new[]{"Street","Basketball","Doorstep"})
            {
                var data=Stage(name).data;bool random=data.randomize;var range=data.horizontalOffsetRange;var depths=data.depthOffsetRange;var winds=data.windSpeedRange;
                try
                {
                    data.randomize=true;data.depthOffsetRange=Vector2.zero;data.windSpeedRange=Vector2.zero;
                    foreach(float offset in new[]{range.x,range.y})
                    {
                        data.horizontalOffsetRange=new Vector2(offset,offset);
                        StartStage(name);
                        if(name=="Street")
                        {
                            Transform lid=game.ActiveStage.movingTarget.Find("Open Lid");
                            Vector3 start=lid.TransformPoint(new Vector3(0,-.065f,-.4f))-lid.up*(data.projectileRadius+.03f);
                            Assert.That(game.Launch(start,Vector3.down*.25f),Is.True);
                        }
                        else if(name=="Basketball")
                        {
                            Vector3 center=game.ActiveStage.gate.transform.position;
                            Assert.That(game.Launch(center+new Vector3(0,.35f,.4f),new Vector3(0,-.3f,5)),Is.True);
                        }
                        else {AimAtGate();Assert.That(game.thrower.TryThrow(),Is.True);}
                        var shot=game.Projectiles.First(p=>p.InFlight);
                        yield return WaitUntil(()=>game.Hits==1,3,name+" boundary offset="+offset);
                        if(name!="Doorstep")
                        {
                            Assert.That(shot.CollisionCount,Is.GreaterThan(0),"Expected a real lid/backboard contact.");
                            Assert.That(game.LastWasClean,Is.False);
                        }
                    }
                }
                finally{data.randomize=random;data.horizontalOffsetRange=range;data.depthOffsetRange=depths;data.windSpeedRange=winds;}
            }
        }

        IEnumerator CompleteStageThroughGate()
        {
            int hits=game.Hits+1;
            var gate=game.ActiveStage.gate;
            Vector3 normal=gate.transform.TransformDirection(gate.entryNormal).normalized;
            Assert.That(game.Launch(gate.transform.position+normal*.5f,-normal*8),Is.True);
            yield return WaitUntil(()=>game.Hits==hits&&game.CanThrow,3);
        }

        [UnityTest]
        public IEnumerator Carnival_FirstClearSpeedsBothAxesOnce_AndPlayerPredictionStillScores()
        {
            var data=Stage("Carnival").data;bool random=data.randomize;
            try
            {
                data.randomize=false;game.repeatStartingStage=true;StartStage("Carnival");
                var stage=game.ActiveStage;
                Assert.That(stage.CurrentMotionSpeedMultiplier,Is.EqualTo(1));
                float start=stage.movingTarget.localPosition.x;
                for(int i=0;i<10;i++)yield return new WaitForFixedUpdate();
                float originalTravel=Mathf.Abs(stage.movingTarget.localPosition.x-start);
                yield return CompleteStageThroughGate();
                Assert.That(stage.CurrentMotionSpeedMultiplier,Is.EqualTo(1.25f));
                Assert.That(stage.CurrentVerticalAmplitude,Is.GreaterThan(0));
                start=stage.movingTarget.localPosition.x;
                for(int i=0;i<10;i++)yield return new WaitForFixedUpdate();
                float fasterTravel=Mathf.Abs(stage.movingTarget.localPosition.x-start);
                Assert.That(fasterTravel,Is.GreaterThan(originalTravel*1.15f));
                Assert.That(fasterTravel,Is.LessThan(originalTravel*1.4f));
                yield return CompleteStageThroughGate();
                Assert.That(stage.CurrentMotionSpeedMultiplier,Is.EqualTo(1.25f),"Further clears must not compound the speed.");
                Vector3 predicted=stage.PredictedGatePosition(.3f);
                yield return new WaitForSeconds(.3f);
                Assert.That(Vector3.Distance(stage.gate.transform.position,predicted),Is.LessThan(.08f));
                AimAtGate();Assert.That(game.thrower.TryThrow(),Is.True);
                yield return WaitUntil(()=>game.Hits==3&&game.CanThrow,3,"Actual player throw at the faster two-axis target.");
                game.StartRound();
                Assert.That(stage.CurrentMotionSpeedMultiplier,Is.EqualTo(1));
                Assert.That(stage.CurrentVerticalAmplitude,Is.Zero);
            }
            finally{data.randomize=random;}
        }

        [UnityTest]
        public IEnumerator WindLines_FollowDirectionAndStrength_KeepAimClear_AndHideWithoutWindOrDuringMenus()
        {
            var effect=game.thrower.view.GetComponentInChildren<WindStreaks>(true);
            Assert.That(effect,Is.Not.Null);
            Assert.That(effect.streaks.Length,Is.EqualTo(12));
            Assert.That(effect.streaks.All(l=>!l.enabled),Is.True,"No wind lines on the opening menu.");
            var data=Stage("Basketball").data;bool random=data.randomize;Vector3 wind=data.wind;float influence=data.windInfluence;
            try
            {
                data.randomize=false;data.wind=Vector3.right*2;data.windInfluence=1;
                StartStage("Basketball");game.thrower.SetCursor(false);
                yield return null;yield return null;
                var visible=effect.streaks.Where(l=>l.enabled).ToArray();
                Assert.That(visible.Length,Is.GreaterThan(0));
                float weakWidth=visible[0].widthMultiplier;int weakCount=visible.Length;
                foreach(var line in visible)
                {
                    Vector3 a=game.thrower.view.WorldToViewportPoint(line.transform.TransformPoint(line.GetPosition(0)));
                    Vector3 b=game.thrower.view.WorldToViewportPoint(line.transform.TransformPoint(line.GetPosition(line.positionCount-1)));
                    Assert.That(b.x,Is.GreaterThan(a.x));
                    Assert.That(line.startColor.b,Is.GreaterThan(line.startColor.g));
                    Assert.That(line.startColor.g,Is.GreaterThan(line.startColor.r));
                    for(int point=0;point<line.positionCount;point++)
                    {
                        Vector3 p=game.thrower.view.WorldToViewportPoint(line.transform.TransformPoint(line.GetPosition(point)));
                        Assert.That(p.x>.341f&&p.x<.659f&&p.y>.321f&&p.y<.719f,Is.False,"Wind must leave the aiming area clear.");
                    }
                }
                Vector3 previous=visible[0].GetPosition(2);
                yield return new WaitForSeconds(.15f);
                Assert.That(Vector3.Distance(visible[0].GetPosition(2),previous),Is.GreaterThan(.005f),"Wind lines must animate.");
                game.thrower.view.transform.localRotation=Quaternion.Euler(game.thrower.restingPitch,30,0);
                yield return null;yield return null;
                var cameraView=game.thrower.view;
                Vector2 expectedDirection=new Vector2(Vector3.Dot(data.wind,cameraView.transform.right),Vector3.Dot(data.wind,cameraView.transform.forward));
                foreach(var line in effect.streaks.Where(l=>l.enabled))
                {
                    Vector3 a=cameraView.WorldToViewportPoint(line.transform.TransformPoint(line.GetPosition(0)));
                    Vector3 b=cameraView.WorldToViewportPoint(line.transform.TransformPoint(line.GetPosition(line.positionCount-1)));
                    Vector2 pixelDirection=new Vector2((b.x-a.x)*cameraView.pixelWidth,(b.y-a.y)*cameraView.pixelHeight);
                    Assert.That(Vector2.Angle(pixelDirection,expectedDirection),Is.LessThan(1),"Wind lines must match direction while looking sideways.");
                }
                cameraView.transform.localRotation=Quaternion.Euler(game.thrower.restingPitch,0,0);
                data.wind=Vector3.left*2;game.ActiveStage.ResetStage();yield return null;yield return null;
                foreach(var line in effect.streaks.Where(l=>l.enabled))
                {
                    Vector3 a=game.thrower.view.WorldToViewportPoint(line.transform.TransformPoint(line.GetPosition(0)));
                    Vector3 b=game.thrower.view.WorldToViewportPoint(line.transform.TransformPoint(line.GetPosition(line.positionCount-1)));
                    Assert.That(b.x,Is.LessThan(a.x));
                }
                data.wind=Vector3.right*7;game.ActiveStage.ResetStage();yield return null;yield return null;
                visible=effect.streaks.Where(l=>l.enabled).ToArray();
                Assert.That(visible.Length,Is.GreaterThanOrEqualTo(weakCount));
                Assert.That(visible[0].widthMultiplier,Is.GreaterThan(weakWidth));
                data.wind=Vector3.zero;game.ActiveStage.ResetStage();yield return null;yield return null;
                Assert.That(effect.streaks.All(l=>!l.enabled),Is.True,"Calm stages must not show wind.");
                data.wind=Vector3.right*7;data.windInfluence=0;game.ActiveStage.ResetStage();yield return null;yield return null;
                Assert.That(effect.streaks.All(l=>!l.enabled),Is.True,"No visual wind when wind has no gameplay influence.");
                data.windInfluence=1;game.ActiveStage.ResetStage();yield return null;yield return null;
                game.hud.ShowMenu(game.Best);yield return null;yield return null;
                Assert.That(effect.streaks.All(l=>!l.enabled),Is.True);
                game.hud.ShowPlay();game.transitionDuration=1;
                var gate=game.ActiveStage.gate;Vector3 normal=gate.transform.TransformDirection(gate.entryNormal);
                game.Launch(gate.transform.position+normal*.5f,-normal*8);
                yield return WaitUntil(()=>game.IsTransitioning,1);yield return null;yield return null;
                Assert.That(effect.streaks.All(l=>!l.enabled),Is.True,"Transition must clear old-stage wind lines.");
                Assert.That(effect.GetComponentsInChildren<Collider>().Length,Is.Zero);
                Assert.That(effect.GetComponentsInChildren<LineRenderer>().Length,Is.EqualTo(12),"No per-frame object creation.");
            }
            finally{data.randomize=random;data.wind=wind;data.windInfluence=influence;}
        }

        [UnityTest]
        public IEnumerator ThemeAtmospheres_FollowScoringTransitions_AndRestoreTheFirstStageOnWrap()
        {
            StartStage("Corner");
            for(int visit=0;visit<6;visit++)
            {
                var active=game.ActiveStage;
                var atmosphere=active.GetComponent<StageAtmosphere>();
                Assert.That(atmosphere,Is.Not.Null,active.name);
                Assert.That(RenderSettings.skybox,Is.SameAs(atmosphere.sky),active.name+" sky leaked from the previous stage.");
                Assert.That(RenderSettings.sun,Is.SameAs(atmosphere.sunlight),active.name+" sunlight was restored over the new stage.");
                Assert.That(atmosphere.sunlight.isActiveAndEnabled,Is.True);
                Assert.That(game.thrower.view.clearFlags,Is.EqualTo(CameraClearFlags.Skybox));
                foreach(var stage in game.stages.Where(s=>s!=active))
                    Assert.That(stage.GetComponent<StageAtmosphere>().sunlight.isActiveAndEnabled,Is.False,stage.name+" sunlight remained active.");
                if(visit<5)yield return CompleteStageThroughGate();
            }
            Assert.That(game.ActiveStage,Is.SameAs(Stage("Corner")));
            Assert.That(game.Hits,Is.EqualTo(5));
            game.StartRound();
            Assert.That(RenderSettings.skybox,Is.SameAs(game.ActiveStage.GetComponent<StageAtmosphere>().sky));
        }

        [UnityTest]
        public IEnumerator Mailbox_SlantedLipEntries_ScoreAfterRotatingInside_FromBothDirections()
        {
            foreach(float vx in new[]{-9f,9f,-16f,16f})
            foreach(float height in new[]{-.08f,-.06f})
            {
                StartStage("Doorstep");
                Vector3 center=game.ActiveStage.gate.transform.position;
                const float vz=30,vy=-3,time=.6f/vz;
                Vector3 start=center+new Vector3(-vx*time,height-vy*time+.5f*9.81f*time*time,-.6f);
                Assert.That(game.Launch(start,new Vector3(vx,vy,vz)),Is.True);
                yield return WaitUntil(()=>game.Hits==1,1,"Slanted entry vx="+vx+", height="+height);
                Assert.That(game.LastAward,Is.GreaterThanOrEqualTo(200));
                Assert.That(game.LastBonus,Is.Zero,"A lip contact must still cancel the clean-shot bonus.");
                yield return new WaitForSeconds(.08f);
                Assert.That(game.Hits,Is.EqualTo(1));
            }
        }

        [UnityTest]
        public IEnumerator Mailbox_PlayerThrowsAtSlantedAngles_ScoreOnBothSidesWithSmallPitchChanges()
        {
            var data=Stage("Doorstep").data;var range=data.horizontalOffsetRange;
            try
            {
                foreach(float side in new[]{-2.5f,2.5f})
                foreach(float pitchChange in new[]{-.25f,.25f})
                {
                    data.horizontalOffsetRange=new Vector2(side,side);
                    StartStage("Doorstep");AimAtGate();
                    Vector3 angles=game.thrower.view.transform.localEulerAngles;
                    angles.x=Mathf.DeltaAngle(0,angles.x)+pitchChange;
                    game.thrower.view.transform.localRotation=Quaternion.Euler(angles);
                    Assert.That(Mathf.Abs(game.thrower.LaunchVelocity.x),Is.GreaterThan(3));
                    Assert.That(game.thrower.TryThrow(),Is.True);
                    yield return WaitUntil(()=>game.Hits==1,2,"Player slanted throw side="+side+", pitch change="+pitchChange);
                }
            }
            finally{data.horizontalOffsetRange=range;}
        }

        [UnityTest]
        public IEnumerator Mailbox_SideMisses_ReverseEntry_AndReusedPartialEntry_DoNotScore()
        {
            foreach(float side in new[]{-.7f,.7f})
            {
                StartStage("Doorstep");
                Vector3 center=game.ActiveStage.gate.transform.position;
                game.Launch(center+new Vector3(side,.02f,-.6f),Vector3.forward*30);
                yield return new WaitForSeconds(.3f);
                Assert.That(game.Hits,Is.Zero,"Outside the side jamb is not an entry.");
            }
            StartStage("Doorstep");
            game.Launch(game.ActiveStage.gate.transform.position+Vector3.forward*.3f,Vector3.back*10);
            yield return new WaitForSeconds(.15f);
            Assert.That(game.Hits,Is.Zero,"Reverse entry must not score.");

            StartStage("Doorstep");
            Vector3 slot=game.ActiveStage.gate.transform.position;
            game.Launch(slot+Vector3.back*.05f,Vector3.back);
            var shot=game.Projectiles.First(p=>p.InFlight);
            yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
            Assert.That(game.Hits,Is.Zero,"Only poking the leading edge in does not score.");
            shot.Retire();
            game.Launch(slot+Vector3.forward*.3f,Vector3.zero);
            Assert.That(shot.InFlight,Is.True,"Exercise the same pooled projectile.");
            yield return new WaitForSeconds(.3f);
            Assert.That(game.Hits,Is.Zero,"Entry history must not carry into a reused shot born inside.");
        }

        [UnityTest]
        public IEnumerator ShelfBooks_RemainStableAtStart_StillKnockOff_AndRestartCorrectly()
        {
            StartStage("Corner");
            var books=game.ActiveStage.GetComponentsInChildren<PhysicsProp>().Where(p=>p.name.StartsWith("Book ")).ToArray();
            Assert.That(books.Length,Is.EqualTo(24));
            var homes=books.Select(b=>b.GetComponent<Rigidbody>().position).ToArray();
            var caseBody=books[0].transform.parent.GetComponent<Rigidbody>();
            var scenery=game.ActiveStage.GetComponentsInChildren<Collider>().Where(c=>!c.isTrigger&&c.enabled).ToArray();
            foreach(var frame in scenery.Where(c=>c.attachedRigidbody==caseBody))
            foreach(var other in scenery.Where(c=>c.attachedRigidbody!=caseBody&&!books.Any(b=>c.attachedRigidbody==b.GetComponent<Rigidbody>())))
                Assert.That(Physics.ComputePenetration(frame,frame.transform.position,frame.transform.rotation,other,other.transform.position,other.transform.rotation,out _,out float overlap)&&overlap>.001f,Is.False,"Bookcase overlaps "+other.name);
            foreach(var book in books)
            {
                var collider=book.GetComponent<Collider>();
                foreach(var shelf in book.transform.parent.GetComponentsInChildren<Collider>().Where(c=>c.name.StartsWith("Shelf ")))
                    Assert.That(Physics.ComputePenetration(collider,collider.transform.position,collider.transform.rotation,shelf,shelf.transform.position,shelf.transform.rotation,out _,out float depth)&&depth>.001f,Is.False,book.name+" starts embedded in "+shelf.name);
            }
            for(int visit=0;visit<3;visit++)
            {
                game.StartRound();
                yield return new WaitForSeconds(2);
                for(int i=0;i<books.Length;i++)
                    Assert.That(Vector3.Distance(books[i].GetComponent<Rigidbody>().position,homes[i]),Is.LessThan(.04f),books[i].name+" scattered without a throw on visit "+visit);
            }
            var target=books.Single(b=>b.name=="Book 1 2");
            Vector3 start=target.GetComponent<Rigidbody>().position;
            Assert.That(game.Launch(start+Vector3.back*.6f,Vector3.forward*12),Is.True);
            yield return WaitUntil(()=>Vector3.Distance(target.GetComponent<Rigidbody>().position,start)>.05f,2,"Books must remain knockable.");
            game.StartRound();
            for(int i=0;i<books.Length;i++)
                Assert.That(Vector3.Distance(books[i].GetComponent<Rigidbody>().position,homes[i]),Is.LessThan(.001f));
        }

        [UnityTest]
        public IEnumerator Mailbox_LowerLipAndSideEntries_ScoreOnce_AndShotsBornInsideDoNotScore()
        {
            foreach(var offset in new[]{new Vector2(0,-.06f),new Vector2(-.145f,-.06f),new Vector2(.145f,-.06f),new Vector2(0,.055f)})
            {
                StartStage("Doorstep");
                var center=game.ActiveStage.gate.transform.position;
                Assert.That(game.Launch(center+Vector3.back*.6f+new Vector3(offset.x,offset.y,0),Vector3.forward*34),Is.True);
                yield return WaitUntil(()=>game.Hits==1,1,"Physical mailbox entry at "+offset);
                Assert.That(game.LastAward,Is.GreaterThanOrEqualTo(200));
                yield return new WaitForSeconds(.15f);
                Assert.That(game.Hits,Is.EqualTo(1));
            }
            StartStage("Doorstep");
            Assert.That(game.Launch(game.ActiveStage.gate.transform.position+Vector3.forward*.3f,Vector3.zero),Is.True);
            yield return new WaitForSeconds(.4f);
            Assert.That(game.Hits,Is.Zero,"Already being inside is not a valid front entry.");
        }

        [UnityTest]
        public IEnumerator Carnival_PrizesAwardImmediatelyOnce_WithoutGoalOrReboundBonus()
        {
            StartStage("Carnival");
            var prizes=game.ActiveStage.GetComponentsInChildren<PhysicsProp>().Where(p=>p.knockdownPoints>0).ToArray();
            Assert.That(prizes.Length,Is.EqualTo(9));
            yield return new WaitForSeconds(.4f);
            Assert.That(game.Score,Is.Zero,"Unhit prizes must not award points while settling.");
            var tin=prizes.Last(p=>p.name.StartsWith("Stacked tin "));
            var collider=tin.GetComponent<Collider>();
            Vector3 center=collider.bounds.center;
            Assert.That(game.Launch(center+Vector3.back*.85f,Vector3.forward*14),Is.True);
            var shot=game.Projectiles.First(p=>p.InFlight);
            yield return WaitUntil(()=>shot.CollisionCount>0,1);
            Assert.That(shot.PendingCollisionPoints,Is.Zero,"Prize contact must not bank +30.");
            shot.Retire();
            yield return WaitUntil(()=>prizes.Any(p=>p.KnockedDown),2);
            yield return new WaitForSeconds(.5f);
            Assert.That(game.Hits,Is.Zero);
            Assert.That(game.Score,Is.EqualTo(prizes.Count(p=>p.KnockedDown)*10));
            int score=game.Score;
            yield return new WaitForSeconds(.4f);
            Assert.That(game.Score,Is.EqualTo(prizes.Count(p=>p.KnockedDown)*10));
            Assert.That(game.Score,Is.GreaterThanOrEqualTo(score));
            game.StartRound();
            Assert.That(prizes.All(p=>!p.KnockedDown),Is.True);
            Assert.That(game.Score,Is.Zero);
        }

        [UnityTest]
        public IEnumerator HouseWindows_DetachOnNewspaperImpact_AndRestoreOnRestart()
        {
            StartStage("Doorstep");
            var windows=game.ActiveStage.GetComponentsInChildren<PhysicsProp>().Where(p=>p.releaseOnImpact).ToArray();
            Assert.That(windows.Length,Is.EqualTo(2));
            foreach(var window in windows)
            {
                var body=window.GetComponent<Rigidbody>();
                Vector3 home=body.position;
                Assert.That(body.isKinematic,Is.True);
                game.Launch(home+Vector3.back*.8f,Vector3.forward*12);
                yield return WaitUntil(()=>!body.isKinematic,1,"Newspaper should detach the window.");
                yield return new WaitForSeconds(.3f);
                Assert.That(body.position.y,Is.LessThan(home.y-.1f));
                foreach(var shot in game.Projectiles)shot.Retire();
                game.StartRound();
                Assert.That(body.isKinematic,Is.True);
                Assert.That(Vector3.Distance(body.position,home),Is.LessThan(.001f));
            }
        }

        [UnityTest]
        public IEnumerator Mailbox_UsesBothSides_LeavesDoorwayClear_AndPlayerCanScoreEachSide()
        {
            StartStage("Doorstep");
            var stage=game.ActiveStage;
            var sides=new HashSet<int>();
            for(int i=0;i<40;i++)
            {
                stage.ResetStage();
                float x=stage.CurrentTargetCenter.x;
                Assert.That(Mathf.Abs(x),Is.InRange(2.25f,2.75f));
                sides.Add(x<0?-1:1);
            }
            Assert.That(sides.Count,Is.EqualTo(2));
            var data=stage.data;var range=data.horizontalOffsetRange;
            try
            {
                foreach(float x in new[]{-2.75f,-2.25f,2.25f,2.75f})
                {
                    data.horizontalOffsetRange=new Vector2(x,x);
                    game.StartRound();AimAtGate();
                    Assert.That(game.thrower.TryThrow(),Is.True);
                    yield return WaitUntil(()=>game.Hits==1,2,"Mailbox side "+x);
                }
            }
            finally{data.horizontalOffsetRange=range;}
        }

        [UnityTest]
        public IEnumerator SceneryHasKnockableFurnitureAndContents_AndStreetBinsHaveDistinctRealColours()
        {
            foreach(string name in new[]{"Corner","Basketball","Carnival","Doorstep"})
            {
                StartStage(name);
                var props=game.ActiveStage.transform.Find("Scene Props").GetComponentsInChildren<PhysicsProp>();
                Assert.That(props.Length,Is.GreaterThanOrEqualTo(8),name);
                Assert.That(props.Any(p=>p.GetComponent<Rigidbody>().mass<.2f),Is.True,name+" needs light loose contents.");
            }
            StartStage("Street");
            var materials=game.ActiveStage.movingTarget.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name=="Wheeled Bin Body").Select(r=>r.sharedMaterial.name).OrderBy(n=>n).ToArray();
            Assert.That(materials,Is.EqualTo(new[]{"BinBlue","BinCharcoal","BinGreen"}));
            yield return null;
        }

        [UnityTest]
        public IEnumerator FixedStagePlaytest_ReentersSameStage_UnlocksMotion_AndCanResumeRotation()
        {
            game.repeatStartingStage = true;
            StartStage("Carnival");
            var carnival = game.ActiveStage;
            int token = game.StageToken;
            Assert.That(carnival.CurrentVerticalAmplitude, Is.Zero);

            yield return CompleteStageThroughGate();
            Assert.That(game.ActiveStage, Is.SameAs(carnival));
            Assert.That(game.StageToken, Is.EqualTo(token + 1));
            Assert.That(carnival.ClearsThisRound, Is.EqualTo(1));
            Assert.That(carnival.CurrentVerticalAmplitude, Is.GreaterThan(0));
            yield return CompleteStageThroughGate();
            Assert.That(game.StageToken, Is.EqualTo(token + 2));
            Assert.That(game.Hits, Is.EqualTo(2));
            Assert.That(carnival.ClearsThisRound, Is.EqualTo(2));

            game.StartRound();
            Assert.That(game.ActiveStage, Is.SameAs(carnival));
            Assert.That(carnival.ClearsThisRound, Is.Zero);
            Assert.That(carnival.CurrentVerticalAmplitude, Is.Zero);
            game.repeatStartingStage = false;
            yield return CompleteStageThroughGate();
            Assert.That(game.ActiveStage, Is.SameAs(Stage("Doorstep")));
        }

        [UnityTest]
        public IEnumerator Carnival_UnlocksVerticalMotionOnNextVisitAfterClear_AndRestartResetsProgress()
        {
            game.startingStageIndex=0;game.StartRound();
            for(int i=0;i<3;i++)yield return CompleteStageThroughGate();
            Assert.That(game.ActiveStage.data.name,Is.EqualTo("Carnival"));
            var carnival=game.ActiveStage;
            Assert.That(game.Hits,Is.EqualTo(3));
            Assert.That(carnival.ClearsThisRound,Is.Zero,"Clearing other stages must not unlock this stage.");
            Assert.That(carnival.CurrentMotionSpeedMultiplier,Is.EqualTo(1));
            Assert.That(carnival.CurrentVerticalAmplitude,Is.Zero);
            float y=carnival.movingTarget.position.y;
            yield return new WaitForSeconds(.3f);
            Assert.That(carnival.movingTarget.position.y,Is.EqualTo(y).Within(.001f));
            for(int i=0;i<5;i++)yield return CompleteStageThroughGate();
            Assert.That(game.Hits,Is.EqualTo(8));
            Assert.That(game.ActiveStage,Is.SameAs(carnival));
            Assert.That(carnival.ClearsThisRound,Is.EqualTo(1));
            Assert.That(carnival.CurrentVerticalAmplitude,Is.EqualTo(.32f));
            Vector3 predicted=carnival.PredictedGatePosition(.3f);
            yield return new WaitForSeconds(.3f);
            Assert.That(Vector3.Distance(carnival.gate.transform.position,predicted),Is.LessThan(.08f));
            float lowest=float.MaxValue,highest=float.MinValue;
            double end=Time.timeAsDouble+1.3f;
            while(Time.timeAsDouble<end)
            {
                float current=carnival.movingTarget.localPosition.y;
                lowest=Mathf.Min(lowest,current);highest=Mathf.Max(highest,current);
                Assert.That(Mathf.Abs(current-carnival.CurrentTargetCenter.y),Is.LessThanOrEqualTo(.321f));
                yield return null;
            }
            Assert.That(highest-lowest,Is.GreaterThan(.03f));
            AimAtGate();
            Assert.That(game.thrower.TryThrow(),Is.True);
            yield return WaitUntil(()=>game.Hits==9&&game.CanThrow,3,"Player throw against two-axis carnival motion.");
            StartStage("Carnival");
            Assert.That(game.Hits,Is.Zero);
            Assert.That(carnival.ClearsThisRound,Is.Zero);
            Assert.That(carnival.CurrentVerticalAmplitude,Is.Zero);
        }

        [UnityTest]
        public IEnumerator WindArrow_FlipsDirection_AndCleanRaysStayHiddenOnMenu()
        {
            var effect=game.hud.cleanShotEffect;
            Assert.That(effect.gameObject.activeSelf,Is.False);
            Assert.That(effect.rays.gameObject.activeSelf,Is.False);
            StartStage("Corner");
            var wind=game.hud.windIndicator;
            wind.SetWind(Vector3.right*2,game.thrower.view.transform,Color.cyan);
            Vector3 right=wind.arrow.right;
            wind.SetWind(Vector3.left*2,game.thrower.view.transform,Color.cyan);
            Assert.That(Vector3.Dot(right,wind.arrow.right),Is.LessThan(-.99f));
            Assert.That(wind.coreImages.All(image=>!image.raycastTarget),Is.True);
            Assert.That(wind.glowImages.Length,Is.GreaterThan(0));
            Assert.That(game.hud.stageText.text,Does.Not.Contain("WIND"));
            effect.Play(50);yield return null;
            Assert.That(effect.rays.gameObject.activeSelf,Is.True);
            game.hud.ShowMenu(game.Best);
            Assert.That(effect.gameObject.activeSelf,Is.False);
            Assert.That(effect.rays.gameObject.activeSelf,Is.False);
        }
    }
}
#endif
