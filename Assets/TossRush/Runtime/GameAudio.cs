using UnityEngine;

namespace TossRush
{
    // Separate sources keep scoring and collisions from restarting the music.
    public sealed class GameAudio : MonoBehaviour
    {
        public AudioSource musicSource, effectsSource;
        public AudioClip music, impact, penalty, start, finish, reward;
        [Tooltip("Optional gameplay sounds, currently disabled. Start, goal and result sounds remain enabled.")]
        public bool playThrowSounds, playCollisionSounds, playPenaltySounds, playRewardSounds;
        [Range(0, 1)] public float musicVolume = .12f;
        [Range(0, 1)] public float effectsVolume = .8f;
        [Range(0, 1)] public float duckedMusicVolume = .035f;
        [Min(0)] public float duckHoldSeconds = .16f;
        float nextImpact, duckUntil;

        void OnEnable()
        {
            if (!musicSource || !music) return;
            musicSource.clip = music;
            musicSource.loop = true;
            musicSource.spatialBlend = 0;
            musicSource.volume = musicVolume;
            if (!musicSource.isPlaying) musicSource.Play();
        }

        public void Play(AudioClip clip, float volume = 1)
        {
            if (!effectsSource || !clip) return;
            effectsSource.PlayOneShot(clip, effectsVolume * volume);
            // Leave a short gap in the busy chiptune mix for the quieter soft impacts.
            duckUntil = Time.unscaledTime + Mathf.Max(duckHoldSeconds, Mathf.Min(clip.length, .55f));
            if (musicSource) musicSource.volume = Mathf.Min(musicVolume, duckedMusicVolume);
        }

        void Update()
        {
            if (!musicSource) return;
            float target = Time.unscaledTime < duckUntil ? Mathf.Min(musicVolume, duckedMusicVolume) : musicVolume;
            musicSource.volume = Mathf.MoveTowards(musicSource.volume, target, Time.unscaledDeltaTime * Mathf.Max(.01f, musicVolume) / .25f);
        }

        public void Impact(float speed)
        {
            if (!playCollisionSounds || speed < .35f || Time.unscaledTime < nextImpact) return;
            nextImpact = Time.unscaledTime + .075f;
            Play(impact, Mathf.Lerp(.5f, 1f, Mathf.Clamp01(speed / 10)));
        }

        void OnDisable() { if (musicSource) musicSource.Stop(); }
    }
}
