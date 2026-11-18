using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TossRush.Editor
{
    public static class StagePlaytestTools
    {
        public static string StageLabel(StageView stage)
        {
            if (!stage || !stage.data) return "（缺少關卡）";
            switch (stage.data.name)
            {
                case "Corner": return "房間";
                case "Street": return "街頭";
                case "Basketball": return "籃球";
                case "Carnival": return "園遊會（寶特瓶）";
                case "Doorstep": return "住家（信箱）";
                case "Darts": return "酒吧（飛鏢）";
                default: return stage.data.title;
            }
        }

        public static bool SelectStage(int index) => Configure(index, true);
        public static bool RestoreNormalRotation() => Configure(0, false);

        static bool Configure(int index, bool repeat)
        {
            var game = Object.FindFirstObjectByType<GameDirector>();
            if (!game)
            {
                Debug.LogWarning("請先開啟 Assets/TossRush/Scenes/TossRush.unity，再選擇測試關卡。");
                return false;
            }

            if (index < 0 || index >= game.stages.Length || !game.stages[index])
            {
                Debug.LogWarning("目前場景沒有選定的測試關卡。");
                return false;
            }

            Undo.RecordObject(game, "Select TossRush playtest stage");
            game.startingStageIndex = index;
            game.repeatStartingStage = repeat;
            Selection.activeGameObject = game.gameObject;
            if (Application.isPlaying) game.StartNormalRound();
            else
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(game);
                EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
            }
            Debug.Log(!repeat ? "已恢復正常關卡輪替。" : "已固定測試「" + game.stages[index].data.title + "」。按 Play 和 LET'S TOSS 開始；遊玩中切換會重新開始回合。");
            return true;
        }
    }
}
