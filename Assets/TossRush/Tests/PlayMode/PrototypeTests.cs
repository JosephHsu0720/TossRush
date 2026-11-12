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
    public sealed class PrototypeTests
    {
        GameDirector game;
        int savedBest;
        bool hadBest;
        Random.State savedRandom;

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
        }

        [TearDown]
        public void TearDown()
        {
            if(hadBest)PlayerPrefs.SetInt("TossRush.Best.v1",savedBest);
            else PlayerPrefs.DeleteKey("TossRush.Best.v1");
            PlayerPrefs.Save();
            Random.state=savedRandom;
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        }

        static IEnumerator WaitUntil(System.Func<bool> condition,float seconds=3,string context="")
        {
            double end=Time.timeAsDouble+seconds;
            while(!condition()&&Time.timeAsDouble<end)yield return null;
            Assert.That(condition(),Is.True,"Expected state did not arrive within "+seconds+" seconds. "+context);
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
            for(float yaw=-24;yaw<=24;yaw+=2)
            for(float elevation=6;elevation<=68;elevation+=2)
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
            Assert.That(best,Is.LessThan(0.07f),"Randomized target must be reachable within the player's aiming limits.");
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
            Assert.That(game.Score,Is.EqualTo(150));
            Assert.That(game.Hits,Is.EqualTo(1));
            Assert.That(game.LastBonus,Is.EqualTo(50));
            Assert.That(game.LastWasClean,Is.True);
            Assert.That(game.hud.cleanShotEffect.label.text,Does.Contain("+50"));
            Assert.That(game.CanThrow,Is.False,"A transition blocks throwing and scoring.");
            yield return WaitUntil(()=>game.StageToken>token&&game.CanThrow);
            Assert.That(game.Projectiles.Any(p=>p.InFlight),Is.False);
            game.RegisterHit(game.Projectiles[0]);
            Assert.That(game.Score,Is.EqualTo(150),"A stale stage token must be rejected.");
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
            yield return WaitUntil(()=>game.Score==250);
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
            yield return WaitUntil(()=>game.Score==250);
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
            yield return WaitUntil(()=>game.Score==300);
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
                Assert.That(left&&right,Is.True,"Wind must vary between both directions.");
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
        public IEnumerator SurfaceMaterials_BasketballBounces_BeanbagSettles()
        {
            game.StartRound();
            var shots=new Projectile[3];
            var sources=new[]{Stage("Corner"),Stage("Basketball"),Stage("Carnival")};
            for(int i=0;i<3;i++)
            {
                shots[i]=game.Projectiles[System.Array.IndexOf(game.stages,sources[i])*game.poolSizePerStage];
                shots[i].Launch(sources[i].data,game.StageToken,new Vector3(-4+i*1.5f,1.4f,-4),Vector3.zero,Vector3.zero);
            }
            float[] rebound=new float[3];
            double end=Time.timeAsDouble+1.5f;
            while(Time.timeAsDouble<end)
            {
                for(int i=0;i<3;i++)if(shots[i].Body.linearVelocity.y>0.15f)
                    rebound[i]=Mathf.Max(rebound[i],shots[i].Body.position.y-sources[i].data.projectileRadius);
                yield return null;
            }
            Assert.That(rebound[1],Is.GreaterThan(0.35f),"Rubber basketball must visibly rebound.");
            Assert.That(rebound[1],Is.GreaterThan(rebound[2]+0.3f));
            Assert.That(rebound[2],Is.LessThan(0.1f),"Fabric beanbag must settle with little rebound.");
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
                    yield return WaitUntil(()=>game.Hits==targetHits&&game.CanThrow,3,"Seed="+seed+", stage="+game.ActiveStage.data.name);
                }
            }
        }

        [UnityTest]
        public IEnumerator CornerCleanShot_Awards50_AndUsesPaperPool()
        {
            StartStage("Corner");
            Assert.That(game.stages.Length,Is.EqualTo(4));
            Assert.That(game.Projectiles.Count,Is.EqualTo(game.poolSizePerStage*4));
            Assert.That(game.thrower.heldItems[0].activeSelf,Is.True);
            var center=game.ActiveStage.gate.transform.position;
            game.Launch(center+Vector3.up*0.5f,Vector3.down*3);
            yield return WaitUntil(()=>game.Hits==1);
            Assert.That(game.Score,Is.EqualTo(150));
            Assert.That(game.LastBonus,Is.EqualTo(50));
            Assert.That(game.hud.cleanShotEffect.label.text,Does.Contain("+50"));
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
                Assert.That(game.Score, Is.EqualTo(shot.CollisionCount * 30));
                Assert.That(shot.PropCollisionCount, Is.Zero, "The rim is part of the target.");
                yield return WaitUntil(() => game.Hits == 1, 2);
                int expected = 100 + shot.CollisionCount * 30;
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
            if(!shot)
            {
                Assert.That(game.Launch(new Vector3(6.4f,1.8f,-3),Vector3.right*8),Is.True);
                shot=game.Projectiles.First(p=>p.InFlight);
            }
            else
            {
                // Leave the contact manifold before starting a second real contact episode.
                shot.Body.position=new Vector3(6,1.8f,-3);shot.Body.linearVelocity=Vector3.zero;
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
        public IEnumerator CornerTwoContacts_Award60_AndReuseClearsCollisionHistory()
        {
            StartStage("Corner");
            yield return ContactWall();
            var shot=game.Projectiles.First(p=>p.InFlight);
            yield return ContactWall(shot);
            Assert.That(shot.CollisionCount,Is.EqualTo(2));
            Assert.That(shot.PropCollisionCount,Is.EqualTo(2));
            DropExistingShotThroughGate(shot);
            yield return WaitUntil(()=>game.Hits==1);
            Assert.That(game.LastWasClean,Is.False);
            Assert.That(game.LastBonus,Is.Zero,"Collision points were already awarded on contact.");
            Assert.That(game.Score,Is.EqualTo(160));
            Assert.That(game.LastAward,Is.EqualTo(100));
            game.StartRound();
            game.Launch(new Vector3(0,2,-3),Vector3.up);
            shot=game.Projectiles.First(p=>p.InFlight);
            Assert.That(shot.CollisionCount,Is.Zero);
            Assert.That(shot.PropCollisionCount,Is.Zero);
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
        public IEnumerator TargetRimContact_Awards30Immediately_AndCancelsCleanBonus()
        {
            Random.InitState(73021);
            StartStage("Corner");
            var center=game.ActiveStage.gate.transform.position;
            game.Launch(center+Vector3.right*.7f+Vector3.up*.25f,Vector3.left*.25f);
            var shot=game.Projectiles.First(p=>p.InFlight);
            yield return WaitUntil(()=>shot.CollisionCount>0,1);
            Assert.That(shot.PropCollisionCount,Is.Zero);
            Assert.That(game.Score,Is.EqualTo(shot.CollisionCount*30));
            DropExistingShotThroughGate(shot);
            yield return WaitUntil(()=>game.Hits==1);
            Assert.That(game.Score,Is.EqualTo(100+shot.CollisionCount*30));
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
        public IEnumerator StreetOrder_UsesFourProjectileTypes_AndHasNoBackdropTitles()
        {
            Assert.That(game.stages.Select(s=>s.data.name),Is.EqualTo(new[]{"Corner","Street","Basketball","Carnival"}));
            Assert.That(game.Projectiles.Count,Is.EqualTo(game.poolSizePerStage*4));
            Assert.That(game.stages.SelectMany(s=>s.GetComponentsInChildren<TMPro.TMP_Text>(true)),Is.Empty);
            var street=Stage("Street");
            Assert.That(street.data.points,Is.EqualTo(200));
            Assert.That(street.data.cleanShotBonus,Is.Zero);
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
            Assert.That(shot.Body.position.x-start.x,Is.GreaterThan(.08f),"Positive vertical spin should curve the bag to the right.");
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
            Assert.That(game.Score,Is.EqualTo(shot.CollisionCount*30));
            Assert.That(shot.PropCollisionCount,Is.Zero,"The lid is part of the target.");
            yield return WaitUntil(()=>game.Hits==1,3);
            int expected=200+shot.CollisionCount*30;
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
            Assert.That(game.LastAward,Is.EqualTo(200));
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
                Assert.That(game.Score,Is.EqualTo(200));
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
        public IEnumerator EveryStage_ContactsAward30BeforeGoal_AndPropsReset()
        {
            foreach(string name in new[]{"Corner","Street","Basketball","Carnival"})
            {
                StartStage(name);
                var props=game.ActiveStage.transform.Find("Extra Physics Props").GetComponentsInChildren<PhysicsProp>();
                Assert.That(props.Length,Is.EqualTo(6));
                Assert.That(props.All(p=>p.GetComponent<Collider>()&&!p.GetComponent<Collider>().isTrigger&&!p.GetComponent<Rigidbody>().isKinematic),Is.True);
                var prop=props.First(p=>p.name.EndsWith(" 1")&&p.GetComponent<BoxCollider>());
                Vector3 home=prop.transform.position;
                game.Launch(home+Vector3.back*.65f,Vector3.forward*6);
                var shot=game.Projectiles.First(p=>p.InFlight);
                yield return WaitUntil(()=>shot.CollisionCount>0,1);
                Assert.That(game.Hits,Is.Zero);
                Assert.That(game.Score,Is.EqualTo(shot.CollisionCount*30),name);
                Assert.That(game.hud.collisionFeedback.labels.Any(l=>l.gameObject.activeInHierarchy&&l.text=="+30"&&!l.raycastTarget),Is.True);
                shot.Retire();
                int before=game.Score;
                game.RegisterCollision(shot,home);
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
