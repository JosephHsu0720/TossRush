using UnityEngine;

namespace TossRush
{
    public sealed class RingAimGuidance : MonoBehaviour
    {
        public ThrowController controller;
        public Material guideMaterial;
        [SerializeField, Tooltip("Internal playtest switch. Off by default; no player-facing option.")]
        bool enableAimAssistForTesting;
        [Range(0, 4)] public float maximumCorrectionDegrees = 1.8f;
        public bool AimAssistEnabled
        {
            get => enableAimAssistForTesting;
            set
            {
                enableAimAssistForTesting = value;
                nextRefresh = 0;
                Assisted = false;
                CorrectionDegrees = 0;
                AssistedVelocity = Vector3.zero;
                if (!value) HideAssistVisuals();
            }
        }
        public RingTrajectory.Result Prediction { get; private set; }
        public Vector3 AssistedVelocity { get; private set; }
        public bool Assisted { get; private set; }
        public float CorrectionDegrees { get; private set; }
        public RingTarget ReadyTarget => AimAssistEnabled && controller && controller.game.CanThrow && Prediction.target && !Prediction.target.IsClaimed
            && Prediction.seconds + .02f < controller.game.Remaining ? Prediction.target : null;
        public float PreviewWidth => pathLine ? pathLine.widthMultiplier : 0;
        public string Hint { get; private set; }
        readonly Vector3[] path = new Vector3[151];
        readonly Vector3[] trial = new Vector3[151];
        readonly Candidate[] candidates = new Candidate[3];
        readonly Vector3[] circle = new Vector3[64];
        struct Candidate { public RingTarget target; public float error; }
        LineRenderer pathLine, landing, landingBorder, halo;
        Material material;
        Transform visualRoot;
        float nextRefresh;
        bool lastAssistEnabled;
        Vector3 lastVelocity, lastOrigin;
        RingTarget hintTarget;
        Vector3 hintVelocity;

        void Awake()
        {
            if (!controller) controller = GetComponent<ThrowController>();
            material = guideMaterial ? new Material(guideMaterial) : new Material(Shader.Find("TossRush/Ring Guide"));
            visualRoot = new GameObject("Ring aiming guide").transform;
            visualRoot.SetParent(transform, false);
            var original = controller.trajectory;
            pathLine = Line("Trajectory", original.widthMultiplier, original.sortingOrder);
            pathLine.sharedMaterial = original.sharedMaterial;
            pathLine.widthCurve = original.widthCurve;
            pathLine.numCapVertices = original.numCapVertices;
            pathLine.numCornerVertices = original.numCornerVertices;
            pathLine.alignment = original.alignment;
            pathLine.textureMode = original.textureMode;
            pathLine.sortingLayerID = original.sortingLayerID;
            landingBorder = Line("Landing contrast", .060f, 72, true);
            landing = Line("Ring footprint", .025f, 73, true);
            halo = Line("Selected prize", .028f, 74, true);
            SetVisible(false);
        }

        LineRenderer Line(string name, float width, int order, bool loop = false)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>();
            line.transform.SetParent(visualRoot, false);
            line.sharedMaterial = material; line.useWorldSpace = true; line.loop = loop;
            line.widthMultiplier = width; line.numCapVertices = 4; line.numCornerVertices = 3;
            line.sortingOrder = order; line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        void LateUpdate()
        {
            bool visible = controller && controller.enabled && controller.game.CanThrow
                && controller.game.ActiveStage.data.ringToss && Cursor.lockState == CursorLockMode.Locked;
            SetVisible(visible);
            if (visible) Refresh();
            else SetHud(false);
        }

