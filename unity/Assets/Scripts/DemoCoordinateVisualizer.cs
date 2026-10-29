using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Draws the sample robot coordinates in Unity meters. The robotFrame transform
/// is the calibration origin; replace its pose after measuring the real setup.
/// </summary>
public class DemoCoordinateVisualizer : MonoBehaviour
{
    public Transform robotFrame;
    public GameObject waypointPrefab;
    public LineRenderer pathLine;
    public float millimetersToMeters = 0.001f;
    public Color normalColor = new Color(0.1f, 0.7f, 1f, 0.8f);
    public Color activeColor = new Color(1f, 0.75f, 0.1f, 1f);

    private readonly Dictionary<string, Vector3> samplePoints = new Dictionary<string, Vector3>
    {
        { "PICK", new Vector3(420f, 0f, 95f) },
        { "MOVE_A", new Vector3(360f, 80f, 220f) },
        { "MOVE_B", new Vector3(300f, 160f, 240f) },
        { "MOVE_C", new Vector3(180f, 160f, 220f) },
        { "PLACE", new Vector3(180f, 160f, 105f) },
    };

    private readonly List<GameObject> spawned = new List<GameObject>();
    private RobotDemoController controller;

    private void Start()
    {
        if (robotFrame == null) robotFrame = transform;
        controller = FindObjectOfType<RobotDemoController>();
        if (controller != null) controller.StateChanged += OnStateChanged;
        Build();
    }

    private void OnDestroy()
    {
        if (controller != null) controller.StateChanged -= OnStateChanged;
    }

    public Vector3 RobotMillimetersToUnity(Vector3 robotMillimeters)
    {
        return robotFrame.TransformPoint(robotMillimeters * millimetersToMeters);
    }

    private void Build()
    {
        foreach (KeyValuePair<string, Vector3> item in samplePoints)
        {
            GameObject marker = waypointPrefab != null
                ? Instantiate(waypointPrefab, RobotMillimetersToUnity(item.Value), Quaternion.identity, robotFrame)
                : GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "AR_WAYPOINT_" + item.Key;
            if (waypointPrefab == null)
            {
                marker.transform.SetParent(robotFrame, true);
                marker.transform.position = RobotMillimetersToUnity(item.Value);
                marker.transform.localScale = Vector3.one * 0.035f;
            }
            spawned.Add(marker);
        }
        if (pathLine != null)
        {
            pathLine.positionCount = samplePoints.Count;
            int index = 0;
            foreach (Vector3 point in samplePoints.Values) pathLine.SetPosition(index++, RobotMillimetersToUnity(point));
        }
    }

    private void OnStateChanged(string state)
    {
        string activeStep = controller != null ? controller.lastStep : string.Empty;
        for (int i = 0; i < spawned.Count; i++)
        {
            string key = i == 0 ? "PICK" : i == 1 ? "MOVE_A" : i == 2 ? "MOVE_B" : i == 3 ? "MOVE_C" : "PLACE";
            Renderer renderer = spawned[i].GetComponentInChildren<Renderer>();
            if (renderer != null) renderer.material.color = key == activeStep ? activeColor : normalColor;
        }
    }
}
