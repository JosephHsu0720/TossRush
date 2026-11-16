using UnityEngine;

namespace TossRush
{
    public sealed class DartboardTarget : MonoBehaviour
    {
        public DartboardLayout layout;
        public MeshFilter maskFilter, outlineFilter;
        public ScoreGate scoreGate;
        public int CurrentRegionIndex { get; private set; } = -1;
        public DartboardLayout.Region CurrentRegion => CurrentRegionIndex >= 0 ? layout.regions[CurrentRegionIndex] : null;
        public string Instruction => "DARTS / HIT " + (CurrentRegion != null ? CurrentRegion.label : "THE YELLOW TARGET") + " / CLEAN +150";

        public void ChooseNextTarget()
        {
            int count = layout.regions.Length;
            int index = Random.Range(0, CurrentRegionIndex >= 0 && count > 1 ? count - 1 : count);
            if (CurrentRegionIndex >= 0 && count > 1 && index >= CurrentRegionIndex) index++;
            SelectRegion(index);
        }

        public void SelectRegion(int index)
        {
            CurrentRegionIndex = Mathf.Clamp(index, 0, layout.regions.Length - 1);
            var region = CurrentRegion;
            maskFilter.sharedMesh = region.mask;
            outlineFilter.sharedMesh = region.outline;
            Vector2 center = region.AimPoint;
            scoreGate.transform.localPosition = new Vector3(center.x, center.y, scoreGate.transform.localPosition.z);
            scoreGate.ResetTracking();
        }

        public bool ContainsWorldPoint(Vector3 point, float tipRadius)
        {
            if (CurrentRegion == null) return false;
            Vector3 local = transform.InverseTransformPoint(point);
            float scale = Mathf.Max(.001f, Mathf.Abs(transform.lossyScale.x));
            return CurrentRegion.Contains(new Vector2(local.x, local.y), tipRadius / scale);
        }
    }
}
