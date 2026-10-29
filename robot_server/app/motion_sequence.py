from typing import Any, Callable, Dict

from .safety import pose


class MotionSequence:
    def __init__(self, config: Dict[str, Any], adapter: Any, notify: Callable[[str, str | None], None]) -> None:
        self.config = config
        self.adapter = adapter
        self.notify = notify

    def execute(self, command: str) -> None:
        if command == "START":
            self.adapter.prepare()
            self.adapter.open_gripper()
            return
        if command == "PICK":
            self.adapter.open_gripper()
            self._move("PICK_APPROACH", approach=True)
            self._move("PICK", approach=True)
            self.adapter.close_gripper()
            self._move("PICK_APPROACH", approach=True)
            return
        if command in ("MOVE_A", "MOVE_B", "MOVE_C"):
            self._move(command)
            return
        if command == "PLACE":
            self._move("PLACE", approach=True)
            self.adapter.open_gripper()
            self._move("PLACE_RETRACT", approach=True)
            return
        if command == "RESET":
            self.adapter.open_gripper()
            self._move("HOME", approach=True)
            return
        if command == "STOP":
            self.adapter.stop()
            return
        raise ValueError(f"unknown_motion:{command}")

    def _move(self, name: str, approach: bool = False) -> None:
        robot = self.config["robot"]
        self.adapter.move_pose(
            pose(self.config, name),
            speed=robot["approach_speed_mm_s"] if approach else robot["default_speed_mm_s"],
            acceleration=robot["acceleration_mm_s2"],
            timeout=robot["timeout_s"],
        )
