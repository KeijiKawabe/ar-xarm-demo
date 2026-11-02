from typing import Any, Callable, Dict

from .safety import pose
from .config_loader import validate_pose
from .state_machine import AXIS_COMMANDS


class MotionSequence:
    def __init__(self, config: Dict[str, Any], adapter: Any, notify: Callable[[str, str | None], None]) -> None:
        self.config = config
        self.adapter = adapter
        self.notify = notify
        self.cancelled = lambda: False

    def check_cancelled(self) -> None:
        if self.cancelled():
            raise RuntimeError("stopped")

    def axis_target(self, command: str) -> list[float]:
        current = self.adapter.get_pose()
        validate_pose(current, self.config["limits"])
        target = list(current)
        target["XYZ".index(command.split("_")[1])] += self.config.get("axis_control", {}).get("step_mm", 10) * (1 if command.endswith("PLUS") else -1)
        validate_pose(target, self.config["limits"])
        return target

    def execute(self, command: str, target=None) -> None:
        self.check_cancelled()
        if command in AXIS_COMMANDS:
            self.check_cancelled()
            robot = self.config["robot"]
            self.adapter.move_pose(target if target is not None else self.axis_target(command),
                speed=self.config.get("axis_control", {}).get("speed_mm_s", 30),
                acceleration=robot["acceleration_mm_s2"], timeout=robot["timeout_s"])
            self.check_cancelled()
            return
        if command == "START":
            self.adapter.prepare()
            self.check_cancelled()
            self.adapter.open_gripper()
            return
        if command == "PICK":
            self.check_cancelled()
            self.adapter.open_gripper()
            self._move("PICK_APPROACH", approach=True)
            self._move("PICK", approach=True)
            self.check_cancelled()
            self.adapter.close_gripper()
            self._move("PICK_APPROACH", approach=True)
            return
        if command in ("MOVE_A", "MOVE_B", "MOVE_C"):
            self._move(command)
            return
        if command == "PLACE":
            self._move("PLACE", approach=True)
            self.check_cancelled()
            self.adapter.open_gripper()
            self._move("PLACE_RETRACT", approach=True)
            return
        if command == "RESET":
            self.adapter.prepare()
            self.check_cancelled()
            self.adapter.open_gripper()
            self._move("HOME", approach=True)
            return
        if command == "STOP":
            self.adapter.stop()
            return
        raise ValueError(f"unknown_motion:{command}")

    def _move(self, name: str, approach: bool = False) -> None:
        self.check_cancelled()
        robot = self.config["robot"]
        self.adapter.move_pose(
            pose(self.config, name),
            speed=robot["approach_speed_mm_s"] if approach else robot["default_speed_mm_s"],
            acceleration=robot["acceleration_mm_s2"],
            timeout=robot["timeout_s"],
        )
