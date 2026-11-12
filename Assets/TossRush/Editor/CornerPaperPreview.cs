using System.IO;
using UnityEditor;
using UnityEngine;

namespace TossRush.Editor
{
    public static class CornerPaperPreview
    {
        const string Request = "Temp/TossRushPaperPreview.request";

        [InitializeOnLoadMethod]
        static void ReadRequest() => EditorApplication.delayCall += () => EditorApplication.delayCall += CaptureRequest;

        static void CaptureRequest()
        {
            if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(Request);
            Capture();
        }

        [MenuItem("TossRush/Preview Crumpled Paper")]
        public static void Capture()
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TossRush/Prefabs/Projectiles/PaperBallVisual.prefab");
            if (!asset) return;
            var preview = new PreviewRenderUtility();
            Texture2D image = null;
            try {
                preview.AddSingleGO(Object.Instantiate(asset));
                preview.camera.transform.position = new Vector3(1.1f,.7f,-1.6f);
                preview.camera.transform.LookAt(Vector3.zero);
                preview.camera.fieldOfView = 38;
                preview.camera.nearClipPlane = .01f;
                preview.camera.farClipPlane = 20;
                preview.camera.clearFlags = CameraClearFlags.SolidColor;
                preview.camera.backgroundColor = new Color(.16f,.18f,.20f);
                preview.lights[0].intensity = 1.2f;
                preview.lights[0].transform.rotation = Quaternion.Euler(40,30,0);
                preview.lights[1].intensity = .35f;
                preview.BeginStaticPreview(new Rect(0,0,768,768));
                preview.Render(true);
                image = preview.EndStaticPreview();
                File.WriteAllBytes("Temp/CornerPaperPreview.png",image.EncodeToPNG());
            }
            finally { if (image) Object.DestroyImmediate(image); preview.Cleanup(); }
        }
    }
}
