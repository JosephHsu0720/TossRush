using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TossRush.Editor
{
    public sealed class TossRushTestWindow : EditorWindow
    {
        Label normalBest, specialBest, unlockState, lastAction;
        VisualElement actions;

        [MenuItem("TossRush/測試工具")]
        public static void Open()
        {
            var window = GetWindow<TossRushTestWindow>();
            window.titleContent = new GUIContent("TossRush 測試工具");
            window.minSize = new Vector2(480, 460);
            window.Show();
        }

        public void CreateGUI()
        {
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/TossRush/Editor/TossRushTestWindow.uxml");
            tree.CloneTree(rootVisualElement);
            normalBest = rootVisualElement.Q<Label>("normalBest");
            specialBest = rootVisualElement.Q<Label>("specialBest");
            unlockState = rootVisualElement.Q<Label>("unlockState");
            lastAction = rootVisualElement.Q<Label>("lastAction");
            actions = rootVisualElement.Q("actions");
            rootVisualElement.Q<Button>("clearScores").clicked += () => Run(HighScoreTools.ClearHighScore,
                "已清除兩種模式最高分，保留特殊模式解鎖狀態。");
            rootVisualElement.Q<Button>("resetUnlock").clicked += () => Run(HighScoreTools.ResetSpecialModeUnlock,
                "已重設特殊模式解鎖與一般模式高分。");
            RefreshRecords();
            rootVisualElement.schedule.Execute(RefreshRecords).Every(300);
        }

        void Run(Action action, string message)
        {
            if (!HighScoreTools.CanModifyRecords) return;
            action(); lastAction.text = message; RefreshRecords();
        }

        void RefreshRecords()
        {
            int normal = PlayerPrefs.GetInt(GameDirector.BestKey, 0);
            normalBest.text = "一般模式最高分：" + normal;
            specialBest.text = "特殊模式最高分：" + PlayerPrefs.GetInt(GameDirector.SpecialBestKey, 0);
            bool unlocked = PlayerPrefs.GetInt(GameDirector.SpecialUnlockKey, 0) == 1 || normal >= GameDirector.SpecialUnlockScore;
            unlockState.text = "特殊模式：" + (unlocked ? "已解鎖" : "未解鎖") + "（一般模式單局需達 1200 分）";
            actions.SetEnabled(HighScoreTools.CanModifyRecords);
        }
    }
}
