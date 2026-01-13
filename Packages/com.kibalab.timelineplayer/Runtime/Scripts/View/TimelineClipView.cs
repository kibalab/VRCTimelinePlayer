using UdonSharp;
using UnityEngine;
using TMPro;

namespace K13A.TimelinePlayer
{
    public class TimelineClipView : UdonSharpBehaviour
    {
        public RectTransform rectTransform;
        public TextMeshProUGUI label;

        public void Setup(string text, float start, float end, float totalDuration)
        {
            if (rectTransform == null) rectTransform = (RectTransform)transform;

            if (totalDuration <= 0f) totalDuration = 0.0001f;

            float length = end - start;
            if (length < 0f) length = 0f;

            float normStart = start / totalDuration;
            float normEnd = end / totalDuration;

            if (normStart < 0f) normStart = 0f;
            if (normEnd > 1f) normEnd = 1f;
            if (normEnd < normStart) normEnd = normStart;

            Vector2 anchorMin = rectTransform.anchorMin;
            Vector2 anchorMax = rectTransform.anchorMax;
            anchorMin.x = normStart;
            anchorMax.x = normEnd;
            anchorMin.y = 0f;
            anchorMax.y = 1f;
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;

            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;

            if (label != null) label.text = text;
            gameObject.SetActive(true);
        }

        public void Clear()
        {
            gameObject.SetActive(false);
        }
    }
}