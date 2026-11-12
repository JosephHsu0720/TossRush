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
        [Tooltip("Immediate points for every projectile collision before scoring, including target contacts.")]
        [Min(0)] public int pointsPerPropCollision;
        [Min(0)] public int heldItemIndex;
        [Min(1)] public float launchSpeed = 11f;
        [Tooltip("Upward loft relative to the viewing direction. Keeps the camera on the scene while throwing an arc.")]
        [Range(0, 65)] public float throwLoft = 22f;
        public ProjectilePhysicsProfile physicsProfile;
        [Min(0.1f)] public float gravityScale = 1f;
        [Tooltip("Fallback air velocity in m/s when randomization is off.")]
        public Vector3 wind;
        [Min(0)] public float windInfluence = 1f;
        [Min(0.1f)] public float throwInterval = 0.38f;
        [Min(0.01f)] public float projectileRadius = 0.13f;
        [Min(0.1f)] public float targetRadius = 0.62f;
        [Min(0)] public float motionAmplitude;
        [Min(0)] public float motionSpeed = 1.4f;
        [Header("Variation on each stage entry")]
        public bool randomize = true;
        public Vector2 horizontalOffsetRange = new Vector2(-0.8f, 0.8f);
        public Vector2 depthOffsetRange = new Vector2(-0.25f, 0.25f);
        public Vector2 windSpeedRange = new Vector2(1, 3);
        [Range(0, 30)] public float windDirectionSpread = 15f;
        public Projectile projectilePrefab;
        public Vector3 projectileEulerAngles;
    }
}
