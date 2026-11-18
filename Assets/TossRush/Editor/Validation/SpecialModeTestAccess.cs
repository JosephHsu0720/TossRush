using System;
using UnityEditor;
using UnityEngine;

namespace TossRush.Editor
{
    // Existing stage tests need temporary access without unlocking the user's save.
    public sealed class SpecialModeTestAccess : IDisposable
    {
        readonly bool existed = PlayerPrefs.HasKey(GameDirector.SpecialUnlockKey);
        readonly int value = PlayerPrefs.GetInt(GameDirector.SpecialUnlockKey);
        bool disposed;
        public SpecialModeTestAccess()
        {
            PlayerPrefs.SetInt(GameDirector.SpecialUnlockKey, 1);
            EditorApplication.playModeStateChanged += OnPlayMode;
            AssemblyReloadEvents.beforeAssemblyReload += Dispose;
        }
        void OnPlayMode(PlayModeStateChange state) { if (state == PlayModeStateChange.ExitingPlayMode) Dispose(); }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            AssemblyReloadEvents.beforeAssemblyReload -= Dispose;
            if (existed) PlayerPrefs.SetInt(GameDirector.SpecialUnlockKey, value);
            else PlayerPrefs.DeleteKey(GameDirector.SpecialUnlockKey);
            PlayerPrefs.Save();
            var hud = UnityEngine.Object.FindFirstObjectByType<GameHUD>();
            if (hud) hud.RefreshSpecialModeAccess();
        }
    }
}
