from enum import Enum


class DemoState(str, Enum):
    DISCONNECTED = "DISCONNECTED"
    CONNECTED = "CONNECTED"
    READY = "READY"
    STARTED = "STARTED"
    PICKING = "PICKING"
    PICKED = "PICKED"
    MOVING_A = "MOVING_A"
    MOVE_A_COMPLETED = "MOVE_A_COMPLETED"
    MOVING_B = "MOVING_B"
    MOVE_B_COMPLETED = "MOVE_B_COMPLETED"
    MOVING_C = "MOVING_C"
    MOVE_C_COMPLETED = "MOVE_C_COMPLETED"
    PLACING = "PLACING"
    SUCCESS = "SUCCESS"
    RESETTING = "RESETTING"
    STOPPING = "STOPPING"
    STOPPED = "STOPPED"
    FAULT = "FAULT"


class StateMachine:
    def __init__(self) -> None:
        self.state = DemoState.DISCONNECTED

    def connect(self) -> None:
        self.state = DemoState.CONNECTED

    def ready(self) -> None:
        self.state = DemoState.READY

    def fault(self) -> None:
        self.state = DemoState.FAULT

    def stopped(self) -> None:
        self.state = DemoState.STOPPED

    def expected(self, command: str) -> bool:
        allowed = {
            DemoState.READY: {"START", "RESET", "GET_STATUS"},
            DemoState.STARTED: {"PICK", "STOP", "GET_STATUS"},
            DemoState.PICKED: {"MOVE_A", "STOP", "GET_STATUS"},
            DemoState.MOVE_A_COMPLETED: {"MOVE_B", "STOP", "GET_STATUS"},
            DemoState.MOVE_B_COMPLETED: {"MOVE_C", "STOP", "GET_STATUS"},
            DemoState.MOVE_C_COMPLETED: {"PLACE", "STOP", "GET_STATUS"},
            DemoState.SUCCESS: {"RESET", "GET_STATUS"},
            DemoState.STOPPED: {"RESET", "GET_STATUS"},
            DemoState.FAULT: {"RESET", "GET_STATUS"},
            DemoState.CONNECTED: {"START", "RESET", "GET_STATUS"},
        }
        return command in allowed.get(self.state, {"GET_STATUS", "STOP"})

    def begin(self, command: str) -> DemoState:
        if not self.expected(command):
            raise ValueError(f"{command} is not allowed in {self.state.value}")
        transitions = {
            "START": DemoState.STARTED,
            "PICK": DemoState.PICKING,
            "MOVE_A": DemoState.MOVING_A,
            "MOVE_B": DemoState.MOVING_B,
            "MOVE_C": DemoState.MOVING_C,
            "PLACE": DemoState.PLACING,
            "RESET": DemoState.RESETTING,
            "STOP": DemoState.STOPPING,
        }
        self.state = transitions.get(command, self.state)
        return self.state

    def complete(self, command: str) -> DemoState:
        transitions = {
            "START": DemoState.STARTED,
            "PICK": DemoState.PICKED,
            "MOVE_A": DemoState.MOVE_A_COMPLETED,
            "MOVE_B": DemoState.MOVE_B_COMPLETED,
            "MOVE_C": DemoState.MOVE_C_COMPLETED,
            "PLACE": DemoState.SUCCESS,
            "RESET": DemoState.READY,
            "STOP": DemoState.STOPPED,
        }
        self.state = transitions[command]
        return self.state
