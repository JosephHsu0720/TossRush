using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TossRush
{
    public sealed class GameDirector : MonoBehaviour
    {
        public StageView[] stages;
        public StageView specialStage;
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
        public bool IsSpecialMode { get; private set; }
        public StageView ActiveStage => IsSpecialMode ? specialStage : stages[stageIndex];
        public int StageToken { get; private set; }
        public int Score { get; private set; }
        public int Best { get; private set; }
        public int Hits { get; private set; }
        public int Throws { get; private set; }
        public int LastAward { get; private set; }
        public TrashColor NextTrashColor { get; private set; }
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
        public const string SpecialBestKey = "TossRush.RingToss.Best.v1";
        public const string SpecialUnlockKey = "TossRush.RingToss.Unlocked.v1";
        public const int SpecialUnlockScore = 1200;
        public int SpecialUnlockProgress => PlayerPrefs.GetInt(BestKey, 0);
        public bool SpecialModeUnlocked => PlayerPrefs.GetInt(SpecialUnlockKey, 0) == 1
            || SpecialUnlockProgress >= SpecialUnlockScore;
        string CurrentBestKey => IsSpecialMode ? SpecialBestKey : BestKey;

        void Awake()
        {
            Best = PlayerPrefs.GetInt(BestKey, 0);
            CheckSpecialModeUnlock();
            Physics.IgnoreLayerCollision(8, 8);
            Physics.IgnoreLayerCollision(8, 9);
            // Release any authored stage atmosphere before selecting the menu stage.
            foreach (var stage in stages) stage.gameObject.SetActive(false);
            if (specialStage) specialStage.gameObject.SetActive(false);
            var sharedPools = new Dictionary<Projectile, List<Projectile>>();
            for (int i = 0; i < stages.Length + (specialStage ? 1 : 0); i++)
            {
                var stage = i < stages.Length ? stages[i] : specialStage;
                stage.Initialize(this);
                if (!sharedPools.TryGetValue(stage.data.projectilePrefab, out var pool))
                {
                    pool = new List<Projectile>();
                    sharedPools.Add(stage.data.projectilePrefab, pool);
                    for (int j = 0; j < poolSizePerStage; j++)
                    {
                        var shot = Instantiate(stage.data.projectilePrefab, poolRoot);
                        shot.Owner = this;
                        shot.name = stage.data.title + " Shot " + j;
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
            if (IsSpecialMode && !SpecialModeUnlocked) return;
            RetireAll();
            Score = Hits = Throws = 0;
            foreach (var stage in stages) stage.ResetRoundProgress();
            if (specialStage) specialStage.ResetRoundProgress();
            LastAward = LastBonus = 0;
            LastWasClean = false;
            hasResult = false;
            IsTransitioning = false;
            IsPlaying = true;
            // A jam round has a wall-clock deadline, including stalls and lost browser focus.
            roundEnd = Time.realtimeSinceStartupAsDouble + CurrentRoundDuration;
            ActivateStage(Mathf.Clamp(startingStageIndex, 0, stages.Length - 1));
            hud.ShowPlay();
            thrower.SetCursor(true);
            if (gameAudio) gameAudio.Play(gameAudio.start);
        }

        public float CurrentRoundDuration => IsSpecialMode ? 20f : roundDuration;

        public void StartNormalRound()
        {
            IsSpecialMode = false;
            Best = PlayerPrefs.GetInt(BestKey, 0);
            StartRound();
        }

        public void StartSpecialRound()
        {
            CheckSpecialModeUnlock();
            if (!specialStage || !SpecialModeUnlocked) return;
            IsSpecialMode = true;
            Best = PlayerPrefs.GetInt(SpecialBestKey, 0);
            StartRound();
        }

        public void ReturnToMenu()
        {
            IsPlaying = IsTransitioning = hasResult = false;
            RetireAll();
            IsSpecialMode = false;
            Best = PlayerPrefs.GetInt(BestKey, 0);
            ActivateStage(0);
            thrower.SetCursor(false);
            hud.ShowMenu(Best);
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
            foreach (var shot in pools[IsSpecialMode ? stages.Length : stageIndex])
            {
                if (shot.InFlight) continue;
                shot.Launch(ActiveStage.data, StageToken, position, velocity, ActiveStage.CurrentWind, NextTrashColor);
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
            CheckSpecialModeUnlock();
            Hits++;
            ActiveStage.RecordClear();
            string detail = LastWasClean ? "CLEAN SHOT" : "NICE SHOT";
            hud.ShowFeedback("+" + LastAward + "   " + detail, data.accent);
            if (LastWasClean && LastBonus > 0) hud.ShowCleanShot(LastBonus);
            if (gameAudio) gameAudio.Play(hitClip, .85f);
            else if (hitClip) audioSource.PlayOneShot(hitClip, .68f);
            BeginTransition();
        }

        public void RegisterRingHit(Projectile shot, RingTarget target)
        {
            if (!CanScore || !IsSpecialMode || !target || target.stage != ActiveStage || !shot
                || shot.Owner != this || !shot.InFlight || shot.Scored || shot.StageToken != StageToken) return;
            if (!target.TryScore(shot) || !target.TryClaim()) return;
            shot.MarkScored();
            LastWasClean = false;
            LastBonus = 0;
            LastAward = target.points;
            Score += LastAward;
            Hits++;
            ActiveStage.RecordClear();
            hud.ShowCollision(LastAward, target.CapturePosition, thrower.view, ActiveStage.data.accent);
            hud.ShowFeedback("+" + LastAward + "   RING ON!", ActiveStage.data.accent);
            if (gameAudio) gameAudio.Play(hitClip, .85f);
        }

        void PrepareStageTrashBag()
        {
            NextTrashColor = ActiveStage.data.matchTrashColors ? (TrashColor)Random.Range(1, 4) : TrashColor.None;
            thrower.RefreshHeldTrashColor();
        }

        public void RegisterTargetEntry(Projectile shot, ScoreGate target)
        {
            if (!CanScore || !shot || shot.Owner != this || !shot.InFlight || shot.Scored || shot.StageToken != StageToken) return;
            if (!target || target.stage != ActiveStage) return;
            // Resolve each launch once, including a wrong bin, so a bounce cannot score twice.
            shot.MarkScored();
            var data = ActiveStage.data;
            if (!data.matchTrashColors || shot.TrashColor != TrashColor.None && shot.TrashColor == target.trashColor)
            {
                RegisterHit(shot);
                return;
            }
            LastWasClean = false;
            LastBonus = 0;
            LastAward = -data.wrongBinPenalty;
            Score += LastAward;
            hud.ShowPenalty(data.wrongBinPenalty, target.transform.position, thrower.view);
            hud.ShowFeedback("-" + data.wrongBinPenalty + "   WRONG BIN", new Color(1, .18f, .12f));
            if (gameAudio && gameAudio.playPenaltySounds) gameAudio.Play(gameAudio.penalty);
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
            CheckSpecialModeUnlock();
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
                if (IsSpecialMode || i != index) stages[i].gameObject.SetActive(false);
            if (specialStage) specialStage.gameObject.SetActive(IsSpecialMode);
            ActiveStage.gameObject.SetActive(true);
            ActiveStage.ResetStage();
            PrepareStageTrashBag();
            thrower.ResetForStage(index);
        }

        void FinishRound()
        {
            CheckSpecialModeUnlock();
            IsPlaying = IsTransitioning = false;
            hasResult = true;
            RetireAll();
            bool newRecord = Score > Best;
            if (newRecord) { Best = Score; PlayerPrefs.SetInt(CurrentBestKey, Best); PlayerPrefs.Save(); }
            thrower.SetCursor(false);
            hud.SetTransition(0);
            hud.ShowResult(Score, Best, Hits, Throws, newRecord);
            if (gameAudio) gameAudio.Play(gameAudio.finish);
        }

        public void ClearHighScore()
        {
            CheckSpecialModeUnlock();
            Best = 0;
            PlayerPrefs.DeleteKey(BestKey);
            PlayerPrefs.DeleteKey(SpecialBestKey);
            PlayerPrefs.Save();
            if (!IsPlaying)
            {
                if (hasResult) hud.ShowResult(Score, 0, Hits, Throws, false);
                else hud.ShowMenu(0);
            }
        }

        void CheckSpecialModeUnlock()
        {
            if (PlayerPrefs.GetInt(SpecialUnlockKey, 0) == 1) return;
            if (SpecialUnlockProgress < SpecialUnlockScore && (!IsPlaying || IsSpecialMode || Score < SpecialUnlockScore)) return;
            PlayerPrefs.SetInt(SpecialUnlockKey, 1);
            PlayerPrefs.Save();
        }

        void RetireAll() { foreach (var shot in allShots) shot.Retire(); }
        void OnDisable() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    }
}
