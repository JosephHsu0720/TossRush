using UnityEngine;
using UnityEngine.InputSystem;

namespace TossRush
{
    public sealed class ThrowController : MonoBehaviour
    {
        public GameDirector game;
        public Camera view;
        public Transform throwOrigin;
        public GameObject[] heldItems;
        public LineRenderer trajectory;
        [Range(0.01f, 1)] public float sensitivity = 0.13f;
        [Range(-10, 20)] public float restingPitch = 6f;
        float yaw, pitch, nextThrow;
        readonly Vector3[] positions = new Vector3[100];

        public Vector3 LaunchPosition => throwOrigin.position;
        public Vector3 LaunchVelocity => Quaternion.AngleAxis(-game.ActiveStage.data.throwLoft, view.transform.right) * view.transform.forward * game.ActiveStage.data.launchSpeed;

        public void ResetForStage(int index)
        {
            yaw = 0;
            // View the stage layout instead of tilting the camera along the ballistic launch angle.
            pitch = restingPitch;
            view.transform.localRotation = Quaternion.Euler(pitch, yaw, 0);
            nextThrow = 0;
            for (int i = 0; i < heldItems.Length; i++) heldItems[i].SetActive(i == game.ActiveStage.data.heldItemIndex);
            var spin = heldItems[game.ActiveStage.data.heldItemIndex].GetComponent<HeldItemSpin>();
            if (spin) spin.ResetSpin();
            trajectory.startColor = trajectory.endColor = game.ActiveStage.data.accent;
        }

        public void SetCursor(bool captured)
        {
            Cursor.lockState = captured ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !captured;
        }

        void Update()
        {
            bool playing = game.IsPlaying;
            trajectory.enabled = game.CanThrow && Cursor.lockState == CursorLockMode.Locked;
            if (!playing) return;
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) { SetCursor(false); return; }
            var mouse = Mouse.current;
            if (mouse == null) return;
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                if (mouse.leftButton.wasPressedThisFrame &&
                    !(UnityEngine.EventSystems.EventSystem.current && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()))
                    SetCursor(true);
                return;
            }
            Vector2 look = mouse.delta.ReadValue() * sensitivity;
            yaw = Mathf.Clamp(yaw + look.x, -38, 38);
            pitch = Mathf.Clamp(pitch - look.y, -25, 30);
            view.transform.localRotation = Quaternion.Euler(pitch, yaw, 0);
            if (game.CanThrow && mouse.leftButton.wasPressedThisFrame) TryThrow();
            if (trajectory.enabled) UpdateTrajectory();
        }

        public bool TryThrow()
        {
            if (!game.CanThrow || Time.time < nextThrow) return false;
            if (!game.Launch(LaunchPosition, LaunchVelocity)) return false;
            nextThrow = Time.time + game.ActiveStage.data.throwInterval;
            return true;
        }

        void UpdateTrajectory()
        {
            StageData data = game.ActiveStage.data;
            Vector3 position = LaunchPosition, velocity = LaunchVelocity;
            Vector3 spin = data.physicsProfile ? data.physicsProfile.initialSpin : Vector3.zero;
            int count = positions.Length;
            positions[0] = position;
            for (int i = 1; i < positions.Length; i++)
            {
                ProjectileFlight.Step(ref position, ref velocity, ref spin, data.physicsProfile, game.ActiveStage.CurrentWind * data.windInfluence, data.gravityScale, Time.fixedDeltaTime);
                positions[i] = position;
                Vector3 segment = position-positions[i-1];
                if (Physics.SphereCast(positions[i-1], data.projectileRadius, segment.normalized, out var hit, segment.magnitude, ~((1<<8)|(1<<9)), QueryTriggerInteraction.Ignore))
                { positions[i] = hit.point; count = i+1; break; }
            }
            trajectory.positionCount = count;
            for (int i = 0; i < count; i++) trajectory.SetPosition(i, positions[i]);
        }

        void OnDisable() => SetCursor(false);
    }
}
