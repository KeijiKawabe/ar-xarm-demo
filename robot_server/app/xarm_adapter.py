from typing import Any, Dict


class XArmAdapter:
    """Thin adapter around the official xArm Python SDK.

    The SDK is imported only when real mode is selected, so simulation and tests
    work without installing the vendor package.
    """

    def __init__(self, config: Dict[str, Any]) -> None:
        self.config = config
        self.arm = None
        self._connect()

    def _connect(self) -> None:
        try:
            from xarm.wrapper import XArmAPI
        except ImportError as exc:
            raise RuntimeError("xarm-python-sdk is required for real mode") from exc
        self.arm = XArmAPI(self.config["robot"]["ip"], is_radian=self.config["robot"].get("is_radian", False))

    def prepare(self) -> None:
        assert self.arm is not None
        self.arm.motion_enable(enable=True)
        self.arm.set_mode(0)
        self.arm.set_state(0)
        if hasattr(self.arm, "set_gripper_enable"):
            self.arm.set_gripper_enable(True)

    def move_pose(self, pose: list[float], speed: float, acceleration: float, timeout: float) -> None:
        assert self.arm is not None
        code = self.arm.set_position(
            x=pose[0], y=pose[1], z=pose[2], roll=pose[3], pitch=pose[4], yaw=pose[5],
            speed=speed, mvacc=acceleration, wait=True, timeout=timeout,
            is_radian=self.config["robot"].get("is_radian", False),
        )
        if isinstance(code, tuple):
            code = code[0]
        if code not in (0, None):
            raise RuntimeError(f"xarm_set_position_failed:{code}")

    def open_gripper(self) -> None:
        self._set_gripper(self.config["gripper"]["open_position"])

    def close_gripper(self) -> None:
        self._set_gripper(self.config["gripper"]["close_position"])

    def _set_gripper(self, position: int) -> None:
        assert self.arm is not None
        code = self.arm.set_gripper_position(
            position, speed=self.config["gripper"].get("speed", 100), wait=True
        )
        if isinstance(code, tuple):
            code = code[0]
        if code not in (0, None):
            raise RuntimeError(f"xarm_gripper_failed:{code}")

    def stop(self) -> None:
        if self.arm is not None:
            self.arm.emergency_stop()

    def status(self) -> Dict[str, Any]:
        if self.arm is None:
            return {"robot_state": "DISCONNECTED"}
        code, state = self.arm.get_state()
        return {"robot_state": state, "api_code": code}

    def disconnect(self) -> None:
        if self.arm is not None:
            self.arm.disconnect()
