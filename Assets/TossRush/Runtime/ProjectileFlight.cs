using UnityEngine;

namespace TossRush
{
    // Shared, collision-free flight model for Rigidbody forces and the trajectory preview.
    public static class ProjectileFlight
    {
        const float AirDensity = 1.225f;

        public static Vector3 Acceleration(ProjectilePhysicsProfile profile, Vector3 velocity, Vector3 wind, float gravityScale)
            => Acceleration(profile, velocity, wind, gravityScale, profile ? profile.initialSpin : Vector3.zero);

        public static Vector3 Acceleration(ProjectilePhysicsProfile profile, Vector3 velocity, Vector3 wind, float gravityScale, Vector3 angularVelocity)
        {
            Vector3 acceleration = Physics.gravity * gravityScale;
            if (!profile) return acceleration;
            Vector3 relativeAirVelocity = wind - velocity;
            float dragPerMass = 0.5f * AirDensity * profile.dragCoefficient * profile.referenceArea / Mathf.Max(0.001f, profile.mass);
            Vector3 spinForce = Vector3.ClampMagnitude(Vector3.Cross(angularVelocity, -relativeAirVelocity) * profile.spinLift, profile.maxSpinAcceleration);
            return acceleration + relativeAirVelocity * (relativeAirVelocity.magnitude * dragPerMass) + spinForce;
        }

        public static void Step(ref Vector3 position, ref Vector3 velocity, ProjectilePhysicsProfile profile, Vector3 wind, float gravityScale, float deltaTime)
        {
            velocity += Acceleration(profile, velocity, wind, gravityScale) * deltaTime;
            position += velocity * deltaTime;
        }

        // Carry spin through prediction so angular damping changes the curve exactly as it does in flight.
        public static void Step(ref Vector3 position, ref Vector3 velocity, ref Vector3 angularVelocity,
            ProjectilePhysicsProfile profile, Vector3 wind, float gravityScale, float deltaTime)
        {
            velocity += Acceleration(profile, velocity, wind, gravityScale, angularVelocity) * deltaTime;
            position += velocity * deltaTime;
            if (profile) angularVelocity *= Mathf.Max(0, 1 - profile.angularDamping * deltaTime);
        }
    }
}
