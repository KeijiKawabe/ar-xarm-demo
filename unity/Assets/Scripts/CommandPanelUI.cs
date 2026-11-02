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
    public Button xPlusButton, xMinusButton, yPlusButton, yMinusButton, zPlusButton, zMinusButton;
    public Text connectionText, axisStepText;
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
        if (connectionText != null && controller != null) connectionText.text = "LINK: " + controller.connectionStatus;
        if (axisStepText != null && controller != null) axisStepText.text = controller.axisStepMm > 0 ? "BASE XYZ / " + controller.axisStepMm.ToString("0.##") + " mm per click" : "BASE XYZ / waiting for server";
        Set(xPlusButton, "MOVE_X_PLUS"); Set(xMinusButton, "MOVE_X_MINUS");
        Set(yPlusButton, "MOVE_Y_PLUS"); Set(yMinusButton, "MOVE_Y_MINUS");
        Set(zPlusButton, "MOVE_Z_PLUS"); Set(zMinusButton, "MOVE_Z_MINUS");
        Set(startButton, "START");
        Set(pickButton, "PICK");
        Set(moveAButton, "MOVE_A");
        Set(moveBButton, "MOVE_B");
        Set(moveCButton, "MOVE_C");
        Set(placeButton, "PLACE");
        Set(resetButton, "RESET");
        Set(stopButton, "STOP");
    }

    private void Set(Button button, string command)
    {
        if (button != null && controller != null) button.interactable = controller.CanExecute(command);
    }
}
