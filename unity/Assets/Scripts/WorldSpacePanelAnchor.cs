using UnityEngine;

/// <summary>
/// Keeps the command panel at a readable position in front of the Quest camera.
/// Assign the XR Origin's Main Camera to cameraTransform.
/// </summary>
public class WorldSpacePanelAnchor : MonoBehaviour
{
    public Transform cameraTransform;
    public Vector3 localOffset = new Vector3(0f, -0.05f, 1.25f);
    public bool followPosition = true;
    public bool followYaw = true;

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        PlacePanel();
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
