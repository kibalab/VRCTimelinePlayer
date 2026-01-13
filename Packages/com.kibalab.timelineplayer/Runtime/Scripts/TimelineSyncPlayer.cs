using UdonSharp;
using UnityEngine;
using UnityEngine.Playables;
using VRC.SDKBase;

namespace K13A.TimelinePlayer
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class TimelineSyncPlayer : UdonSharpBehaviour
    {
        [Header("Timeline")] public PlayableDirector director;
        [SerializeField] private DirectorWrapMode directorWrapMode = DirectorWrapMode.None;
        [SerializeField] private float framerate = 60f;

        [UdonSynced] private float syncBaseServerTime;
        [UdonSynced] private float syncBaseTimelineTime;
        [UdonSynced] private bool syncIsPlaying;
        [UdonSynced] private bool syncLoop;
        [UdonSynced] private int syncStateId;

        private float duration;
        private bool initialized;
        private bool externalControl;
        private int lastAppliedStateId = -1;

        private void Start()
        {
            InitializeIfNeeded();

            if (Networking.IsOwner(gameObject))
            {
                syncBaseServerTime = GetNetworkTime();
                syncBaseTimelineTime = 0f;
                syncIsPlaying = false;
                syncLoop = false;
                syncStateId++;
                RequestSerialization();
                ApplyLocalOwnerState();
            }
            else
            {
                ApplyRemoteState();
            }
        }

        private void InitializeIfNeeded()
        {
            if (initialized) return;
            if (director == null) return;

            duration = (float)director.duration;
            if (duration <= 0f) duration = 1f;

            director.extrapolationMode = directorWrapMode;
            director.timeUpdateMode = DirectorUpdateMode.DSPClock;

            initialized = true;
        }

        private float GetNetworkTime()
        {
            if (Networking.LocalPlayer == null) return Time.time;
            return (float)Networking.GetServerTimeInSeconds();
        }

        private void Update()
        {
            InitializeIfNeeded();
            if (!initialized || director == null) return;

            if (Networking.IsOwner(gameObject) && syncIsPlaying && !syncLoop)
            {
                if (director.time >= duration)
                {
                    syncIsPlaying = false;
                    syncBaseTimelineTime = Mathf.Clamp((float)director.time, 0f, duration);
                    syncBaseServerTime = GetNetworkTime();
                    syncStateId++;
                    RequestSerialization();

                    director.Pause();
                    director.time = syncBaseTimelineTime;
                    director.Evaluate();
                }
            }
        }

        public override void OnDeserialization()
        {
            InitializeIfNeeded();
            if (!initialized || director == null) return;
            if (Networking.IsOwner(gameObject)) return;
            if (syncStateId == lastAppliedStateId) return;

            lastAppliedStateId = syncStateId;
            ApplyRemoteState();
        }

        private float GetTimelineTimeFromSync()
        {
            float t = syncBaseTimelineTime;

            if (syncIsPlaying)
            {
                float elapsed = GetNetworkTime() - syncBaseServerTime;
                if (elapsed < 0f) elapsed = 0f;
                t += elapsed;
            }

            if (duration > 0f)
            {
                if (syncLoop)
                {
                    t = Mathf.Repeat(t, duration);
                }
                else
                {
                    t = Mathf.Clamp(t, 0f, duration);
                }
            }

            return t;
        }

        private void ApplyRemoteState()
        {
            float t = GetTimelineTimeFromSync();

            director.extrapolationMode = syncLoop ? DirectorWrapMode.Loop : directorWrapMode;
            director.time = t;
            director.Evaluate();

            if (syncIsPlaying && !externalControl)
            {
                director.Play();
            }
            else
            {
                director.Pause();
            }
        }

        private void ApplyLocalOwnerState()
        {
            float t = Mathf.Clamp((float)director.time, 0f, duration);

            if (!syncIsPlaying)
            {
                syncBaseTimelineTime = t;
                syncBaseServerTime = GetNetworkTime();
            }

            director.extrapolationMode = syncLoop ? DirectorWrapMode.Loop : directorWrapMode;

            if (syncIsPlaying && !externalControl)
            {
                director.Play();
            }
            else
            {
                director.Pause();
                director.time = syncBaseTimelineTime;
                director.Evaluate();
            }
        }

        private void TakeOwnership()
        {
            if (Networking.LocalPlayer == null) return;
            if (Networking.IsOwner(gameObject)) return;
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        public float GetDuration()
        {
            InitializeIfNeeded();
            return duration;
        }

        public float GetCurrentTime()
        {
            if (director == null) return 0f;
            return (float)director.time;
        }

        public bool GetIsPlaying()
        {
            return syncIsPlaying;
        }

        public bool GetIsLoop()
        {
            return syncLoop;
        }

        public float GetFramerate()
        {
            return framerate;
        }

        public void TogglePlayPause()
        {
            InitializeIfNeeded();
            if (!initialized) return;
            if (Networking.LocalPlayer == null) return;

            TakeOwnership();

            float currentTime = Mathf.Clamp((float)director.time, 0f, duration);
            float now = GetNetworkTime();

            if (syncIsPlaying)
            {
                syncIsPlaying = false;
                syncBaseTimelineTime = currentTime;
                syncBaseServerTime = now;
            }
            else
            {
                syncIsPlaying = true;
                syncBaseTimelineTime = currentTime;
                syncBaseServerTime = now;
            }

            syncStateId++;
            RequestSerialization();
            ApplyLocalOwnerState();
        }

        public void StopTimeline()
        {
            InitializeIfNeeded();
            if (!initialized) return;
            if (Networking.LocalPlayer == null) return;

            TakeOwnership();

            syncIsPlaying = false;
            syncBaseTimelineTime = 0f;
            syncBaseServerTime = GetNetworkTime();
            syncStateId++;

            RequestSerialization();

            director.Pause();
            director.time = 0f;
            director.Evaluate();
        }

        public void ToggleLoop()
        {
            InitializeIfNeeded();
            if (!initialized) return;
            if (Networking.LocalPlayer == null) return;

            TakeOwnership();

            syncLoop = !syncLoop;
            syncStateId++;
            RequestSerialization();

            director.extrapolationMode = syncLoop ? DirectorWrapMode.Loop : directorWrapMode;
        }

        public void Resync()
        {
            InitializeIfNeeded();
            if (!initialized) return;
            if (Networking.LocalPlayer == null) return;

            TakeOwnership();

            syncStateId++;
            RequestSerialization();
        }

        public void SeekFromUI(float newTime)
        {
            InitializeIfNeeded();
            if (!initialized) return;
            if (Networking.LocalPlayer == null) return;

            TakeOwnership();

            newTime = Mathf.Clamp(newTime, 0f, duration);

            float now = GetNetworkTime();
            syncBaseTimelineTime = newTime;
            syncBaseServerTime = now;
            syncStateId++;
            RequestSerialization();

            director.time = newTime;
            director.Evaluate();

            if (syncIsPlaying && !externalControl)
            {
                director.Play();
            }
            else
            {
                director.Pause();
            }
        }

        public void SetExternalControl(bool value)
        {
            externalControl = value;

            if (!initialized || director == null) return;
            if (!Networking.IsOwner(gameObject)) return;

            if (externalControl)
            {
                director.Pause();
                director.Evaluate();
            }
            else
            {
                if (syncIsPlaying)
                {
                    director.Play();
                }
                else
                {
                    director.Pause();
                }
            }
        }

        public void PreviewLocalTime(float newTime)
        {
            InitializeIfNeeded();
            if (!initialized || director == null) return;
            if (!Networking.IsOwner(gameObject)) return;

            newTime = Mathf.Clamp(newTime, 0f, duration);
            director.time = newTime;
            director.Evaluate();
        }
    }
}