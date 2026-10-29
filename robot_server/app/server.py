import argparse
import json
import logging
import socket
import threading
from pathlib import Path
from typing import Any, Optional

from .config_loader import load_config
from .motion_sequence import MotionSequence
from .protocol import ProtocolError, decode_line, encode, event, response
from .safety import validate_motion_config
from .sim_adapter import SimRobotAdapter
from .state_machine import DemoState, StateMachine
from .xarm_adapter import XArmAdapter


LOG = logging.getLogger("xarm_demo")


class DemoServer:
    def __init__(self, config: dict[str, Any]) -> None:
        validate_motion_config(config)
        self.config = config
        self.machine = StateMachine()
        self.client: Optional[socket.socket] = None
        self.client_lock = threading.Lock()
        self.motion_lock = threading.Lock()
        self.active_motion = False
        self.stop_requested = threading.Event()
        self.stop_event = threading.Event()
        mode = config["server"].get("mode", "simulation")
        self.adapter = SimRobotAdapter(config) if mode == "simulation" else XArmAdapter(config)
        self.sequence = MotionSequence(config, self.adapter, self._notify)

    def serve(self) -> None:
        host = self.config["server"].get("host", "0.0.0.0")
        port = int(self.config["server"].get("port", 5000))
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as sock:
            sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
            sock.bind((host, port))
            sock.listen(1)
            LOG.info("server listening on %s:%s", host, port)
            while not self.stop_event.is_set():
                sock.settimeout(1.0)
                try:
                    client, address = sock.accept()
                except socket.timeout:
                    continue
                with self.client_lock:
                    if self.client is not None:
                        client.close()
                        continue
                    self.client = client
                LOG.info("client connected: %s", address)
                self._client_loop(client)
                with self.client_lock:
                    self.client = None
                if self.machine.state not in (DemoState.FAULT, DemoState.STOPPED):
                    self.machine.state = DemoState.CONNECTED

    def _client_loop(self, client: socket.socket) -> None:
        self.machine.connect()
        client.settimeout(1.0)
        buffer = b""
        try:
            while not self.stop_event.is_set():
                try:
                    chunk = client.recv(4096)
                except socket.timeout:
                    continue
                if not chunk:
                    break
                buffer += chunk
                while b"\n" in buffer:
                    line, buffer = buffer.split(b"\n", 1)
                    if line.strip():
                        self._handle_line(line)
        except (ConnectionError, OSError):
            LOG.exception("client connection failed")
        finally:
            if self.active_motion:
                self.adapter.stop()

    def _handle_line(self, line: bytes) -> None:
        try:
            message = decode_line(line)
        except ProtocolError as exc:
            self._send({"version": "1.0", "type": "error", "code": str(exc), "recoverable": False})
            return
        command = message["command"]
        request_id = message["request_id"]
        if command == "HELLO":
            self.machine.ready()
            self._send(response(request_id, True, self.machine.state.value, event="WELCOME"))
            self._send(self._status_event())
            return
        if command == "GET_STATUS":
            self._send(self._status_event(request_id))
            return
        if command == "HEARTBEAT":
            self._send(response(request_id, True, self.machine.state.value, event="HEARTBEAT_ACK"))
            return
        if command == "STOP":
            if self.active_motion:
                self.stop_requested.set()
                try:
                    self.adapter.stop()
                except Exception as exc:
                    LOG.exception("stop failed")
                    self.machine.fault()
                    self._send(response(request_id, False, self.machine.state.value, code="STOP_FAILED", message=str(exc)))
                    return
                self._send(response(request_id, True, self.machine.state.value, accepted=True, event="STOP_REQUESTED"))
                return
            try:
                self.machine.begin(command)
            except ValueError as exc:
                self._send(response(request_id, False, self.machine.state.value, code="INVALID_STATE", message=str(exc), recoverable=True))
                return
            self._send(response(request_id, True, self.machine.state.value, accepted=True))
            self._start_motion(command, request_id)
            return
        if self.active_motion:
            self._send(response(request_id, False, self.machine.state.value, code="BUSY", message="motion_in_progress"))
            return
        try:
            self.machine.begin(command)
        except ValueError as exc:
            self._send(response(request_id, False, self.machine.state.value, code="INVALID_STATE", message=str(exc), recoverable=True))
            return
        self._send(response(request_id, True, self.machine.state.value, accepted=True))
        self._start_motion(command, request_id)

    def _start_motion(self, command: str, request_id: str) -> None:
        if self.active_motion:
            self._send(response(request_id, False, self.machine.state.value, code="BUSY", message="motion_in_progress"))
            return
        self.active_motion = True
        thread = threading.Thread(target=self._run_motion, args=(command, request_id), daemon=True)
        thread.start()

    def _run_motion(self, command: str, request_id: str) -> None:
        with self.motion_lock:
            try:
                self.sequence.execute(command)
                self.machine.complete(command)
                self._send(self._step_event(command, request_id))
                if command == "PLACE":
                    self._send(self._status_event())
            except Exception as exc:
                if self.stop_requested.is_set():
                    self.machine.stopped()
                    self._send(event("STOPPED", self.machine.state.value, "STOP", request_id=request_id, reason="stop_requested"))
                    return
                LOG.exception("motion failed: %s", command)
                self.machine.fault()
                self._send(event("FAULT", self.machine.state.value, command, request_id=request_id, error=str(exc)))
            finally:
                self.stop_requested.clear()
                self.active_motion = False

    def _step_event(self, command: str, request_id: str) -> dict[str, Any]:
        return event("STEP_COMPLETED", self.machine.state.value, command, request_id=request_id, robot_state=self.adapter.status())

    def _status_event(self, request_id: str | None = None) -> dict[str, Any]:
        return event("STATUS", self.machine.state.value, robot_state=self.adapter.status(), request_id=request_id)

    def _notify(self, name: str, step: str | None) -> None:
        self._send(event(name, self.machine.state.value, step))

    def _send(self, message: dict[str, Any]) -> None:
        with self.client_lock:
            client = self.client
            if client is None:
                return
            try:
                client.sendall(encode(message))
            except OSError:
                LOG.exception("send failed")

    def close(self) -> None:
        self.stop_event.set()
        if self.active_motion:
            self.stop_requested.set()
            try:
                self.adapter.stop()
            except Exception:
                LOG.exception("shutdown stop failed")
        self.adapter.disconnect()


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--config", default=str(Path(__file__).parents[1] / "config" / "demo.json"))
    args = parser.parse_args()
    config = load_config(args.config)
    log_handlers: list[logging.Handler] = [logging.StreamHandler()]
    log_file = config["server"].get("log_file")
    if log_file:
        log_path = Path(log_file)
        if not log_path.is_absolute(): log_path = Path.cwd() / log_path
        log_path.parent.mkdir(parents=True, exist_ok=True)
        log_handlers.append(logging.FileHandler(log_path, encoding="utf-8"))
    logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(message)s", handlers=log_handlers)
    server = DemoServer(config)
    try:
        server.serve()
    except KeyboardInterrupt:
        pass
    finally:
        server.close()


if __name__ == "__main__":
    main()
