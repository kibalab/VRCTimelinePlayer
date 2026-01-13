using UdonSharp;
using UnityEngine;

namespace K13A.TimelinePlayer
{
    public class TimelineTrackDatabase : UdonSharpBehaviour
    {
        public string[] timelineNames;
        public int[] timelineDepths;
        public float[] timelineDurations;
        public float[] timelineRootStarts;

        public int[] clipTimelineIndex;
        public string[] clipTrackName;
        public string[] clipTrackType;
        public string[] clipDisplayName;
        public string[] clipPlayableType;
        public float[] clipRootStart;
        public float[] clipRootEnd;
        public bool[] clipIsControl;
        public int[] clipChildTimelineIndex;
    }
}