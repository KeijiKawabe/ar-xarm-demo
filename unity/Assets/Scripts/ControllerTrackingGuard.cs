using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

// A lost controller must not leave a usable ray at its last known pose.
[DefaultExecutionOrder(-100)]
public class ControllerTrackingGuard : MonoBehaviour
{
    public XRNode hand = XRNode.RightHand;
    public XRRayInteractor ray;
    public XRInteractorLineVisual visual;
    public LineRenderer line;
    void Update()
    {
        var device = InputDevices.GetDeviceAtXRNode(hand);
        bool tracked = device.isValid && device.TryGetFeatureValue(CommonUsages.isTracked, out bool value) && value;
        if (ray != null) ray.enabled = tracked;
        if (visual != null) visual.enabled = tracked;
        if (line != null && !tracked) line.enabled = false;
    }
}
