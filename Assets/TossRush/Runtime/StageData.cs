using UnityEngine;

namespace TossRush
{
    [CreateAssetMenu(menuName = "TossRush/Stage Data")]
    public sealed class StageData : ScriptableObject
    {
        public string title;
        [TextArea] public string instruction;
        public Color accent = Color.cyan;
        [Min(1)] public int points = 100;
        [Header("Scoring")]
        [Min(0)] public int cleanShotBonus = 100;
        [Tooltip("Points stored per projectile collision, including target contacts. Awarded only when that projectile scores.")]
        [Min(0)] public int pointsPerPropCollision;
        [Tooltip("Minimum outward speed relative to the contacted surface in m/s. A rebound must exceed this to bank collision points.")]
        [Min(0)] public float minimumReboundSpeed = 1.5f;
        [Tooltip("Immediate deduction per new projectile contact with a pedestrian. Zero disables it for this stage.")]
        [Min(0)] public int pedestrianHitPenalty;
        [Min(0)] public int heldItemIndex;
        [Min(1)] public float launchSpeed = 11f;
        [Tooltip("Upward loft relative to the viewing direction. Keeps the camera on the scene while throwing an arc.")]
        [Range(0, 65)] public float throwLoft = 22f;
        [Tooltip("Throw straight along the viewing direction without additional upward loft. Camera pitch still controls elevation.")]
        public bool horizontalThrow;
        public ProjectilePhysicsProfile physicsProfile;
        [Min(0.1f)] public float gravityScale = 1f;
        [Tooltip("Fallback air velocity in m/s when randomization is off.")]
        public Vector3 wind;
        [Min(0)] public float windInfluence = 1f;
        [Tooltip("Visual wind-line width only. Does not change wind strength or projectile physics.")]
        [Min(.1f)] public float windLineWidthMultiplier = 1f;
        [Min(0.1f)] public float throwInterval = 0.38f;
        [Min(0.01f)] public float projectileRadius = 0.13f;
        [Min(0.1f)] public float targetRadius = 0.62f;
        [Min(0)] public float motionAmplitude;
        [Min(0)] public float motionSpeed = 1.4f;
        [Tooltip("One-time speed multiplier after this stage's first clear in the current round. One keeps the original speed.")]
        [Min(1)] public float motionSpeedAfterFirstClear = 1f;
        [Tooltip("Optional up/down motion, unlocked by this stage's own clears within the current round.")]
        [Min(0)] public float verticalMotionAmplitude;
        [Min(0)] public float verticalMotionSpeed = 1.2f;
        [Tooltip("Successful clears of this specific stage required before a later visit uses vertical motion.")]
        [Min(1)] public int verticalMotionUnlockHits = 1;
        [Header("Variation on each stage entry")]
        public bool randomize = true;
        public Vector2 horizontalOffsetRange = new Vector2(-0.8f, 0.8f);
        [Tooltip("Keep randomized horizontal offsets outside the central walkway. Zero allows the full range.")]
        [Min(0)] public float minimumHorizontalDistanceFromCenter;
        [Tooltip("Minimum change between consecutive horizontal samples, limited to half the configured range.")]
        [Min(0)] public float minimumHorizontalChange;
        public Vector2 depthOffsetRange = new Vector2(-0.25f, 0.25f);
        public Vector2 windSpeedRange = new Vector2(1, 3);
        [Range(0, 30)] public float windDirectionSpread = 15f;
        public Projectile projectilePrefab;
        public Vector3 projectileEulerAngles;
    }
}
