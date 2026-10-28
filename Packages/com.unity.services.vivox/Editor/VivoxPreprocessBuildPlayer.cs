#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Unity.Services.Vivox.Editor;
using UnityEngine;

class VivoxPreprocessBuildPlayer : IPreprocessBuildWithReport
{
    string vivoxSampleDirectory = Application.dataPath + "/Vivox/Examples/ChatChannelSample";
    string vivoxAudioDirectory = Application.dataPath + "/Vivox/Examples/ChatChannelSample/Audio";
    string vivoxStreamingAssetsPath = Application.dataPath + "/StreamingAssets/VivoxAssets";
    string vivoxAudioFile = "VivoxAudioForInjection.wav";

    public int callbackOrder { get { return 0; } }

    public void OnPreprocessBuild(BuildReport report)
    {
#if UNITY_EDITOR_OSX && (UNITY_IOS || UNITY_VISIONOS)
        CheckMicDescription();
#endif
        StreamingAssetsSetup();
#if AUTH_PACKAGE_PRESENT
        EditorGameServiceAnalyticsSender.SendVivoxBuildWithAuthenticationEvent();
#endif
    }

    void StreamingAssetsSetup()
    {
        if (Directory.Exists(vivoxAudioDirectory))
        {
            if (!Directory.Exists(vivoxStreamingAssetsPath))
            {
                Directory.CreateDirectory(vivoxStreamingAssetsPath);
                File.Copy(vivoxAudioDirectory + "/" + vivoxAudioFile, vivoxStreamingAssetsPath + "/" + vivoxAudioFile);
            }
            else
            {
                if (!File.Exists(vivoxStreamingAssetsPath + "/" + vivoxAudioFile))
                {
                    File.Copy(vivoxAudioDirectory + "/" + vivoxAudioFile, vivoxStreamingAssetsPath + "/" + vivoxAudioFile);
                }
            }
        }
    }

#if UNITY_EDITOR_OSX && (UNITY_IOS || UNITY_VISIONOS)
    void CheckMicDescription()
    {
        if (string.IsNullOrEmpty(PlayerSettings.iOS.microphoneUsageDescription))
        {
            Debug.LogWarning(
                "Vivox voice chat requires microphone access. If your application uses voice chat, " +
                "you must set a Microphone Usage Description in Player Settings > Other Settings > " +
                "Microphone Usage Description. Without this, the app will be rejected by App Store review. " +
                "If you are only using Vivox for text chat, you can ignore this warning.");
        }
    }
#endif
}
#endif
