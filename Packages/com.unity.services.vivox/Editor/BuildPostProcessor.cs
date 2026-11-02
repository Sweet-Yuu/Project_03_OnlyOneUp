using UnityEditor;
using UnityEditor.Callbacks;
#if PLATFORM_IOS || PLATFORM_VISIONOS
using System.IO;
using UnityEditor.iOS.Xcode;
#endif

internal class BuildPostProcessor
{
    [PostProcessBuildAttribute(1)]
    internal static void OnPostProcessBuild(BuildTarget target, string path)
    {
#if PLATFORM_IOS
        if (target == BuildTarget.iOS)
        {
            PostProcessIosBuild(path);
        }
#elif PLATFORM_VISIONOS
        if (target == BuildTarget.VisionOS)
        {
            PostProcessVisionOsBuild(path);
        }
#endif
    }

    internal static void PostProcessIosBuild(string path)
    {
#if PLATFORM_IOS
        string simulatorSuffix = PlayerSettings.iOS.sdkVersion == iOSSdkVersion.SimulatorSDK ? "-simulator" : "";
        string vivoxNativeLib = $"libVivoxNative{simulatorSuffix}.a";
        string sdkLibName = $"libvivoxsdk{simulatorSuffix}.a";
        string staticLibrarySrcPath = $"Packages/com.unity.services.vivox/Plugins/iOS/";
        string relativeLibPathInXcodeProj = $"Libraries/com.unity.services.vivox/Plugins/iOS/";

        PBXProject project = new PBXProject();
        string sPath = PBXProject.GetPBXProjectPath(path);
        project.ReadFromFile(sPath);

        string vivoxNativeLibSrcPath = $"{staticLibrarySrcPath}{vivoxNativeLib}";
        string vivoxNativeLibPathInXcodeProj = $"{relativeLibPathInXcodeProj}{vivoxNativeLib}";
        AddFileToXcodeProject(project, path, vivoxNativeLibSrcPath, vivoxNativeLibPathInXcodeProj);

        string sdkLibSrcPath = $"{staticLibrarySrcPath}{sdkLibName}";
        string sdkLibPathInXcodeProj = $"{relativeLibPathInXcodeProj}{sdkLibName}";
        AddFileToXcodeProject(project, path, sdkLibSrcPath, sdkLibPathInXcodeProj);

        // Since we don't ask Unity to add our libraries, we need to add the search path manually
        string unityFrameworkTarget = project.GetUnityFrameworkTargetGuid();
        project.AddBuildProperty(unityFrameworkTarget, "LIBRARY_SEARCH_PATHS", "$(PROJECT_DIR)/" + relativeLibPathInXcodeProj);

        string projectContents = project.WriteToString();
        if (simulatorSuffix.Length > 0)
        {
            projectContents = ReplaceSimulatorAllLoadWithForceLoad(projectContents);
        }
        File.WriteAllText(sPath, projectContents);
#endif
    }

