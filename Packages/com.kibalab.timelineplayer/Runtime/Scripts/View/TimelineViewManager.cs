using UdonSharp;
using UnityEngine;
using UnityEngine.Playables;

namespace K13A.TimelinePlayer
{
    public class TimelineViewManager : UdonSharpBehaviour
    {
        public PlayableDirector rootDirector;
        public TimelineTrackDatabase db;

        public RectTransform viewsRoot;
        public GameObject timelineViewPrefab;
        public float trackHeight = 24f;

        private TimelineView[] timelineViews;
        private int[] activeList;
        private bool[] activeFlags;
        private int timelineCount;
        private bool initialized;

        private void Start()
        {
            InitializeViews();
        }

        private void InitializeViews()
        {
            if (initialized) return;
            if (db == null || timelineViewPrefab == null || viewsRoot == null) return;
            if (db.timelineNames == null) return;

            timelineCount = db.timelineNames.Length;
            if (timelineCount <= 0) return;

            timelineViews = new TimelineView[timelineCount];
            activeList = new int[timelineCount];
            activeFlags = new bool[timelineCount];

            for (int i = 0; i < timelineCount; i++)
            {
                GameObject go = (GameObject)Instantiate(timelineViewPrefab, viewsRoot);
                var view = go.GetComponent<TimelineView>();
                if (view == null) continue;

                view.db = db;
                view.rootDirector = rootDirector;
                view.timelineIndex = i;
                view.trackHeight = trackHeight;
                view.BuildTimeline();

                go.SetActive(false);
                timelineViews[i] = view;
            }

            initialized = true;
        }

        private void Update()
        {
            if (!initialized) return;
            if (rootDirector == null || db == null) return;
            if (timelineViews == null || timelineViews.Length == 0) return;

            float rootTime = (float)rootDirector.time;

            int activeCount = BuildActiveTimelineList(rootTime);

            for (int i = 0; i < timelineViews.Length; i++)
            {
                var v = timelineViews[i];
                if (v == null) continue;
                v.gameObject.SetActive(false);
            }

            for (int i = 0; i < activeCount; i++)
            {
                int tlIndex = activeList[i];
                if (tlIndex < 0 || tlIndex >= timelineViews.Length) continue;

                var v = timelineViews[tlIndex];
                if (v == null) continue;

                v.gameObject.SetActive(true);
                v.transform.SetSiblingIndex(i);
            }
        }

        private int BuildActiveTimelineList(float rootTime)
        {
            if (timelineCount <= 0) return 0;

            for (int i = 0; i < timelineCount; i++)
            {
                activeFlags[i] = false;
            }

            activeFlags[0] = true;

            if (db.clipTimelineIndex != null &&
                db.clipIsControl != null &&
                db.clipChildTimelineIndex != null)
            {
                int clipCount = db.clipTimelineIndex.Length;
                for (int i = 0; i < clipCount; i++)
                {
                    if (!db.clipIsControl[i]) continue;

                    int child = db.clipChildTimelineIndex[i];
                    if (child < 0 || child >= timelineCount) continue;

                    float s = db.clipRootStart[i];
                    float e = db.clipRootEnd[i];
                    if (rootTime < s || rootTime >= e) continue;

                    activeFlags[child] = true;
                }
            }

            int count = 0;

            if (db.timelineDepths != null && db.timelineDepths.Length == timelineCount)
            {
                int maxDepth = 0;
                for (int i = 0; i < timelineCount; i++)
                {
                    int d = db.timelineDepths[i];
                    if (d > maxDepth) maxDepth = d;
                }

                for (int depth = 0; depth <= maxDepth; depth++)
                {
                    for (int i = 0; i < timelineCount; i++)
                    {
                        if (!activeFlags[i]) continue;
                        if (db.timelineDepths[i] != depth) continue;
                        activeList[count] = i;
                        count++;
                    }
                }
            }
            else
            {
                for (int i = 0; i < timelineCount; i++)
                {
                    if (!activeFlags[i]) continue;
                    activeList[count] = i;
                    count++;
                }
            }

            return count;
        }
    }
}