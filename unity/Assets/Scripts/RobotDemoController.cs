using System;
using UnityEngine;

public class RobotDemoController : MonoBehaviour
{
    public RobotTcpClient client;
    public string currentState = "DISCONNECTED";
    public string lastStep = "";
    public string lastError = "";
    public string connectionStatus = "DISCONNECTED";
    public float axisStepMm;
    string pendingRequest;
    public event Action<string> StateChanged;

    void OnEnable()
    {
        if (client == null) client = GetComponent<RobotTcpClient>();
        if (client == null) return;
        client.MessageReceived += OnMessage;
        client.ConnectionChanged += OnConnectionChanged;
    }
    void OnDisable()
    {
        if (client == null) return;
        client.MessageReceived -= OnMessage;
        client.ConnectionChanged -= OnConnectionChanged;
    }
    public bool CanExecute(string command)
    {
        if (client == null || !client.IsConnected) return false;
        if (command == "STOP") return true;
        if (pendingRequest != null) return false;
        switch (currentState)
        {
            case "READY": return command == "START" || command == "RESET";
            case "STARTED": return command == "PICK";
            case "PICKED": return command == "PLACE" || command == "MOVE_A" || IsAxisCommand(command);
            case "MOVE_A_COMPLETED": return command == "MOVE_B";
            case "MOVE_B_COMPLETED": return command == "MOVE_C";
            case "MOVE_C_COMPLETED": return command == "PLACE";
            case "SUCCESS": case "STOPPED": case "FAULT": return command == "RESET";
            default: return false;
        }
    }
    static bool IsAxisCommand(string c)
    {
        return c == "MOVE_X_PLUS" || c == "MOVE_X_MINUS" || c == "MOVE_Y_PLUS" || c == "MOVE_Y_MINUS" || c == "MOVE_Z_PLUS" || c == "MOVE_Z_MINUS";
    }
    public void Execute(string command)
    {
        if (!CanExecute(command)) return;
        lastError = "";
        pendingRequest = client.Send(command);
        if (pendingRequest == null) lastError = "Not connected";
        StateChanged?.Invoke(currentState);
    }
    public void StartDemo() { Execute("START"); }
    public void Pick() { Execute("PICK"); }
    public void MoveA() { Execute("MOVE_A"); }
    public void MoveB() { Execute("MOVE_B"); }
    public void MoveC() { Execute("MOVE_C"); }
    public void MoveXPlus() { Execute("MOVE_X_PLUS"); }
    public void MoveXMinus() { Execute("MOVE_X_MINUS"); }
    public void MoveYPlus() { Execute("MOVE_Y_PLUS"); }
    public void MoveYMinus() { Execute("MOVE_Y_MINUS"); }
    public void MoveZPlus() { Execute("MOVE_Z_PLUS"); }
    public void MoveZMinus() { Execute("MOVE_Z_MINUS"); }
    public void Place() { Execute("PLACE"); }
    public void Reset() { Execute("RESET"); }
    public void Stop() { if (client != null && client.IsConnected) client.Send("STOP"); }
    void OnConnectionChanged(string status)
    {
        connectionStatus = status;
        if (status != "CONNECTED") { currentState = "DISCONNECTED"; pendingRequest = null; axisStepMm = 0; }
        else currentState = "CONNECTED";
        if (status.Contains("ERROR")) lastError = status;
        StateChanged?.Invoke(currentState);
    }
    void OnMessage(RobotMessage message)
    {
        if (message == null) return;
        if (!string.IsNullOrEmpty(message.state)) currentState = message.state;
        if (!string.IsNullOrEmpty(message.step)) lastStep = message.step;
        if (message.axis_step_mm > 0) axisStepMm = message.axis_step_mm;
        if (message.type == "response" && !message.ok)
        {
            lastError = string.IsNullOrEmpty(message.message) ? message.code : message.message;
            if (message.request_id == pendingRequest) pendingRequest = null;
        }
        if (message.@event == "STEP_COMPLETED" && message.request_id == pendingRequest) pendingRequest = null;
        if (message.@event == "FAULT" || message.@event == "STOPPED")
        { pendingRequest = null; lastError = message.@event == "FAULT" ? message.error : "Stopped"; }
        if (message.type == "error") { lastError = message.code; pendingRequest = null; }
        StateChanged?.Invoke(currentState);
    }
}
