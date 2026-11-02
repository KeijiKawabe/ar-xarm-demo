using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Unity.XR.CoreUtils;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;

public static class LabDemoPanelBuilder
{
    [MenuItem("Lab Demo/Create Command Panel")]
    public static void Build()
    {
        if (Application.isPlaying) return;
        var controller = Object.FindFirstObjectByType<RobotDemoController>();
        if (controller == null || Camera.main == null)
        { Debug.LogError("Open LabDemo with RobotManager and the XR camera first."); return; }
        var origin = Camera.main.GetComponentInParent<XROrigin>();
        if (origin == null || origin.CameraFloorOffsetObject == null)
        { Debug.LogError("XR Origin / Camera Offset is missing."); return; }
        if (Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 1)
        { Debug.LogError("Resolve duplicate EventSystems first."); return; }
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build Lab Demo UI");
        var previous = GameObject.Find("LabDemo Command Panel");
        if (previous != null) Undo.DestroyObjectImmediate(previous);
        var root = new GameObject("LabDemo Command Panel", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(TrackedDeviceGraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(root, "Create command panel");
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(760, 700);
        root.transform.localScale = Vector3.one * 0.001f;
        var anchor = root.AddComponent<WorldSpacePanelAnchor>();
        anchor.cameraTransform = Camera.main.transform;
        anchor.followPosition = false;
        anchor.followYaw = false;
        root.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 1.25f;
        root.transform.rotation = Quaternion.Euler(0, Camera.main.transform.eulerAngles.y, 0);
        var background = root.AddComponent<Image>();
        background.color = new Color(0.035f, 0.055f, 0.09f, 0.98f);
        var ui = root.AddComponent<CommandPanelUI>();
        ui.controller = controller;
        Label(root.transform, "XARM / COMMAND PANEL", 310, 32);
        ui.connectionText = Label(root.transform, "LINK: DISCONNECTED", 270, 20);
        ui.stateText = Label(root.transform, "STATE: DISCONNECTED", 235, 24);
        ui.stepText = Label(root.transform, "STEP:", 200, 24);
        ui.errorText = Label(root.transform, "", -290, 20);
        ui.startButton = Button(root.transform, "START", -180, 145, controller.StartDemo);
        ui.pickButton = Button(root.transform, "PICK", 180, 145, controller.Pick);
        string[] axes = {"X +", "X -", "Y +", "Y -", "Z +", "Z -"};
        UnityEngine.Events.UnityAction[] actions = { controller.MoveXPlus, controller.MoveXMinus, controller.MoveYPlus, controller.MoveYMinus, controller.MoveZPlus, controller.MoveZMinus };
        var buttons = new Button[6];
        for (int i = 0; i < axes.Length; i++)
        {
            var button = Button(root.transform, axes[i], i % 2 == 0 ? -180 : 180, 65 - (i / 2) * 75, actions[i]);
            buttons[i] = button;
        }
        ui.xPlusButton = buttons[0]; ui.xMinusButton = buttons[1];
        ui.yPlusButton = buttons[2]; ui.yMinusButton = buttons[3];
        ui.zPlusButton = buttons[4]; ui.zMinusButton = buttons[5];
        ui.placeButton = Button(root.transform, "PLACE", -180, -165, controller.Place);
        ui.resetButton = Button(root.transform, "RESET", 180, -165, controller.Reset);
        ui.stopButton = Button(root.transform, "STOP", 0, -235, controller.Stop);
        ui.stopButton.GetComponent<Image>().color = new Color(0.7f, 0.08f, 0.1f);
        ui.axisStepText = Label(root.transform, "BASE XYZ / waiting for server", -330, 18);
        var events = Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
        if (events == null)
        {
            var eventObject = new GameObject("EventSystem", typeof(EventSystem));
            Undo.RegisterCreatedObjectUndo(eventObject, "Create XR EventSystem");
            events = eventObject.GetComponent<EventSystem>();
        }
        Undo.RecordObject(events.gameObject, "Enable EventSystem");
        events.gameObject.SetActive(true);
        Undo.RecordObject(events, "Enable EventSystem");
        events.enabled = true;
        foreach (var module in events.GetComponents<BaseInputModule>())
        { Undo.RecordObject(module, "Configure input module"); module.enabled = module is XRUIInputModule; }
        var xrUI = events.GetComponent<XRUIInputModule>();
        if (xrUI == null) xrUI = Undo.AddComponent<XRUIInputModule>(events.gameObject);
        xrUI.enabled = true; xrUI.enableXRInput = true; xrUI.enableMouseInput = true;
        xrUI.enableTouchInput = false; xrUI.enableGamepadInput = false;
        if (Object.FindFirstObjectByType<XRInteractionManager>() == null)
        {
            var manager = new GameObject("XR Interaction Manager", typeof(XRInteractionManager));
            Undo.RegisterCreatedObjectUndo(manager, "Create interaction manager");
        }
        CreateController(origin.CameraFloorOffsetObject.transform, "LeftHand", XRNode.LeftHand);
        CreateController(origin.CameraFloorOffsetObject.transform, "RightHand", XRNode.RightHand);
        foreach (var target in new[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android })
        {
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(target);
            var profile = settings != null ? settings.GetFeature<OculusTouchControllerProfile>() : null;
            if (profile == null) { Debug.LogWarning("Enable Oculus Touch Controller Profile for " + target); continue; }
            Undo.RecordObject(profile, "Enable Touch input");
            profile.enabled = true;
            EditorUtility.SetDirty(profile);
        }
        ui.Refresh(controller.currentState);
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Debug.Log("Panel and controller rays created. Save LabDemo, start the simulation server, then Play.");
    }

    static void CreateController(Transform parent, string usage, XRNode hand)
    {
        string name = "LabDemo " + usage + " Ray";
        var old = parent.Find(name);
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
        var go = new GameObject(name);
        go.SetActive(false);
        go.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(go, "Create controller ray");
        string path = "<XRController>{" + usage + "}/";
        var tracking = go.AddComponent<TrackedPoseDriver>();
        tracking.positionInput = new InputActionProperty(new InputAction("Aim position", InputActionType.Value, path + "pointerPosition", expectedControlType: "Vector3"));
        tracking.rotationInput = new InputActionProperty(new InputAction("Aim rotation", InputActionType.Value, path + "pointerRotation", expectedControlType: "Quaternion"));
        tracking.trackingStateInput = new InputActionProperty(new InputAction("Tracking", InputActionType.Value, path + "trackingState", expectedControlType: "Integer"));
        var ray = go.AddComponent<XRRayInteractor>();
        ray.enableUIInteraction = true;
        ray.maxRaycastDistance = 5f;
        ray.interactionLayers = 0; // This ray operates UI only.
        ray.uiPressInput = new XRInputButtonReader("UI press", inputSourceMode: XRInputButtonReader.InputSourceMode.InputAction)
        {
            inputActionPerformed = new InputAction("Trigger press", InputActionType.Button, path + "triggerPressed"),
            inputActionValue = new InputAction("Trigger value", InputActionType.Value, path + "trigger")
        };
        var line = go.AddComponent<LineRenderer>();
        var visual = go.AddComponent<XRInteractorLineVisual>();
        visual.lineWidth = 0.003f;
        visual.lineLength = 5f;
        // Store a material asset so its shader is retained in player builds.
        const string materialPath = "Assets/LabDemoRay.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            material = new Material(shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.cyan);
            AssetDatabase.CreateAsset(material, materialPath);
        }
        line.sharedMaterial = material;
        var guard = go.AddComponent<ControllerTrackingGuard>();
        guard.hand = hand; guard.ray = ray; guard.visual = visual; guard.line = line;
        go.SetActive(true);
    }

    static Text Label(Transform parent, string value, float y, int size)
    {
        var go = new GameObject(value.Length > 0 ? value : "Error", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value; text.fontSize = size; text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white; text.raycastTarget = false;
        text.rectTransform.sizeDelta = new Vector2(720, 42);
        text.rectTransform.anchoredPosition = new Vector2(0, y);
        return text;
    }

    static Button Button(Transform parent, string caption, float x, float y, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject(caption, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(320, 60); rect.anchoredPosition = new Vector2(x, y);
        go.GetComponent<Image>().color = new Color(0.08f, 0.32f, 0.48f);
        var button = go.GetComponent<Button>();
        button.targetGraphic = go.GetComponent<Image>();
        var label = Label(go.transform, caption, 0, 25);
        label.rectTransform.sizeDelta = rect.sizeDelta;
        if (action != null) UnityEventTools.AddPersistentListener(button.onClick, action);
        return button;
    }
}
