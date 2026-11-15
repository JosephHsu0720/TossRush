using UnityEngine;
using UnityEngine.EventSystems;

namespace TossRush
{
    public sealed class MenuVolumeControl : MonoBehaviour, IEndDragHandler
    {
        public UnityEngine.UI.Slider slider;
        public UnityEngine.UI.Button speakerButton;
        public SpeakerIcon speakerIcon;
        public const string VolumeKey = "TossRush.Audio.Master.v1";
        const string UnmutedKey = "TossRush.Audio.Unmuted.v1";
        float unmutedVolume = 1;
        bool initialized;

        void Awake()
        {
            unmutedVolume = Mathf.Clamp(PlayerPrefs.GetFloat(UnmutedKey, 1), .01f, 1);
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.SetValueWithoutNotify(Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1)));
            slider.onValueChanged.AddListener(SetVolume);
            speakerButton.onClick.AddListener(ToggleMute);
            initialized = true;
            SetVolume(slider.value);
        }

        void SetVolume(float value)
        {
            value = Mathf.Clamp01(value);
            AudioListener.volume = value;
            if (value > .0001f) unmutedVolume = value;
            speakerIcon.SetMuted(value <= .0001f);
            PlayerPrefs.SetFloat(VolumeKey, value);
            PlayerPrefs.SetFloat(UnmutedKey, unmutedVolume);
        }

        void ToggleMute()
        {
            slider.value = slider.value > .0001f ? 0 : unmutedVolume;
            PlayerPrefs.Save();
        }

        public void OnEndDrag(PointerEventData eventData) => PlayerPrefs.Save();
        void OnDisable() { if (initialized) PlayerPrefs.Save(); }
        void OnApplicationPause(bool paused) { if (paused && initialized) PlayerPrefs.Save(); }
        void OnApplicationQuit() { if (initialized) PlayerPrefs.Save(); }
        void OnDestroy()
        {
            if (!initialized) return;
            if (slider) slider.onValueChanged.RemoveListener(SetVolume);
            if (speakerButton) speakerButton.onClick.RemoveListener(ToggleMute);
        }
    }
}
