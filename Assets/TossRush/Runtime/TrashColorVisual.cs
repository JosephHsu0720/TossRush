using UnityEngine;

namespace TossRush
{
    public enum TrashColor { None, Red, Green, Blue }

    public sealed class TrashColorVisual : MonoBehaviour
    {
        public Renderer[] surfaces;
        public Material red, green, blue;

        public void Apply(TrashColor color)
        {
            Material material = color == TrashColor.Red ? red : color == TrashColor.Green ? green : color == TrashColor.Blue ? blue : null;
            if (!material) return;
            foreach (var surface in surfaces) surface.sharedMaterial = material;
        }
    }
}
