import threading
import time
from typing import Any, Dict


class SimRobotAdapter:
    def __init__(self, config: Dict[str, Any]) -> None:
        self.config = config
        self._stop = threading.Event()
        self.state = "READY"
        self.current_pose = list(config["coordinates"]["HOME"])
        self.gripper = "OPEN"

    def prepare(self) -> None:
        self.state = "READY"

    def move_pose(self, pose: list[float], speed: float, acceleration: float, timeout: float) -> None:
        self._stop.clear()
        delay = float(self.config["robot"].get("simulation_delay_s", 0.05))
        started = time.monotonic()
        while time.monotonic() - started < delay:
            if self._stop.is_set():
                raise RuntimeError("stopped")
            time.sleep(0.01)
            if time.monotonic() - started > timeout:
                raise TimeoutError("motion_timeout")
        self.current_pose = list(pose)

    def open_gripper(self) -> None:
        self.gripper = "OPEN"

    def close_gripper(self) -> None:
        self.gripper = "CLOSED"

    def stop(self) -> None:
        self._stop.set()
        self.state = "STOPPED"

    def status(self) -> Dict[str, Any]:
        return {"robot_state": self.state, "gripper": self.gripper, "pose": self.current_pose}

    def disconnect(self) -> None:
        self._stop.set()
