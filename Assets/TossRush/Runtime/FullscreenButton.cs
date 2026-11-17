using UnityEngine;

namespace TossRush
{
    public sealed class FullscreenButton : MonoBehaviour
    {
        [SerializeField] GameObject enterFullscreenIcon, exitFullscreenIcon;
        bool expanded, hasState;
#if UNITY_EDITOR
        UnityEditor.EditorWindow gameView;
#endif

        void OnEnable()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // The web template handles fullscreen inside the browser's click event.
            gameObject.SetActive(false);
#else
#if UNITY_EDITOR
            foreach (var window in Resources.FindObjectsOfTypeAll<UnityEditor.EditorWindow>())
                if (window.GetType().FullName == "UnityEditor.GameView") { gameView = window; break; }
#endif
            RefreshIcon();
#endif
        }

        void Update() => RefreshIcon();

        public void ToggleFullscreen()
        {
#if UNITY_EDITOR
            if (!gameView)
                gameView = UnityEditor.EditorWindow.GetWindow(typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.GameView"));
            gameView.maximized = !gameView.maximized;
#elif !UNITY_WEBGL
            Screen.fullScreenMode = Screen.fullScreen ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow;
#endif
#if !UNITY_WEBGL || UNITY_EDITOR
            RefreshIcon();
#endif
        }

        void RefreshIcon()
        {
#if UNITY_EDITOR
            bool current = gameView && gameView.maximized;
#else
            bool current = Screen.fullScreen;
#endif
            if (hasState && expanded == current) return;
            hasState = true;
            expanded = current;
            enterFullscreenIcon.SetActive(!current);
            exitFullscreenIcon.SetActive(current);
        }
    }
}