        public void Refresh(bool force = false)
        {
            if (!controller || !controller.game.CanThrow || !controller.game.ActiveStage.data.ringToss) return;
            Vector3 raw = controller.LaunchVelocity, origin = controller.LaunchPosition;
            if (!force && Time.unscaledTime < nextRefresh && Vector3.Angle(raw, lastVelocity) < .12f
                && (origin - lastOrigin).sqrMagnitude < .0001f && lastAssistEnabled == AimAssistEnabled
                && (!Prediction.target || !Prediction.target.IsClaimed)) return;
            nextRefresh = Time.unscaledTime + .05f; lastVelocity = raw; lastOrigin = origin;
            lastAssistEnabled = AimAssistEnabled;
            StageView stage = controller.game.ActiveStage;
            AssistedVelocity = raw; Assisted = false; CorrectionDegrees = 0; hintTarget = null;
            Prediction = RingTrajectory.Predict(stage, origin, raw, path);
            if (AimAssistEnabled && !Prediction.target)
            {
                for (int i = 0; i < candidates.Length; i++) candidates[i] = new Candidate { error = float.PositiveInfinity };
                foreach (var target in stage.RingTargets)
                {
                    if (!target || target.IsClaimed || !target.gameObject.activeInHierarchy) continue;
                    if (!RingTrajectory.Crossing(stage, target, origin, raw, out var crossing)) continue;
                    float distance = Vector3.Distance(crossing, target.CapturePosition);
                    float error = Mathf.Atan2(distance, Vector3.Distance(origin, target.CapturePosition)) * Mathf.Rad2Deg;
                    for (int i = 0; i < candidates.Length; i++)
                    {
                        if (error >= candidates[i].error) continue;
                        for (int j = candidates.Length - 1; j > i; j--) candidates[j] = candidates[j - 1];
                        candidates[i] = new Candidate { target = target, error = error }; break;
                    }
                }
                float smallestAngle = float.PositiveInfinity;
                foreach (var candidate in candidates)
                {
                    if (!candidate.target || candidate.error > 8) continue;
                    Vector3 adjusted = Solve(stage, candidate.target, origin);
                    float correction = Vector3.Angle(raw, adjusted);
                    if (correction < smallestAngle)
                    { smallestAngle = correction; hintTarget = candidate.target; hintVelocity = adjusted; }
                    if (!AimAssistEnabled || correction > maximumCorrectionDegrees) continue;
                    var prediction = RingTrajectory.Predict(stage, origin, adjusted, trial);
                    if (prediction.target != candidate.target) continue;
                    Prediction = prediction; AssistedVelocity = adjusted; Assisted = true;
                    CorrectionDegrees = correction;
                    System.Array.Copy(trial, path, prediction.count);
                    break;
                }
            }
            Draw();
        }

        Vector3 Solve(StageView stage, RingTarget target, Vector3 origin)
        {
            float speed = stage.data.launchSpeed, gravity = Mathf.Abs(Physics.gravity.y) * stage.data.gravityScale;
            Vector3 goal = target.CapturePosition + Vector3.forward * .06f;
            Vector3 delta = goal - origin;
            float range = new Vector2(delta.x, delta.z).magnitude, v2 = speed * speed;
            float discriminant = v2 * v2 - gravity * (gravity * range * range + 2 * delta.y * v2);
            float elevation = Mathf.Atan2(v2 - Mathf.Sqrt(Mathf.Max(0, discriminant)), gravity * range) * Mathf.Rad2Deg + 3;
            float yaw = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
            float minElevation = stage.data.throwLoft - 30, maxElevation = stage.data.throwLoft + 25;
            Vector3 Velocity(float up, float side) => Quaternion.Euler(-up, side, 0) * Vector3.forward * speed;
            for (int i = 0; i < 8; i++)
            {
                elevation = Mathf.Clamp(elevation, minElevation, maxElevation);
                if (!RingTrajectory.Crossing(stage, target, origin, Velocity(elevation, yaw), out var p)) { elevation += 2; continue; }
                if (!RingTrajectory.Crossing(stage, target, origin, Velocity(elevation + .08f, yaw), out var pe)
                    || !RingTrajectory.Crossing(stage, target, origin, Velocity(elevation, yaw + .08f), out var py)) break;
                float ex = p.x - goal.x, ez = p.z - goal.z;
                if (ex * ex + ez * ez < .000004f) break;
                float a = (pe.x - p.x) / .08f, b = (py.x - p.x) / .08f, c = (pe.z - p.z) / .08f, d = (py.z - p.z) / .08f;
                float det = a * d - b * c;
                if (Mathf.Abs(det) < .000001f) break;
                elevation += Mathf.Clamp((-ex * d + ez * b) / det, -4, 4);
                yaw += Mathf.Clamp((ex * c - ez * a) / det, -4, 4);
            }
            return Velocity(Mathf.Clamp(elevation, minElevation, maxElevation), yaw);
        }

