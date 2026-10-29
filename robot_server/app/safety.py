from typing import Any, Dict

from .config_loader import validate_pose


class SafetyError(ValueError):
    pass


def validate_motion_config(config: Dict[str, Any]) -> None:
    robot = config["robot"]
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
