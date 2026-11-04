using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.CompositionLayers;
using UnityEngine.XR.OpenXR.Features.Meta;

public static class LabDemoPassthrough
{
    [MenuItem("Lab Demo/Report Quest Link Status")]
    static void Report()
    {
        if (!Application.isPlaying) { Debug.LogWarning("Run LabDemo in Play Mode first."); return; }
        var camera = Camera.main;
        var manager = camera ? camera.GetComponent<ARCameraManager>() : null;
        var extension = UnityEngine.XR.OpenXR.OpenXRRuntime.IsExtensionEnabled("XR_FB_passthrough");
        var client = Object.FindFirstObjectByType<RobotTcpClient>();
        var loader = UnityEngine.XR.Management.XRGeneralSettings.Instance?.Manager?.activeLoader;
        var cameraSub = loader?.GetLoadedSubsystem<UnityEngine.XR.ARSubsystems.XRCameraSubsystem>();
        var sessionSub = loader?.GetLoadedSubsystem<UnityEngine.XR.ARSubsystems.XRSessionSubsystem>();
        var feature = OpenXRSettings.Instance?.GetFeature<ARCameraFeature>();
        var sessionFeature = OpenXRSettings.Instance?.GetFeature<ARSessionFeature>();
        string component = manager == null ? "missing" : manager.isActiveAndEnabled ? "active" : "disabled";
        Debug.Log($"Quest diagnostics | XR display: {UnityEngine.XR.XRSettings.isDeviceActive}" +
            $" | XR_FB_passthrough: {extension} | AR session: {ARSession.state}" +
            $" | Camera subsystem running: {manager != null && manager.subsystem != null && manager.subsystem.running}" +
            $" | runtime={OpenXRRuntime.name} | loader={loader?.name}" +
            $" | camera manager={component} | camera feature={feature != null && feature.enabled}" +
            $" | camera provider={cameraSub?.subsystemDescriptor.id ?? "none"}" +
            $" | session feature={sessionFeature != null && sessionFeature.enabled} | session provider={sessionSub?.subsystemDescriptor.id ?? "none"}" +
            $" | scene capture={OpenXRRuntime.IsExtensionEnabled("XR_FB_scene_capture")}" +
            $" | Robot TCP connected: {client != null && client.IsConnected}");
    }

    [MenuItem("Lab Demo/Enable Quest Link Passthrough")]
    static void Configure()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/Scenes/LabDemo.unity")
        {
            Debug.LogError("Open LabDemo in Edit Mode first.");
            return;
        }

        var camera = Camera.main;
        if (!camera || camera.gameObject.scene != scene || !camera.GetComponentInParent<XROrigin>())
        {
            Debug.LogError("LabDemo needs its existing XR Origin and tagged Main Camera.");
            return;
        }

        var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);
        var passthrough = settings ? settings.GetFeature<ARCameraFeature>() : null;
        var sessionFeature = settings ? settings.GetFeature<ARSessionFeature>() : null;
        var layers = settings ? settings.GetFeature<OpenXRCompositionLayersFeature>() : null;
        // Meta OpenXR 2.7.0 normally refreshes its hidden lifecycle feature on
        // editor load. Our menu also works when enabling Meta features later.
        var lifecycleType = typeof(ARCameraFeature).Assembly.GetType("UnityEngine.XR.OpenXR.Features.Meta.OpenXRLifeCycleFeature");
        var lifecycle = settings && lifecycleType != null ? settings.GetFeature(lifecycleType) : null;
        if (!passthrough || !sessionFeature || !layers || !lifecycle)
        {
            Debug.LogError("Check Standalone OpenXR settings and the Unity OpenXR: Meta package.");
            return;
        }

        foreach (var feature in new UnityEngine.XR.OpenXR.Features.OpenXRFeature[] {lifecycle, sessionFeature, layers, passthrough})
        {
            Undo.RecordObject(feature, "Enable Quest Link passthrough");
            feature.enabled = true;
            EditorUtility.SetDirty(feature);
        }

        ARSession session = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            session = root.GetComponentInChildren<ARSession>(true);
            if (session) break;
        }
        if (!session)
        {
            var obj = new GameObject("LabDemo AR Session");
            Undo.RegisterCreatedObjectUndo(obj, "Create AR Session");
            session = Undo.AddComponent<ARSession>(obj);
        }
        Undo.RecordObject(session, "Enable AR Session");
        session.enabled = true;
        var manager = camera.GetComponent<ARCameraManager>() ?? Undo.AddComponent<ARCameraManager>(camera.gameObject);
        Undo.RecordObject(manager, "Enable passthrough camera");
        manager.enabled = true;
        Undo.RecordObject(camera, "Make camera background transparent");
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.clear;
        camera.allowHDR = false;
        if (camera.TryGetComponent<UniversalAdditionalCameraData>(out var data))
        {
            Undo.RecordObject(data, "Preserve passthrough alpha");
            data.renderPostProcessing = false;
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("Quest Link passthrough configured. Save LabDemo and check the view in the headset.");
    }
}
