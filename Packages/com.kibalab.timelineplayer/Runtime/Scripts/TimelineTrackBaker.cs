using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using K13A.TimelinePlayer;

[ExecuteAlways]
[DisallowMultipleComponent]
public class TimelineTrackBaker : MonoBehaviour
{
    public PlayableDirector director;
    public TimelineTrackDatabase targetDb;

    [System.Serializable]
    private class TimelineRecord
    {
        public string name;
        public int depth;
        public float duration;
        public float rootStart;
    }

    [System.Serializable]
    private class ClipRecord
    {
        public int timelineIndex;
        public string trackName;
        public string trackType;
        public string clipName;
        public string playableType;
        public float rootStart;
        public float rootEnd;
        public bool isControl;
        public int childTimelineIndex;
    }

    private void OnValidate()
    {
        if (!director || !targetDb) return;
        BakeNow();
    }

    public void BakeNow()
    {
        var rootAsset = director.playableAsset as TimelineAsset;
        if (rootAsset == null)
        {
            Debug.LogWarning("[TimelineTrackBaker] PlayableDirector에 TimelineAsset이 없음", this);
            return;
        }

        var timelines = new List<TimelineRecord>();
        var clips = new List<ClipRecord>();

        BakeTimelineRecursive(rootAsset, 0, 0f, timelines, clips);
        ApplyToDatabase(timelines, clips);

        Debug.Log($"[TimelineTrackBaker] Baked timelines={timelines.Count}, clips={clips.Count}", this);
    }

    private int BakeTimelineRecursive(
        TimelineAsset asset,
        int depth,
        float rootTimeOffset,
        List<TimelineRecord> timelines,
        List<ClipRecord> clips)
    {
        int timelineIndex = timelines.Count;

        var tr = new TimelineRecord();
        tr.name = asset.name;
        tr.depth = depth;
        tr.duration = (float)asset.duration;
        tr.rootStart = rootTimeOffset;
        timelines.Add(tr);

        foreach (var track in asset.GetRootTracks())
        {
            BakeTrackRecursive(track, timelineIndex, depth, rootTimeOffset, timelines, clips);
        }

        return timelineIndex;
    }

    private void BakeTrackRecursive(
        TrackAsset track,
        int timelineIndex,
        int depth,
        float rootTimeOffset,
        List<TimelineRecord> timelines,
        List<ClipRecord> clips)
    {
        bool hasClipAssets = false;

        foreach (var clip in track.GetClips())
        {
            hasClipAssets = true;

            float localStart = (float)clip.start;
            float localEnd = (float)(clip.start + clip.duration);
            float rootStart = rootTimeOffset + localStart;
            float rootEnd = rootTimeOffset + localEnd;

            var rec = new ClipRecord();
            rec.timelineIndex = timelineIndex;
            rec.trackName = track.name;
            rec.trackType = track.GetType().Name;
            rec.clipName = clip.displayName;
            rec.playableType = clip.asset != null ? clip.asset.GetType().Name : string.Empty;
            rec.rootStart = rootStart;
            rec.rootEnd = rootEnd;
            rec.isControl = false;
            rec.childTimelineIndex = -1;

            var controlPlayable = clip.asset as ControlPlayableAsset;
            if (controlPlayable != null)
            {
                rec.isControl = true;

                GameObject sourceGO = controlPlayable.sourceGameObject.Resolve(director);
                if (sourceGO != null)
                {
                    var childDirector = sourceGO.GetComponent<PlayableDirector>();
                    if (childDirector != null && childDirector.playableAsset is TimelineAsset childAsset)
                    {
                        float childRootOffset = rootStart;

                        int childIndex = BakeTimelineRecursive(
                            childAsset,
                            depth + 1,
                            childRootOffset,
                            timelines,
                            clips);

                        rec.childTimelineIndex = childIndex;
                    }
                }
            }

            clips.Add(rec);
        }

        var animTrack = track as AnimationTrack;
        if (animTrack != null && !hasClipAssets && animTrack.infiniteClip != null)
        {
            float localStart = 0f;
            float localEnd = (float)(track.timelineAsset != null ? track.timelineAsset.duration : 0f);
            if (localEnd <= 0f) localEnd = 0.0001f;

            float rootStart = rootTimeOffset + localStart;
            float rootEnd = rootTimeOffset + localEnd;

            var rec = new ClipRecord();
            rec.timelineIndex = timelineIndex;
            rec.trackName = track.name;
            rec.trackType = track.GetType().Name;
            rec.clipName = string.IsNullOrEmpty(animTrack.name) ? track.name : animTrack.name;
            rec.playableType = "InfiniteAnimation";
            rec.rootStart = rootStart;
            rec.rootEnd = rootEnd;
            rec.isControl = false;
            rec.childTimelineIndex = -1;

            clips.Add(rec);
        }

        foreach (var subTrack in track.GetChildTracks())
        {
            BakeTrackRecursive(subTrack, timelineIndex, depth, rootTimeOffset, timelines, clips);
        }
    }

    private void ApplyToDatabase(List<TimelineRecord> timelines, List<ClipRecord> clips)
    {
        if (targetDb == null) return;

        int tlCount = timelines.Count;
        int clCount = clips.Count;

        targetDb.timelineNames = new string[tlCount];
        targetDb.timelineDepths = new int[tlCount];
        targetDb.timelineDurations = new float[tlCount];
        targetDb.timelineRootStarts = new float[tlCount];

        for (int i = 0; i < tlCount; i++)
        {
            var t = timelines[i];
            targetDb.timelineNames[i] = t.name;
            targetDb.timelineDepths[i] = t.depth;
            targetDb.timelineDurations[i] = t.duration;
            targetDb.timelineRootStarts[i] = t.rootStart;
        }

        targetDb.clipTimelineIndex = new int[clCount];
        targetDb.clipTrackName = new string[clCount];
        targetDb.clipTrackType = new string[clCount];
        targetDb.clipDisplayName = new string[clCount];
        targetDb.clipPlayableType = new string[clCount];
        targetDb.clipRootStart = new float[clCount];
        targetDb.clipRootEnd = new float[clCount];
        targetDb.clipIsControl = new bool[clCount];
        targetDb.clipChildTimelineIndex = new int[clCount];

        for (int i = 0; i < clCount; i++)
        {
            var c = clips[i];
            targetDb.clipTimelineIndex[i] = c.timelineIndex;
            targetDb.clipTrackName[i] = c.trackName;
            targetDb.clipTrackType[i] = c.trackType;
            targetDb.clipDisplayName[i] = c.clipName;
            targetDb.clipPlayableType[i] = c.playableType;
            targetDb.clipRootStart[i] = c.rootStart;
            targetDb.clipRootEnd[i] = c.rootEnd;
            targetDb.clipIsControl[i] = c.isControl;
            targetDb.clipChildTimelineIndex[i] = c.childTimelineIndex;
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(targetDb);
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}