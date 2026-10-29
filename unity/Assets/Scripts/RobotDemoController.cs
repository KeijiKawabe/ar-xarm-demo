using System;
using UnityEngine;

public class RobotDemoController : MonoBehaviour
{
    public RobotTcpClient client;
    public string currentState = "DISCONNECTED";
    public string lastStep = "";
    public string lastError = "";

    public event Action<string> StateChanged;

    private void OnEnable()
    {
        if (client != null) client.MessageReceived += OnMessage;
        if (client != null) client.ConnectionChanged += OnConnectionChanged;
    }

    private void OnDisable()
    {
        if (client != null) client.MessageReceived -= OnMessage;
        if (client != null) client.ConnectionChanged -= OnConnectionChanged;
    }

    public bool CanExecute(string command)
    {
        switch (currentState)
        {
            case "READY": return command == "START" || command == "RESET";
            case "STARTED": return command == "PICK" || command == "STOP";
            case "PICKED": return command == "MOVE_A" || command == "STOP";
            case "MOVE_A_COMPLETED": return command == "MOVE_B" || command == "STOP";
            case "MOVE_B_COMPLETED": return command == "MOVE_C" || command == "STOP";
            case "MOVE_C_COMPLETED": return command == "PLACE" || command == "STOP";
            case "SUCCESS": return command == "RESET";
            case "STOPPED": return command == "RESET";
            case "FAULT": return command == "RESET";
            default: return command == "STOP";
        }
    }

    public void Execute(string command)
    {
        if (client == null || !CanExecute(command)) return;
        client.Send(command);
    }

    public void StartDemo() { Execute("START"); }
    public void Pick() { Execute("PICK"); }
    public void MoveA() { Execute("MOVE_A"); }
    public void MoveB() { Execute("MOVE_B"); }
    public void MoveC() { Execute("MOVE_C"); }
    public void Place() { Execute("PLACE"); }
    public void Reset() { Execute("RESET"); }
    public void Stop() { if (client != null) client.Send("STOP"); }

    private void OnConnectionChanged(string status)
    {
        currentState = status == "CONNECTED" ? "CONNECTED" : currentState;
        StateChanged?.Invoke(currentState);
    }

    private void OnMessage(RobotMessage message)
    {
        if (!string.IsNullOrEmpty(message.state)) currentState = message.state;
        if (!string.IsNullOrEmpty(message.step)) lastStep = message.step;
        if (!string.IsNullOrEmpty(message.message)) lastError = message.message;
        if (message.type == "error" || message.@event == "FAULT") lastError = message.message ?? message.code;
        StateChanged?.Invoke(currentState);
    }
}
