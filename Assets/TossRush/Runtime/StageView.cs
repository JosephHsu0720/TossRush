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
        public float CurrentVerticalAmplitude { get; private set; }
        public float CurrentMotionSpeedMultiplier { get; private set; } = 1;
        public int ClearsThisRound { get; private set; }
        Vector3 home;
        float elapsed, motionPhase;
        float lastHorizontalOffset;
        bool hasHorizontalOffset;
        PhysicsProp[] props;
        TrafficVehicle[] vehicles;
        Pedestrian[] pedestrians;
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
            foreach (var prop in props) { prop.stage = this; prop.InitializePose(); }
            vehicles = GetComponentsInChildren<TrafficVehicle>(true);
            foreach (var vehicle in vehicles) vehicle.InitializePose();
            pedestrians = GetComponentsInChildren<Pedestrian>(true);
            foreach (var pedestrian in pedestrians) pedestrian.InitializePose();
        }

        public void ResetStage()
        {
            elapsed = 0;
            CurrentTargetCenter = home;
            CurrentWind = data.wind;
            motionPhase = 0;
            CurrentVerticalAmplitude = ClearsThisRound >= data.verticalMotionUnlockHits ? data.verticalMotionAmplitude : 0;
            CurrentMotionSpeedMultiplier = ClearsThisRound > 0 ? Mathf.Max(1, data.motionSpeedAfterFirstClear) : 1;
            if (data.randomize)
            {
                float horizontal = SampleHorizontalOffset();
                CurrentTargetCenter += new Vector3(horizontal, 0, Sample(data.depthOffsetRange));
                float speed = Mathf.Max(0, Sample(data.windSpeedRange));
                float side = Random.value < 0.5f ? -1 : 1;
                CurrentWind = Quaternion.Euler(0, Random.Range(-data.windDirectionSpread, data.windDirectionSpread), 0) * Vector3.right * side * speed;
                motionPhase = Random.Range(0, Mathf.PI * 2);
            }
            movingTarget.localPosition = PositionAtTime(0);
            foreach (var targetGate in gates) targetGate.ResetTracking();
            foreach (var prop in props) prop.ResetPose();
            foreach (var vehicle in vehicles) vehicle.ResetPose();
            foreach (var pedestrian in pedestrians) pedestrian.ResetPose();
        }

        static float Sample(Vector2 range) => Random.Range(Mathf.Min(range.x, range.y), Mathf.Max(range.x, range.y));

        internal void RecordClear() => ClearsThisRound++;
        internal void ResetRoundProgress() { ClearsThisRound = 0; CurrentVerticalAmplitude = 0; CurrentMotionSpeedMultiplier = 1; }

        float SampleHorizontalOffset()
        {
            float low = Mathf.Min(data.horizontalOffsetRange.x, data.horizontalOffsetRange.y);
            float high = Mathf.Max(data.horizontalOffsetRange.x, data.horizontalOffsetRange.y);
            float change = Mathf.Min(data.minimumHorizontalChange, (high - low) * .5f);
            float value = SampleAllowedHorizontal(low, high);
            if (hasHorizontalOffset && change > 0)
            {
                for (int i = 0; i < 8 && Mathf.Abs(value - lastHorizontalOffset) < change; i++) value = SampleAllowedHorizontal(low, high);
                if (Mathf.Abs(value - lastHorizontalOffset) < change)
                    value = lastHorizontalOffset - low > high - lastHorizontalOffset ? low : high;
            }
            hasHorizontalOffset = true; lastHorizontalOffset = value;
            return value;
        }

        float SampleAllowedHorizontal(float low, float high)
        {
            float gap = data.minimumHorizontalDistanceFromCenter;
            if (gap <= 0 || high <= -gap || low >= gap) return Random.Range(low, high);
            float leftLength = Mathf.Max(0, Mathf.Min(high, -gap) - low);
            float rightStart = Mathf.Max(low, gap);
            float rightLength = Mathf.Max(0, high - rightStart);
            if (leftLength + rightLength <= 0) return Random.Range(low, high);
            float distance = Random.Range(0, leftLength + rightLength);
            return distance < leftLength ? low + distance : rightStart + distance - leftLength;
        }

        internal bool TryScoreBeforeInteriorContact(Projectile shot, Collision collision)
        {
            foreach (var targetGate in gates) if (targetGate.TryScoreBeforeInteriorContact(shot, collision)) return true;
            return false;
        }

        Vector3 PositionAtTime(float time) => CurrentTargetCenter
            + Vector3.right * (Mathf.Sin(time * data.motionSpeed * CurrentMotionSpeedMultiplier + motionPhase) * data.motionAmplitude)
            + Vector3.up * (Mathf.Sin(time * data.verticalMotionSpeed * CurrentMotionSpeedMultiplier + motionPhase) * CurrentVerticalAmplitude);

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
