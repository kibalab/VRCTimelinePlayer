using UdonSharp;
using UnityEngine;
using UnityEngine.Playables;

namespace K13A.TimelinePlayer
{
    public class TimelineView : UdonSharpBehaviour
    {
        public PlayableDirector rootDirector;
        public TimelineTrackDatabase db;

        public RectTransform tracksRoot;
        public RectTransform rootPanelRect;
        public GameObject trackViewPrefab;

        public RectTransform currentBar;

        public RectTransform currentBarArea;

        public float trackHeight = 24f;
        public int timelineIndex = 0;

        private TimelineTrackView[] trackViews;
        private bool built;

        private float _duration;
        private float _rootStart;

        private void Start()
        {
            BuildTimeline();
        }

        private void Update()
        {
            UpdateCurrentBar();
        }

        public void BuildTimeline()
        {
            if (built) return;
            if (db == null || tracksRoot == null || trackViewPrefab == null) return;
            if (db.timelineNames == null || db.timelineDurations == null) return;
            if (timelineIndex < 0 || timelineIndex >= db.timelineNames.Length) return;
            if (db.clipTimelineIndex == null || db.clipTrackName == null) return;

            _duration = db.timelineDurations[timelineIndex];
            if (_duration <= 0f) _duration = 0.001f;

            _rootStart = 0f;
            if (db.timelineRootStarts != null && timelineIndex < db.timelineRootStarts.Length)
            {
                _rootStart = db.timelineRootStarts[timelineIndex];
            }

            int clipTotal = db.clipTimelineIndex.Length;
            string[] uniqueTrackNames = new string[clipTotal];
            int trackCount = 0;
            int i;

            for (i = 0; i < clipTotal; i++)
            {
                if (db.clipTimelineIndex[i] != timelineIndex) continue;

                string tName = db.clipTrackName[i];
                if (tName == null) tName = "";
                if (tName == "") continue;

                bool exists = false;
                int j;
                for (j = 0; j < trackCount; j++)
                {
                    if (uniqueTrackNames[j] == tName)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    uniqueTrackNames[trackCount] = tName;
                    trackCount++;
                }
            }

            if (trackCount <= 0)
            {
                FitHeight(0);
                built = true;
                return;
            }

            trackViews = new TimelineTrackView[trackCount];

            for (i = 0; i < trackCount; i++)
            {
                GameObject go = (GameObject)Instantiate(trackViewPrefab, tracksRoot);
                TimelineTrackView view = go.GetComponent<TimelineTrackView>();
                if (view == null) continue;

                view.trackHeight = trackHeight;

                string tName = uniqueTrackNames[i];
                view.BuildTrack(db, timelineIndex, tName, _duration, _rootStart);

                trackViews[i] = view;
                go.SetActive(true);
            }

            FitHeight(trackCount);
            built = true;
        }

        private void FitHeight(int trackCount)
        {
            float totalHeight = trackCount * trackHeight;
            if (totalHeight < 1f) totalHeight = trackHeight;

            RectTransform rootRect = rootPanelRect;
            if (rootRect == null) rootRect = (RectTransform)transform;

            Vector2 rootSize = rootRect.sizeDelta;
            rootSize.y = totalHeight;
            rootRect.sizeDelta = rootSize;

            Vector2 contentSize = tracksRoot.sizeDelta;
            contentSize.y = totalHeight;
            tracksRoot.sizeDelta = contentSize;
        }

        private void UpdateCurrentBar()
        {
            if (currentBar == null) return;
            if (!built) return;
            if (rootDirector == null) return;
            if (_duration <= 0f) return;

            float rootTime = (float)rootDirector.time;

            float localTime = rootTime - _rootStart;
            if (localTime < 0f) localTime = 0f;
            if (localTime > _duration) localTime = _duration;

            float t = localTime / _duration;
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;

            RectTransform area = currentBarArea;
            if (area == null) area = tracksRoot;
            if (area == null) return;

            Vector2 areaAnchorMin = area.anchorMin;
            Vector2 areaAnchorMax = area.anchorMax;

            float leftAnchor = areaAnchorMin.x;
            float rightAnchor = areaAnchorMax.x;
            float barAnchorX = leftAnchor + (rightAnchor - leftAnchor) * t;

            Vector2 cbAnchorMin = currentBar.anchorMin;
            Vector2 cbAnchorMax = currentBar.anchorMax;
            cbAnchorMin.x = barAnchorX;
            cbAnchorMax.x = barAnchorX;
            currentBar.anchorMin = cbAnchorMin;
            currentBar.anchorMax = cbAnchorMax;

            Vector2 pivot = currentBar.pivot;
            pivot.x = 0.5f;
            currentBar.pivot = pivot;

            Vector2 pos = currentBar.anchoredPosition;
            pos.x = 0f;
            currentBar.anchoredPosition = pos;
        }
    }
}