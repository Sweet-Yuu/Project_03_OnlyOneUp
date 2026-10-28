using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Unity.Services.Vivox
{
    class VivoxLogger
    {
        const string k_Tag = "[Vivox]";
        const string k_VerboseLoggingDefine = "ENABLE_UNITY_VIVOX_VERBOSE_LOGGING";

        public static void Log(object message) => Debug.unityLogger.Log(k_Tag, message);
        public static void LogWarning(object message) => Debug.unityLogger.LogWarning(k_Tag, message);
        public static void LogError(object message) => Debug.unityLogger.LogError(k_Tag, message);
        public static void LogException(Exception exception) => Debug.unityLogger.Log(LogType.Exception, k_Tag, exception);
        // Vivox Exception is currently just logged as an Error
        // This should be revisited when we improve our Exception handling and create Exception types
        public static void LogVxException(object message)
        {
            string callerMethodName = new StackFrame(1).GetMethod().Name;
            string newMessage = $"{callerMethodName}: {message}";

            // While teardown is faulting the requests that were still in flight, their failures are expected
            // (the SDK is being uninitialized), so log them at verbose rather than spamming errors. This is
            // scoped to that fault loop only, so unrelated failures are still surfaced as errors.
            if (VxClient.Instance.SuppressingTeardownRequestFailures)
            {
                LogVerbose(newMessage);
                return;
            }

            LogError(newMessage);
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void LogAssertion(object message) => Debug.unityLogger.Log(LogType.Assert, k_Tag, message);

#if !ENABLE_UNITY_SERVICES_VERBOSE_LOGGING
        [Conditional(k_VerboseLoggingDefine)]
#endif
        public static void LogVerbose(object message) => Debug.unityLogger.Log(k_Tag, message);
    }
}
