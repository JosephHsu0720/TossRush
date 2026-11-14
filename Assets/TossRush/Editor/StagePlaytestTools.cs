using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TossRush.Editor
{
    public static class StagePlaytestTools
    {
        const string Menu = "TossRush/固定關卡測試/";

        [MenuItem(Menu + "房間")]
        static void Room() => SelectStage("Corner");
        [MenuItem(Menu + "街頭")]
        static void Street() => SelectStage("Street");
        [MenuItem(Menu + "籃球")]
        static void Basketball() => SelectStage("Basketball");
        [MenuItem(Menu + "園遊會（寶特瓶）")]
        static void Carnival() => SelectStage("Carnival");
        [MenuItem(Menu + "住家（信箱）")]
        static void Doorstep() => SelectStage("Doorstep");
        [MenuItem(Menu + "恢復正常輪替", false, 20)]
        static void NormalRotation() => SelectStage(null);

        static void SelectStage(string stageName)
        {
            var game = Object.FindFirstObjectByType<GameDirector>();
            if (!game)
            {
                Debug.LogWarning("請先開啟 Assets/TossRush/Scenes/TossRush.unity，再選擇測試關卡。");
                return;
            }

            int index = 0;
            if (stageName != null)
            {
                index = System.Array.FindIndex(game.stages, stage => stage && stage.data && stage.data.name == stageName);
                if (index < 0)
                {
                    Debug.LogWarning("目前場景沒有這個關卡：" + stageName);
                    return;
                }
            }

            Undo.RecordObject(game, "Select TossRush playtest stage");
            game.startingStageIndex = index;
            game.repeatStartingStage = stageName != null;
            Selection.activeGameObject = game.gameObject;
            if (Application.isPlaying) game.StartRound();
            else
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(game);
                EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
            }
            Debug.Log(stageName == null ? "已恢復正常關卡輪替。" : "已固定測試「" + game.stages[index].data.title + "」。按 Play 和 LET'S TOSS 開始；遊玩中切換會重新開始回合。");
        }
    }
}