    internal static void PostProcessVisionOsBuild(string path)
    {
#if PLATFORM_VISIONOS
        string simulatorSuffix = PlayerSettings.VisionOS.sdkVersion == VisionOSSdkVersion.Simulator ? "-simulator" : "";

        string vivoxNativeLib = $"libVivoxNative{simulatorSuffix}.a";
        string sdkLibName = $"libvivoxsdk{simulatorSuffix}.a";
        string staticLibrarySrcPath = $"Packages/com.unity.services.vivox/Plugins/visionOS/";
        string relativeLibPathInXcodeProj = $"Libraries/ARM64/com.unity.services.vivox/Plugins/visionOS/";

        PBXProject project = new PBXProject();
        string sPath = path + "/Unity-VisionOS.xcodeproj/project.pbxproj";
        project.ReadFromFile(sPath);

        string vivoxNativeLibSrcPath = $"{staticLibrarySrcPath}{vivoxNativeLib}";
        string vivoxNativeLibPathInXcodeProj = $"{relativeLibPathInXcodeProj}{vivoxNativeLib}";
        AddFileToXcodeProject(project, path, vivoxNativeLibSrcPath, vivoxNativeLibPathInXcodeProj);

        string sdkLibSrcPath = $"{staticLibrarySrcPath}{sdkLibName}";
        string sdkLibPathInXcodeProj = $"{relativeLibPathInXcodeProj}{sdkLibName}";
        AddFileToXcodeProject(project, path, sdkLibSrcPath, sdkLibPathInXcodeProj);

        // Since we don't ask Unity to add our libraries, we need to add the search path manually
        string unityFrameworkTarget = project.GetUnityFrameworkTargetGuid();
        project.AddBuildProperty(unityFrameworkTarget, "LIBRARY_SEARCH_PATHS", "$(PROJECT_DIR)/" + relativeLibPathInXcodeProj);

        string projectContents = project.WriteToString();
        if (simulatorSuffix.Length > 0)
        {
            projectContents = ReplaceSimulatorAllLoadWithForceLoad(projectContents);
        }
        File.WriteAllText(sPath, projectContents);
#endif
    }

#if PLATFORM_IOS || PLATFORM_VISIONOS
    // Unity's Simulator template adds "-all_load", which force-loads every static archive — including
    // Vivox's libvivoxsdk, whose bundled webrtc objects then collide as "duplicate symbol" errors.
    // Swap it for a "-force_load" of just il2cpp (which must stay force-loaded or its runtime-resolved
    // il2cpp_* symbols get dead-stripped, crashing at launch); libvivoxsdk then links on demand, no dupes.
    internal static string ReplaceSimulatorAllLoadWithForceLoad(string projectContents)
    {
        // Some templates (e.g. Unity 6 iOS) use "-exported_symbols_list" instead of "-all_load" and
        // have no duplicate-symbol problem, so there's nothing to do.
        if (!projectContents.Contains(",-all_load"))
        {
            return projectContents;
        }

        // il2cpp library path differs by Unity version.
        bool il2cppIsGeneratedProduct = projectContents.Contains("$CONFIGURATION_BUILD_DIR/il2cpp.a");
        string il2cppLib = il2cppIsGeneratedProduct
            ? "$CONFIGURATION_BUILD_DIR/il2cpp.a"   // Unity 6+: a generated build product
            : "Libraries/libil2cpp.a";              // Unity 2022.3: a pre-existing file
        projectContents = projectContents.Replace(",-all_load", $",-force_load,{il2cppLib}");

        // On Unity 6+ the generated il2cpp.a isn't a declared build output, so "-force_load"-ing it
        // fails with "Build input file cannot be found" until we declare it as its producer's output.
        if (il2cppIsGeneratedProduct)
        {
            projectContents = DeclareIl2CppAsScriptOutput(projectContents);
        }
        return projectContents;
    }

    // Declares il2cpp.a as an output of the il2cpp compile script phase (the GameAssembly phase with
    // "--static-lib-il2-cpp") so Xcode's dependency graph knows what produces it.
    internal static string DeclareIl2CppAsScriptOutput(string projectContents)
    {
        const string anchor = "--static-lib-il2-cpp";
        int anchorIndex = projectContents.IndexOf(anchor, System.StringComparison.Ordinal);
        if (anchorIndex < 0)
        {
            return projectContents;
        }

        const string shellKey = "shellScript = \"";
        int shellStart = projectContents.LastIndexOf(shellKey, anchorIndex, System.StringComparison.Ordinal);
        if (shellStart < 0)
        {
            return projectContents;
        }

        // Don't add a second outputPaths key if this script phase already declares one.
        int phaseStart = projectContents.LastIndexOf("isa = PBXShellScriptBuildPhase;", shellStart, System.StringComparison.Ordinal);
        if (phaseStart >= 0 &&
            projectContents.IndexOf("outputPaths = (", phaseStart, shellStart - phaseStart, System.StringComparison.Ordinal) >= 0)
        {
            return projectContents;
        }

        const string outputDecl = "outputPaths = (\n\t\t\t\t\"$CONFIGURATION_BUILD_DIR/il2cpp.a\",\n\t\t\t);\n\t\t\t";
        return projectContents.Insert(shellStart, outputDecl);
    }

    internal static void AddFileToXcodeProject(PBXProject project, string projectPath, string srcPath, string relativePathInXcodeProj)
    {
        string xcodeLibPath = projectPath + "/" + relativePathInXcodeProj;
        string xcodeLibDirectory = Path.GetDirectoryName(xcodeLibPath);
        if (!Directory.Exists(xcodeLibDirectory))
        {
            Directory.CreateDirectory(xcodeLibDirectory);
        }

        UnityEngine.Debug.Log($"Copying Vivox library from: {srcPath} to {xcodeLibPath}");
        CopyAndReplaceFile(srcPath, xcodeLibPath);
        string target = project.GetUnityFrameworkTargetGuid();
        string fileGuid = project.AddFile(relativePathInXcodeProj, relativePathInXcodeProj);
        project.AddFileToBuild(target, fileGuid);
    }

    internal static void CopyAndReplaceFile(string srcPath, string dstPath)
    {
        if (File.Exists(dstPath))
        {
            File.Delete(dstPath);
        }
        File.Copy(srcPath, dstPath);
    }
#endif
}