        void Draw()
        {
            bool ready = ReadyTarget;
            Color color = ready ? new Color(.25f, 1, .55f) : new Color(1, .92f, .25f);
            Color border = new Color(.025f, .035f, .06f, .95f);
            pathLine.positionCount = Prediction.count;
            for (int i = 0; i < Prediction.count; i++) pathLine.SetPosition(i, path[i]);
            Paint(pathLine, controller.game.ActiveStage.data.accent);
            if (!AimAssistEnabled) { HideAssistVisuals(); return; }
            landing.enabled = landingBorder.enabled = true;
            var ring = controller.game.ActiveStage.data.projectilePrefab.GetComponent<RingFlight>();
            Circle(Prediction.end + Vector3.up * .012f, Prediction.rotation, ring.innerRadius + ring.tubeRadius, landing);
            Circle(Prediction.end + Vector3.up * .012f, Prediction.rotation, ring.innerRadius + ring.tubeRadius, landingBorder);
            Paint(landing, Prediction.blocked && !Prediction.target ? new Color(1, .45f, .23f) : color); Paint(landingBorder, border);
            halo.enabled = ready;
            if (ready)
            {
                Circle(Prediction.target.transform.position + Vector3.up * .035f, Quaternion.identity, .34f, halo);
                Paint(halo, color);
                Hint = "READY  +" + Prediction.target.points + (Assisted ? "  /  AIM ASSIST" : "") + "\nLEFT CLICK TO THROW";
            }
            else if (Prediction.target)
                Hint = "TOO LATE FOR THIS PRIZE\nAIM AT A CLOSER ROW";
            else if (hintTarget)
            {
                float vertical = Mathf.Asin(hintVelocity.normalized.y) - Mathf.Asin(controller.LaunchVelocity.normalized.y);
                float horizontal = Vector3.SignedAngle(Vector3.ProjectOnPlane(controller.LaunchVelocity, Vector3.up), Vector3.ProjectOnPlane(hintVelocity, Vector3.up), Vector3.up) * Mathf.Deg2Rad;
                string direction = Mathf.Abs(vertical) > Mathf.Abs(horizontal) ? (vertical > 0 ? "AIM HIGHER" : "AIM LOWER")
                    : horizontal > 0 ? "AIM RIGHT" : "AIM LEFT";
                Hint = direction + "  /  +" + hintTarget.points + "\nMOVE MOUSE UNTIL THE RING TURNS GREEN";
            }
            else Hint = "FOLLOW THE LANDING RING\nAIM AT A PRIZE / GREEN = READY TO THROW";
            SetHud(true);
            if (controller.game.hud.ringAimText)
            { controller.game.hud.ringAimText.text = Hint; controller.game.hud.ringAimText.color = color; }
        }

        void Circle(Vector3 center, Quaternion rotation, float radius, LineRenderer line)
        {
            for (int i = 0; i < circle.Length; i++)
            {
                float angle = i * Mathf.PI * 2 / circle.Length;
                circle[i] = center + rotation * new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            }
            line.positionCount = circle.Length; line.SetPositions(circle);
        }
        static void Paint(LineRenderer line, Color color) => line.startColor = line.endColor = color;
        void SetVisible(bool visible)
        {
            if (!visualRoot) return;
            visualRoot.gameObject.SetActive(visible);
        }
        void SetHud(bool visible)
        {
            if (controller && controller.game && controller.game.hud && controller.game.hud.ringAimText)
                controller.game.hud.ringAimText.transform.parent.gameObject.SetActive(visible && AimAssistEnabled);
        }
        void HideAssistVisuals()
        {
            if (landing) landing.enabled = false;
            if (landingBorder) landingBorder.enabled = false;
            if (halo) halo.enabled = false;
            Hint = string.Empty;
            SetHud(false);
        }
        void OnDisable() { SetVisible(false); SetHud(false); }
        void OnDestroy() { if (material) Destroy(material); if (visualRoot) Destroy(visualRoot.gameObject); }
    }
}
