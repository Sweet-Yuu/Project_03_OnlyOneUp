#if UNITY_5_3_OR_NEWER
using UnityEngine;
using System;
using System.Collections;

namespace Unity.Services.Vivox
{
    internal class VxUnityInterop : MonoBehaviour
    {
        private static object m_Lock = new object();
#if !UNITY_INCLUDE_TESTS
        private bool quitting = false;
#endif
        private static VxUnityInterop m_Instance;

#if UNITY_EDITOR
        // Resets Static members on runtime initialization
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStaticsOnLoad()
        {
            m_Lock = new object();
            m_Instance = null;
        }
#endif

        /// <summary>
        /// Access singleton instance through this propriety.
        /// </summary>
        public static VxUnityInterop Instance
        {
            get
            {
                lock (m_Lock)
                {
                    if (m_Instance == null)
                    {
                        // Search for existing instance.
                        m_Instance = FindAnyObjectByType<VxUnityInterop>();

                        // Create new instance if one doesn't already exist.
                        if (m_Instance == null)
                        {
                            // Need to create a new GameObject to attach the singleton to.
                            var singletonObject = new GameObject();
                            m_Instance = singletonObject.AddComponent<VxUnityInterop>();
                            singletonObject.name = typeof(VxUnityInterop).ToString() + " (Singleton)";
                        }
                    }
                    // Make instance persistent even if its already in the scene
                    DontDestroyOnLoad(m_Instance.gameObject);
                    return m_Instance;
                }
            }
        }

        void ApplicationQuitting()
        {
#if !UNITY_INCLUDE_TESTS
            quitting = true;
#endif
            if (Client.Instance.Initialized && !VxClient.Instance.IsQuitting)
            {
                VxClient.Instance.IsQuitting = true;
                Client.Instance.Uninitialize();
            }
            Application.quitting -= ApplicationQuitting;
        }

        // Setting up Unity Coroutine to run on the main thread
        public virtual void StartVivoxUnity()
        {
            StartCoroutine(VivoxUnityRun());
            Application.quitting += ApplicationQuitting;
        }

        // VOX-3552 (follow-up): Debug.LogException invokes Unity's stack-trace
        // extraction path (scripting_stack_trace_info_for), which on Unity
        // 6000.0 has an uninitialized-POD race that becomes a crash cluster
        // under load. Even Debug.LogWarning enters that path at the default
        // Warning stack-trace setting (ScriptOnly). LogOption.NoStacktrace
        // guarantees Unity skips stack extraction, closing the crash trigger.
        //
        // The log is also rate-limited independently of exception type or
        // message. Filtering on content is defeated by exceptions embedding
        // variable state (KeyNotFoundException, VivoxApiException), and a
        // persistent RunOnce failure at ~100 Hz would otherwise flood
        // Player.log and do synchronous main-thread I/O every frame even
        // with NoStacktrace. First failure logs immediately for support
        // visibility; subsequent failures within PumpErrorLogIntervalSeconds
        // are counted and reported in the next log.
        //
        // The prior fix (#630) added dedup + a consecutive-failure bail-out
        // and was reverted here: the dedup key was Type + Message so any
        // exception embedding variable state defeated it, and the bail-out
        // permanently disabled voice/text for the process -- a burst of
        // bad queued messages could trip it without a permanent condition,
        // and silent voice death is a worse failure than the intermittent
        // exceptions themselves.
        private const double PumpErrorLogIntervalSeconds = 60.0;

        private IEnumerator VivoxUnityRun()
        {
            var logClock = new System.Diagnostics.Stopwatch(); // stopped -> first failure logs immediately
            long suppressedSinceLastLog = 0;

            while (VxClient.Instance.Started)
            {
                try
                {
                    Client.RunOnce();
                }
                catch (Exception e)
                {
                    if (!logClock.IsRunning || logClock.Elapsed.TotalSeconds >= PumpErrorLogIntervalSeconds)
                    {
                        var suppressed = suppressedSinceLastLog;
                        suppressedSinceLastLog = 0;
                        logClock.Restart();

                        if (suppressed > 0)
                        {
                            Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, this,
                                "Vivox message pump caught {0}: {1} ({2} additional exceptions suppressed in the last {3}s)",
                                e.GetType().Name, e.Message, suppressed, PumpErrorLogIntervalSeconds);
                        }
                        else
                        {
                            Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, this,
                                "Vivox message pump caught {0}: {1}", e.GetType().Name, e.Message);
                        }
                    }
                    else
                    {
                        suppressedSinceLastLog++;
                    }
                }
                yield return new WaitForSecondsRealtime(0.01f);
            }
        }

        void OnDestroy()
        {
#if !UNITY_INCLUDE_TESTS
            if (!quitting)
            {
                var classType = GetType();
                Debug.LogWarning(classType.Namespace + " requrires " + classType.Name + " to communicate messages to and from "
                    + classType.Namespace + " Core. Deleting this object will prevent the " + classType.Namespace + " SDK from working.  " +
                    "If you would like to change it's implementation please override StartVivoxUnity method in "
                    + classType.Name);
            }
#endif
            Application.quitting -= ApplicationQuitting;
        }
    }
}
#endif
