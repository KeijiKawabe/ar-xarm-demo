import json
from datetime import datetime, timezone
from typing import Any, Dict


PROTOCOL_VERSION = "1.0"


class ProtocolError(ValueError):
    pass


def now_iso() -> str:
    return datetime.now(timezone.utc).astimezone().isoformat(timespec="seconds")


def encode(message: Dict[str, Any]) -> bytes:
    return (json.dumps(message, ensure_ascii=False, separators=(",", ":")) + "\n").encode("utf-8")


def decode_line(line: bytes) -> Dict[str, Any]:
    try:
        value = json.loads(line.decode("utf-8"))
    except (UnicodeDecodeError, json.JSONDecodeError) as exc:
        raise ProtocolError("invalid_json") from exc
    if not isinstance(value, dict):
        raise ProtocolError("message_must_be_object")
    if value.get("version") != PROTOCOL_VERSION:
        raise ProtocolError("unsupported_version")
    if value.get("type") != "command":
        raise ProtocolError("only_command_messages_are_accepted")
    if not isinstance(value.get("command"), str):
        raise ProtocolError("command_is_required")
    if not isinstance(value.get("request_id"), str) or not value["request_id"]:
        raise ProtocolError("request_id_is_required")
    return value


def response(request_id: str, ok: bool, state: str, **extra: Any) -> Dict[str, Any]:
    result: Dict[str, Any] = {
        "version": PROTOCOL_VERSION,
        "type": "response",
        "request_id": request_id,
        "ok": ok,
        "state": state,
        "timestamp": now_iso(),
    }
    result.update(extra)
    return result


def event(name: str, state: str, step: str | None = None, **extra: Any) -> Dict[str, Any]:
    result: Dict[str, Any] = {
        "version": PROTOCOL_VERSION,
        "type": "event",
        "event": name,
        "state": state,
        "step": step,
        "timestamp": now_iso(),
    }
    result.update(extra)
    return result
