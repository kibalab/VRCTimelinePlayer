using UdonSharp;
using UnityEngine;
using TMPro;

namespace K13A.TimelinePlayer
{
    public class TimelineTrackView : UdonSharpBehaviour
    {
        public RectTransform trackRectTransform;
        public RectTransform clipsRoot;
        public TextMeshProUGUI trackNameLabel;
        public GameObject clipPrefab;

        public float trackHeight = 24f;

        private TimelineTrackDatabase db;
        private int timelineIndex;
        private string trackName;
        private bool built;

        public void BuildTrack(
            TimelineTrackDatabase database,
            int tlIndex,
            string tName,
            float timelineDuration,
            float rootStartOffset)
        {
            db = database;
            timelineIndex = tlIndex;
            trackName = tName;

            if (trackRectTransform == null)
                trackRectTransform = (RectTransform)transform;

            if (trackNameLabel != null)
                trackNameLabel.text = trackName;

            Vector2 size = trackRectTransform.sizeDelta;
            size.y = trackHeight;
            trackRectTransform.sizeDelta = size;

            if (db == null || clipPrefab == null || clipsRoot == null) return;
            if (db.clipTimelineIndex == null) return;

            int clipTotal = db.clipTimelineIndex.Length;
            int countForTrack = 0;
            int i;

            for (i = 0; i < clipTotal; i++)
            {
                if (db.clipTimelineIndex[i] != timelineIndex) continue;
                string tn = db.clipTrackName[i];
                if (tn == null) tn = "";
                if (tn != trackName) continue;
                countForTrack++;
            }

            if (countForTrack <= 0)
            {
                built = true;
                return;
            }

            int[] clipIndices = new int[countForTrack];
            int idx = 0;
            for (i = 0; i < clipTotal; i++)
            {
                if (db.clipTimelineIndex[i] != timelineIndex) continue;
                string tn = db.clipTrackName[i];
                if (tn == null) tn = "";
                if (tn != trackName) continue;
                clipIndices[idx] = i;
                idx++;
            }

            for (i = 0; i < countForTrack; i++)
            {
                int clipIndex = clipIndices[i];

                GameObject go = (GameObject)Instantiate(clipPrefab, clipsRoot);
                TimelineClipView view = go.GetComponent<TimelineClipView>();
                if (view == null) continue;

                float rootStart = db.clipRootStart[clipIndex];
                float rootEnd = db.clipRootEnd[clipIndex];

                float localStart = rootStart - rootStartOffset;
                float localEnd = rootEnd - rootStartOffset;

                if (localEnd < 0f) continue;
                if (localStart > timelineDuration) continue;

                if (localStart < 0f) localStart = 0f;
                if (localEnd > timelineDuration) localEnd = timelineDuration;

                string label = db.clipDisplayName[clipIndex];
                if (string.IsNullOrEmpty(label)) label = db.clipPlayableType[clipIndex];

                view.Setup(label, localStart, localEnd, timelineDuration);
            }

            built = true;
        }
    }
}