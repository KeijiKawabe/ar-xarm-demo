from typing import Any, Dict
import math

from .config_loader import validate_pose


class SafetyError(ValueError):
    pass


def validate_motion_config(config: Dict[str, Any]) -> None:
    robot = config["robot"]
    axis = config.get("axis_control", {})
    step = axis.get("step_mm", 10)
    maximum = axis.get("max_step_mm", 25)
    speed = axis.get("speed_mm_s", 30)
    if any(isinstance(v, bool) or not isinstance(v, (int, float)) or not math.isfinite(v) or v <= 0 for v in (step, maximum, speed)):
        raise SafetyError("invalid_axis_settings")
    if step > maximum or speed > config["limits"]["max_speed_mm_s"]:
        raise SafetyError("axis_settings_exceed_limits")
    if robot.get("default_speed_mm_s", 0) > config["limits"]["max_speed_mm_s"]:
        raise SafetyError("default_speed_exceeds_limit")
    if robot.get("approach_speed_mm_s", 0) > config["limits"]["max_speed_mm_s"]:
        raise SafetyError("approach_speed_exceeds_limit")


def pose(config: Dict[str, Any], name: str) -> list[float]:
    try:
        value = config["coordinates"][name]
    except KeyError as exc:
        raise SafetyError(f"unknown_pose:{name}") from exc
    validate_pose(value, config["limits"])
    return value
