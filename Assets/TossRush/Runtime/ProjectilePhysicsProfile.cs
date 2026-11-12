using UnityEngine;

namespace TossRush
{
    [CreateAssetMenu(menuName = "TossRush/Projectile Physics Profile")]
    public sealed class ProjectilePhysicsProfile : ScriptableObject
    {
        public string displayName;
        [Min(0.001f)] public float mass = 0.3f;
        [Tooltip("Effective projected area in square metres. Visual size is exaggerated for readability.")]
        [Min(0.0001f)] public float referenceArea = 0.014f;
        [Min(0)] public float dragCoefficient = 0.9f;
        [Min(0)] public float angularDamping = 1f;
        public Vector3 initialSpin = new Vector3(2, 1, 3);
        [Tooltip("Stylized spin lift. Zero keeps ordinary paper, basketball and beanbag flight unchanged.")]
        [Min(0)] public float spinLift;
        [Min(0)] public float maxSpinAcceleration = 6f;
        [Min(0.1f)] public float flightLifetime = 4f;
        public PhysicsMaterial surfaceMaterial;
    }
}
