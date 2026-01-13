using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace K13A.TimelinePlayer
{
    public class TimelineUIController : UdonSharpBehaviour
    {
        [Header("References")] public TimelineSyncPlayer player;

        [Header("Text")] public TextMeshProUGUI lengthLabel;
        public TextMeshProUGUI currentLabel;

        [Header("Slider")] public Slider timeSlider;

        [Header("Play / Pause Button")] public Image playPauseIcon;
        public Sprite playSprite;
        public Sprite pauseSprite;

        [Header("Loop Button")] public Image loopIcon;
        public Sprite loopOnSprite;
        public Sprite loopOffSprite;

        [Header("Display Mode Toggle")] public TextMeshProUGUI displayModeLabel;
        public bool defaultShowFrames = true;

        private bool showFrames;
        private bool sliderDragging;
        private bool sliderInitialized;

        private void Start()
        {
            showFrames = defaultShowFrames;
            UpdateDisplayModeLabel();
        }

        private void Update()
        {
            if (player == null) return;

            float duration = player.GetDuration();
            float current = player.GetCurrentTime();
            float framerate = player.GetFramerate();

            if (!sliderInitialized && timeSlider != null && duration > 0f)
            {
                timeSlider.minValue = 0f;
                timeSlider.maxValue = duration;
                timeSlider.value = 0f;
                sliderInitialized = true;
            }

            if (timeSlider != null && sliderInitialized && !sliderDragging)
            {
                timeSlider.value = Mathf.Clamp(current, 0f, duration);
            }

            if (sliderDragging && timeSlider != null)
            {
                float previewTime = Mathf.Clamp(timeSlider.value, 0f, duration);
                UpdateTexts(duration, previewTime, framerate);
            }
            else
            {
                UpdateTexts(duration, current, framerate);
            }

            UpdateIcons();
        }

        private void UpdateTexts(float duration, float current, float framerate)
        {
            if (lengthLabel != null)
            {
                if (showFrames)
                {
                    int totalFrames = Mathf.RoundToInt(duration * framerate);
                    lengthLabel.text = totalFrames.ToString() + " f";
                }
                else
                {
                    lengthLabel.text = duration.ToString("0.00");
                }
            }

            if (currentLabel != null)
            {
                if (showFrames)
                {
                    int curFrames = Mathf.RoundToInt(current * framerate);
                    currentLabel.text = curFrames.ToString();
                }
                else
                {
                    currentLabel.text = current.ToString("0.00");
                }
            }
        }

        private void UpdateIcons()
        {
            if (player == null) return;

            bool isPlaying = player.GetIsPlaying();
            bool isLoop = player.GetIsLoop();

            if (playPauseIcon != null)
            {
                if (isPlaying && pauseSprite != null)
                {
                    playPauseIcon.sprite = pauseSprite;
                }
                else if (!isPlaying && playSprite != null)
                {
                    playPauseIcon.sprite = playSprite;
                }
            }

            if (loopIcon != null)
            {
                if (isLoop && loopOnSprite != null)
                {
                    loopIcon.sprite = loopOnSprite;
                }
                else if (!isLoop && loopOffSprite != null)
                {
                    loopIcon.sprite = loopOffSprite;
                }
            }
        }

        private void UpdateDisplayModeLabel()
        {
            if (displayModeLabel == null) return;
            displayModeLabel.text = showFrames ? "FRAME" : "SEC";
        }

        public void OnClick_PlayPause()
        {
            if (player == null) return;
            player.TogglePlayPause();
        }

        public void OnClick_Stop()
        {
            if (player == null) return;
            player.StopTimeline();
        }

        public void OnClick_Loop()
        {
            if (player == null) return;
            player.ToggleLoop();
        }

        public void OnClick_Resync()
        {
            if (player == null) return;
            player.Resync();
        }

        public void OnClick_ToggleDisplayMode()
        {
            showFrames = !showFrames;
            UpdateDisplayModeLabel();
        }

        public void OnSliderPointerDown()
        {
            if (player == null || timeSlider == null) return;
            sliderDragging = true;
            player.SetExternalControl(true);
        }

        public void OnSliderPointerUp()
        {
            if (player == null || timeSlider == null) return;
            sliderDragging = false;

            float t = timeSlider.value;
            player.SeekFromUI(t);
            player.SetExternalControl(false);
        }

        public void OnSliderValueChanged()
        {
            if (!sliderDragging || player == null || timeSlider == null) return;

            float t = timeSlider.value;
            player.PreviewLocalTime(t);
        }
    }
}