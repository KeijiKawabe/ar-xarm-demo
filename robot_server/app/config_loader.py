import json
from pathlib import Path
from typing import Any, Dict


class ConfigError(ValueError):
    pass


def load_config(path: str | Path) -> Dict[str, Any]:
    file_path = Path(path)
    try:
        config = json.loads(file_path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise ConfigError(f"cannot_load_config:{file_path}") from exc
    validate_config(config)
    return config


def validate_config(config: Dict[str, Any]) -> None:
    for key in ("server", "robot", "coordinates", "limits"):
        if key not in config:
            raise ConfigError(f"missing_config:{key}")
    if config["server"].get("port", 0) <= 0:
        raise ConfigError("invalid_port")
    limits = config["limits"]
    for axis in ("x_mm", "y_mm", "z_mm"):
        if axis not in limits or len(limits[axis]) != 2:
            raise ConfigError(f"invalid_limit:{axis}")
    for name, pose in config["coordinates"].items():
        if len(pose) != 6:
            raise ConfigError(f"invalid_pose:{name}")
        validate_pose(pose, limits)


def validate_pose(pose: list[float], limits: Dict[str, Any]) -> None:
    for value, axis in zip(pose[:3], ("x_mm", "y_mm", "z_mm")):
        low, high = limits[axis]
        if not low <= value <= high:
            raise ConfigError(f"pose_out_of_range:{axis}:{value}")
