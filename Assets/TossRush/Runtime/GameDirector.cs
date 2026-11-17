using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TossRush
{
    public sealed class GameDirector : MonoBehaviour
    {
        public StageView[] stages;
        public ThrowController thrower;
        public GameHUD hud;
        public Transform poolRoot;
        public AudioSource audioSource;
        public AudioClip hitClip, throwClip;
        public GameAudio gameAudio;
        [Min(1)] public float roundDuration = 20f;
        [Min(0.1f)] public float transitionDuration = 0.3f;
        [Range(4, 32)] public int poolSizePerStage = 16;
        [Tooltip("Choose a starting stage for Editor playtesting. Normal play starts at zero.")]
        [Min(0)] public int startingStageIndex;
        [Tooltip("Editor only: repeat the selected starting stage after each hit. Player builds use normal rotation.")]
        public bool repeatStartingStage;

        public bool IsPlaying { get; private set; }
        public bool IsTransitioning { get; private set; }
        public bool CanScore => IsPlaying && !IsTransitioning && Time.realtimeSinceStartupAsDouble < roundEnd;
        public bool CanThrow => CanScore;
        public StageView ActiveStage => stages[stageIndex];
        public int StageToken { get; private set; }
        public int Score { get; private set; }
        public int Best { get; private set; }
        public int Hits { get; private set; }
        public int Throws { get; private set; }
        public int LastAward { get; private set; }
        public int LastBonus { get; private set; }
        public bool LastWasClean { get; private set; }
        public float Remaining => IsPlaying ? Mathf.Max(0, (float)(roundEnd - Time.realtimeSinceStartupAsDouble)) : 0;
        public IReadOnlyList<Projectile> Projectiles => allShots;
        readonly List<Projectile> allShots = new List<Projectile>();
        readonly List<List<Projectile>> pools = new List<List<Projectile>>();
        int stageIndex, nextStage;
        double roundEnd, transitionStart;
        bool hasResult, transitionActivated;
        public const string BestKey = "TossRush.Best.v1";

        void Awake()
        {
            Best = PlayerPrefs.GetInt(BestKey, 0);
            Physics.IgnoreLayerCollision(8, 8);
            Physics.IgnoreLayerCollision(8, 9);
            // Release any authored stage atmosphere before selecting the menu stage.
            foreach (var stage in stages) stage.gameObject.SetActive(false);
            var sharedPools = new Dictionary<Projectile, List<Projectile>>();
            for (int i = 0; i < stages.Length; i++)
            {
                stages[i].Initialize(this);
                if (!sharedPools.TryGetValue(stages[i].data.projectilePrefab, out var pool))
                {
                    pool = new List<Projectile>();
                    sharedPools.Add(stages[i].data.projectilePrefab, pool);
                    for (int j = 0; j < poolSizePerStage; j++)
                    {
                        var shot = Instantiate(stages[i].data.projectilePrefab, poolRoot);
                        shot.Owner = this;
                        shot.name = stages[i].data.title + " Shot " + j;
                        shot.Retire();
                        pool.Add(shot); allShots.Add(shot);
                    }
                }
                pools.Add(pool);
            }
            stages[0].gameObject.SetActive(true);
            hud.Initialize(this);
            thrower.ResetForStage(0);
            thrower.SetCursor(false);
            hud.ShowMenu(Best);
        }

        public void StartRound()
        {
            RetireAll();
            Score = Hits = Throws = 0;
            foreach (var stage in stages) stage.ResetRoundProgress();
            LastAward = LastBonus = 0;
            LastWasClean = false;
            hasResult = false;
            IsTransitioning = false;
            IsPlaying = true;
            // A jam round has a wall-clock deadline, including stalls and lost browser focus.
            roundEnd = Time.realtimeSinceStartupAsDouble + roundDuration;
            ActivateStage(Mathf.Clamp(startingStageIndex, 0, stages.Length - 1));
            hud.ShowPlay();
            thrower.SetCursor(true);
            if (gameAudio) gameAudio.Play(gameAudio.start);
        }

        void Update()
        {
            if (!IsPlaying)
            {
                if (hasResult && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) StartRound();
                return;
            }
            if (Time.realtimeSinceStartupAsDouble >= roundEnd) { FinishRound(); return; }
            if (IsTransitioning)
            {
                float t = (float)((Time.timeAsDouble-transitionStart)/transitionDuration);
                hud.SetTransition(1-Mathf.Abs(2*Mathf.Clamp01(t)-1));
                if (t >= 0.5f && !transitionActivated)
                {
                    ActivateStage(nextStage);
                    transitionActivated = true;
                }
                if (t >= 1) { IsTransitioning = false; hud.SetTransition(0); }
            }
            hud.Refresh(Remaining, Score, ActiveStage.data,
                Cursor.lockState != CursorLockMode.Locked);
        }

        public bool Launch(Vector3 position, Vector3 velocity)
        {
            if (!CanThrow) return false;
            foreach (var shot in pools[stageIndex])
            {
                if (shot.InFlight) continue;
                shot.Launch(ActiveStage.data, StageToken, position, velocity, ActiveStage.CurrentWind);
                Throws++;
                if (gameAudio && gameAudio.playThrowSounds) gameAudio.Play(throwClip);
                return true;
            }
            return false;
        }

        public void RegisterCollision(Projectile shot, Vector3 point, float reboundSpeed = 0)
        {
            if (!CanScore || !shot || shot.Owner != this || !shot.InFlight || shot.Scored || shot.StageToken != StageToken) return;
            if (!(reboundSpeed > ActiveStage.data.minimumReboundSpeed)) return;
            int points = ActiveStage.data.pointsPerPropCollision;
            if (points <= 0) return;
            shot.AccumulateCollisionPoints(points);
            hud.ShowCollision(points, point, thrower.view, ActiveStage.data.accent);
        }

        public void RegisterHit(Projectile shot)
        {
            if (!CanScore || !shot || shot.Owner != this || !shot.Scored || !shot.InFlight || shot.StageToken != StageToken) return;
            var data = ActiveStage.data;
            LastWasClean = shot.CollisionCount == 0;
            LastBonus = LastWasClean ? data.cleanShotBonus : 0;
            LastAward = data.points + LastBonus + shot.PendingCollisionPoints;
            Score += LastAward;
            Hits++;
            ActiveStage.RecordClear();
            string detail = LastWasClean ? "CLEAN SHOT" : "NICE SHOT";
            hud.ShowFeedback("+" + LastAward + "   " + detail, data.accent);
            if (LastWasClean && LastBonus > 0) hud.ShowCleanShot(LastBonus);
            if (gameAudio) gameAudio.Play(hitClip, .85f);
            else if (hitClip) audioSource.PlayOneShot(hitClip, .68f);
            BeginTransition();
        }

        public void RegisterPedestrianHit(Projectile shot, Pedestrian pedestrian, Vector3 point)
        {
            if (!CanScore || !shot || shot.Owner != this || !shot.InFlight || shot.Scored || shot.StageToken != StageToken) return;
            if (!pedestrian || pedestrian.stage != ActiveStage) return;
            int penalty = ActiveStage.data.pedestrianHitPenalty;
            if (penalty <= 0) return;
            Score -= penalty;
            if (gameAudio && gameAudio.playPenaltySounds) gameAudio.Play(gameAudio.penalty);
            hud.ShowPenalty(penalty, point, thrower.view);
        }

        internal void RegisterKnockdown(PhysicsProp prop)
        {
            if (!CanScore || !prop || prop.stage != ActiveStage || !prop.KnockedDown || prop.knockdownPoints <= 0) return;
            Score += prop.knockdownPoints;
            if (gameAudio && gameAudio.playRewardSounds) gameAudio.Play(gameAudio.reward, .65f);
            hud.collisionFeedback.Show(prop.knockdownPoints, prop.transform.position, thrower.view, ActiveStage.data.accent, false);
        }

        void BeginTransition()
        {
            IsTransitioning = true;
            transitionActivated = false;
            transitionStart = Time.timeAsDouble;
            nextStage = (stageIndex+1)%stages.Length;
#if UNITY_EDITOR
            if (repeatStartingStage) nextStage = stageIndex;
#endif
            RetireAll();
        }

        void ActivateStage(int index)
        {
            RetireAll();
            stageIndex = index;
            StageToken++;
            // Let the previous stage restore its lighting before the next one takes it.
            for (int i = 0; i < stages.Length; i++)
                if (i != index) stages[i].gameObject.SetActive(false);
            ActiveStage.gameObject.SetActive(true);
            ActiveStage.ResetStage();
            thrower.ResetForStage(index);
        }

        void FinishRound()
        {
            IsPlaying = IsTransitioning = false;
            hasResult = true;
            RetireAll();
            bool newRecord = Score > Best;
            if (newRecord) { Best = Score; PlayerPrefs.SetInt(BestKey, Best); PlayerPrefs.Save(); }
            thrower.SetCursor(false);
            hud.SetTransition(0);
            hud.ShowResult(Score, Best, Hits, Throws, newRecord);
            if (gameAudio) gameAudio.Play(gameAudio.finish);
        }

        public void ClearHighScore()
        {
            Best = 0;
            PlayerPrefs.DeleteKey(BestKey);
            PlayerPrefs.Save();
            if (!IsPlaying)
            {
                if (hasResult) hud.ShowResult(Score, 0, Hits, Throws, false);
                else hud.ShowMenu(0);
            }
        }

        void RetireAll() { foreach (var shot in allShots) shot.Retire(); }
        void OnDisable() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    }
}
