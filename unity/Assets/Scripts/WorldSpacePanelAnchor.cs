using System.Collections;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Keeps the command panel at a readable position in front of the Quest camera.
/// Assign the XR Origin's Main Camera to cameraTransform.
/// </summary>
public class WorldSpacePanelAnchor : MonoBehaviour
{
    public Transform cameraTransform;
    public Vector3 localOffset = new Vector3(0f, -0.05f, 1.25f);
    public bool followPosition = false;
    public bool followYaw = false;

    private IEnumerator Start()
    {
        yield return null; // Let tracking update before the one-time placement.
        while (XRSettings.isDeviceActive)
        {
            var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (head.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) && tracked) break;
            yield return null;
        }
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        if (cameraTransform == null) yield break;
        Vector3 heading = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up);
        if (heading.sqrMagnitude < 0.001f) heading = Vector3.forward;
        Quaternion yaw = Quaternion.LookRotation(heading.normalized, Vector3.up);
        transform.position = cameraTransform.position + yaw * localOffset;
        transform.rotation = yaw;
    }

    private void LateUpdate()
    {
        if (cameraTransform == null) return;
        if (followPosition || followYaw) PlacePanel();
    }

    private void PlacePanel()
    {
        if (followPosition) transform.position = cameraTransform.TransformPoint(localOffset);
        if (followYaw)
        {
            Vector3 forward = cameraTransform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
        }
    }
}
