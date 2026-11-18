using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TossRush.Editor
{
    public sealed class TossRushTestWindow : EditorWindow
    {
        Label normalBest, specialBest, unlockState, lastAction;
        VisualElement actions;
        VisualElement stageActions;
        DropdownField stageChoice;
        Label stageStatus;
        GameDirector stageGame;

        [MenuItem("TossRush/TestingTools")]
        public static void Open()
        {
            var window = GetWindow<TossRushTestWindow>();
            window.titleContent = new GUIContent("TossRush 測試工具");
            window.minSize = new Vector2(480, 600);
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
            stageActions = rootVisualElement.Q("stageActions");
            stageChoice = rootVisualElement.Q<DropdownField>("stageChoice");
            stageStatus = rootVisualElement.Q<Label>("stageStatus");
            rootVisualElement.Q<Button>("fixStage").clicked += () =>
            {
                if (StagePlaytestTools.SelectStage(stageChoice.index))
                    lastAction.text = "已固定測試：" + stageChoice.value;
                RefreshRecords();
            };
            rootVisualElement.Q<Button>("normalRotation").clicked += () =>
            {
                if (StagePlaytestTools.RestoreNormalRotation())
                {
                    stageChoice.index = 0;
                    lastAction.text = "已恢復正常關卡輪替。";
                }
                RefreshRecords();
            };
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
            var game = UnityEngine.Object.FindFirstObjectByType<GameDirector>();
            bool available = game && game.stages != null && game.stages.Length > 0;
            stageActions.SetEnabled(available && !EditorApplication.isCompiling);
            if (!available)
            {
                stageStatus.text = "請先開啟 TossRush 遊戲場景。";
                stageGame = null;
                return;
            }
            if (stageGame != game || stageChoice.choices.Count != game.stages.Length)
            {
                stageChoice.choices = game.stages.Select(StagePlaytestTools.StageLabel).ToList();
                stageChoice.index = Mathf.Clamp(game.startingStageIndex, 0, game.stages.Length - 1);
                stageGame = game;
            }
            stageStatus.text = game.repeatStartingStage
                ? "一般模式設定：固定「" + StagePlaytestTools.StageLabel(game.stages[Mathf.Clamp(game.startingStageIndex, 0, game.stages.Length - 1)]) + "」"
                : "一般模式設定：正常輪替";
        }
    }
}
