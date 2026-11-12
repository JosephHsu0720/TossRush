using UnityEngine;

namespace TossRush
{
    public sealed class HeldItemSpin : MonoBehaviour
    {
        public float degreesPerSecond = 150;
        Quaternion restingRotation;
        float angle;
        bool initialized;
        void Awake() => Initialize();
        void Initialize()
        {
            if (initialized) return;
            restingRotation = transform.localRotation;
            initialized = true;
        }
        void OnEnable() => ResetSpin();
        public void ResetSpin()
        {
            Initialize();
            angle = 0;
            transform.localRotation = restingRotation;
        }
        void Update()
        {
            angle = (angle + degreesPerSecond * Time.deltaTime) % 360;
            Quaternion parentRotation = transform.parent ? transform.parent.rotation : Quaternion.identity;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.up) * parentRotation * restingRotation;
        }
    }
}
