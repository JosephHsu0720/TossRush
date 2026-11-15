using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TossRush.Editor
{
    public static class GameAudioSetup
    {
        public const string MusicPath = "Assets/TossRush/ThirdParty/SubspaceAudio/Juhani Junkala [Retro Game Music Pack] Level 1.wav";
        const string Sounds = "Assets/TossRush/ThirdParty/Kenney/impact-sounds/";
        const string Interface = "Assets/TossRush/ThirdParty/Kenney/interface-sounds/";

        [MenuItem("TossRush/Apply CC0 Game Audio")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before saving audio.");
            AssetDatabase.Refresh();
            var game = Object.FindFirstObjectByType<GameDirector>();
            if (!game) throw new System.InvalidOperationException("Open the TossRush game scene first.");
            if (AssetImporter.GetAtPath(MusicPath) is AudioImporter importer)
            {
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.CompressedInMemory;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = .7f;
                settings.preloadAudioData = true;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
            Undo.RecordObject(game, "Add game audio");
            var audio = game.GetComponent<GameAudio>();
            if (!audio) audio = Undo.AddComponent<GameAudio>(game.gameObject);
            Undo.RecordObject(audio, "Set CC0 clips");
            var music = game.transform.Find("Background Music");
            if (!music)
            {
                var child = new GameObject("Background Music");
                Undo.RegisterCreatedObjectUndo(child, "Create music source");
                child.transform.SetParent(game.transform, false);
                music = child.transform;
            }
            var source = music.GetComponent<AudioSource>();
            if (!source) source = Undo.AddComponent<AudioSource>(music.gameObject);
            Undo.RecordObject(source, "Configure music loop");
            audio.musicSource = source;
            audio.effectsSource = game.audioSource;
            audio.music = AssetDatabase.LoadAssetAtPath<AudioClip>(MusicPath);
            audio.impact = Clip("impactSoft_medium_001.ogg");
            audio.penalty = Clip("impactSoft_heavy_000.ogg");
            audio.start = AssetDatabase.LoadAssetAtPath<AudioClip>(Interface + "open_001.ogg");
            audio.finish = AssetDatabase.LoadAssetAtPath<AudioClip>(Interface + "close_001.ogg");
            audio.reward = AssetDatabase.LoadAssetAtPath<AudioClip>(Interface + "pluck_001.ogg");
            game.hitClip = AssetDatabase.LoadAssetAtPath<AudioClip>(Interface + "confirmation_001.ogg");
            game.throwClip = Clip("impactSoft_medium_000.ogg");
            audio.musicVolume = .12f;
            audio.effectsVolume = .8f;
            audio.duckedMusicVolume = .035f;
            audio.duckHoldSeconds = .16f;
            if (!audio.music || !audio.impact || !audio.penalty || !audio.start || !audio.finish || !audio.reward || !game.hitClip || !game.throwClip)
                throw new System.InvalidOperationException("Required licensed audio clip is missing.");
            source.clip = audio.music;
            source.playOnAwake = false;
            source.loop = true;
            source.volume = audio.musicVolume;
            source.spatialBlend = 0;
            game.gameAudio = audio;
            EditorUtility.SetDirty(game);
            EditorUtility.SetDirty(audio);
            EditorUtility.SetDirty(source);
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
            EditorSceneManager.SaveScene(game.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("CC0 audio saved: Level 1 BGM + throw, hit, collision, penalty, start and result effects.");
        }

        static AudioClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>(Sounds + name);
    }
}
