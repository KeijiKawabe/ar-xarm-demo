using UnityEngine;
using UnityEngine.UI;

public class CommandPanelUI : MonoBehaviour
{
    public RobotDemoController controller;
    public Button startButton;
    public Button pickButton;
    public Button moveAButton;
    public Button moveBButton;
    public Button moveCButton;
    public Button placeButton;
    public Button resetButton;
    public Button stopButton;
    public Text stateText;
    public Text stepText;
    public Text errorText;

    private void OnEnable()
    {
        if (controller != null) controller.StateChanged += Refresh;
        Refresh(controller != null ? controller.currentState : "DISCONNECTED");
    }

    private void OnDisable()
    {
        if (controller != null) controller.StateChanged -= Refresh;
    }

    public void Refresh(string state)
    {
        if (stateText != null) stateText.text = "STATE: " + state;
        if (stepText != null && controller != null) stepText.text = "STEP: " + controller.lastStep;
        if (errorText != null && controller != null) errorText.text = controller.lastError;
        Set(startButton, "START");
        Set(pickButton, "PICK");
        Set(moveAButton, "MOVE_A");
        Set(moveBButton, "MOVE_B");
        Set(moveCButton, "MOVE_C");
        Set(placeButton, "PLACE");
        Set(resetButton, "RESET");
        if (stopButton != null) stopButton.interactable = state != "READY" && state != "SUCCESS" && state != "DISCONNECTED";
    }

    private void Set(Button button, string command)
    {
        if (button != null && controller != null) button.interactable = controller.CanExecute(command);
    }
}
