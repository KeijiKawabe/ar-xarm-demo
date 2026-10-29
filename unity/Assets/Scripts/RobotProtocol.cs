using System;

[Serializable]
public class RobotMessage
{
    public string version;
    public string type;
    public string request_id;
    public string command;
    public string @event;
    public string state;
    public string step;
    public bool ok;
    public bool accepted;
    public string code;
    public string message;
    public string error;
    public RobotStateInfo robot_state;
    public string timestamp;
}

[Serializable]
public class RobotStateInfo
{
    public string robot_state;
    public string gripper;
    public float[] pose;
    public int api_code;
}
