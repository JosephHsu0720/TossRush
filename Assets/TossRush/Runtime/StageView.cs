using UnityEngine;

namespace TossRush
{
    [DefaultExecutionOrder(-200)]
    public sealed class StageView : MonoBehaviour
    {
        public StageData data;
        public Transform movingTarget;
        public ScoreGate gate;
        [Min(0.01f)] public float authoredRadius = 0.67f;
        public GameDirector Game { get; private set; }
        public Vector3 CurrentWind { get; private set; }
        public Vector3 CurrentTargetCenter { get; private set; }
        Vector3 home;
        float elapsed, motionPhase;
        PhysicsProp[] props;
        TrafficVehicle[] vehicles;
        ScoreGate[] gates;

        public void Initialize(GameDirector game)
        {
            Game = game;
            home = movingTarget.localPosition;
            movingTarget.localScale = Vector3.one * (data.targetRadius / authoredRadius);
            CurrentTargetCenter = home;
            CurrentWind = data.wind;
            gates = movingTarget.GetComponentsInChildren<ScoreGate>(true);
            foreach (var targetGate in gates) targetGate.stage = this;
            props = GetComponentsInChildren<PhysicsProp>(true);
            foreach (var prop in props) prop.InitializePose();
            vehicles = GetComponentsInChildren<TrafficVehicle>(true);
            foreach (var vehicle in vehicles) vehicle.InitializePose();
        }

        public void ResetStage()
        {
            elapsed = 0;
            CurrentTargetCenter = home;
            CurrentWind = data.wind;
            motionPhase = 0;
            if (data.randomize)
            {
                CurrentTargetCenter += new Vector3(Sample(data.horizontalOffsetRange), 0, Sample(data.depthOffsetRange));
                float speed = Mathf.Max(0, Sample(data.windSpeedRange));
                float side = Random.value < 0.5f ? -1 : 1;
                CurrentWind = Quaternion.Euler(0, Random.Range(-data.windDirectionSpread, data.windDirectionSpread), 0) * Vector3.right * side * speed;
                motionPhase = Random.Range(0, Mathf.PI * 2);
            }
            movingTarget.localPosition = PositionAtTime(0);
            foreach (var targetGate in gates) targetGate.ResetTracking();
            foreach (var prop in props) prop.ResetPose();
            foreach (var vehicle in vehicles) vehicle.ResetPose();
        }

        static float Sample(Vector2 range) => Random.Range(Mathf.Min(range.x, range.y), Mathf.Max(range.x, range.y));

        Vector3 PositionAtTime(float time) => CurrentTargetCenter + Vector3.right * (Mathf.Sin(time * data.motionSpeed + motionPhase) * data.motionAmplitude);

        public Vector3 PredictedGatePosition(float secondsAhead)
        {
            Vector3 offset = PositionAtTime(elapsed + secondsAhead) - movingTarget.localPosition;
            return gate.transform.position + transform.TransformVector(offset);
        }

        void FixedUpdate()
        {
            if (!Game || !Game.CanScore) return;
            elapsed += Time.fixedDeltaTime;
            movingTarget.localPosition = PositionAtTime(elapsed);
        }
    }
}
