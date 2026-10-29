import json
import threading
import time
import unittest
from pathlib import Path

from app.config_loader import load_config
from app.server import DemoServer


class FakeClient:
    def __init__(self):
        self.messages = []

    def sendall(self, data):
        self.messages.extend(json.loads(line) for line in data.decode("utf-8").splitlines() if line)

    def settimeout(self, _timeout):
        pass


class ServerIntegrationTests(unittest.TestCase):
    def test_full_sequence_over_json_lines(self):
        config = load_config(Path(__file__).parents[1] / "config" / "demo.json")
        config["robot"]["simulation_delay_s"] = 0
        server = DemoServer(config)
        client = FakeClient()
        server.client = client

        def send(request_id, command):
            server._handle_line((json.dumps({
                "version": "1.0",
                "type": "command",
                "request_id": request_id,
                "command": command,
            }) + "\n").encode("utf-8"))

        def read_until(event_name=None, step=None):
            while True:
                deadline = time.time() + 2
                while not client.messages and time.time() < deadline:
                    time.sleep(0.01)
                self.assertTrue(client.messages, "server did not emit a response")
                message = client.messages.pop(0)
                if event_name is None:
                    return message
                if message.get("event") == event_name and (step is None or message.get("step") == step):
                    return message

        try:
            send("0", "HELLO")
            read_until()
            self.assertEqual(read_until("STATUS")["state"], "READY")
            for index, command in enumerate(["START", "PICK", "MOVE_A", "MOVE_B", "MOVE_C", "PLACE"], 1):
                send(str(index), command)
                completed = read_until("STEP_COMPLETED", command)
                self.assertIn("state", completed)
            self.assertEqual(server.machine.state.value, "SUCCESS")
        finally:
            server.close()

    def test_stop_interrupts_active_motion(self):
        config = load_config(Path(__file__).parents[1] / "config" / "demo.json")
        config["robot"]["simulation_delay_s"] = 0.2
        server = DemoServer(config)
        client = FakeClient()
        server.client = client

        def send(request_id, command):
            server._handle_line((json.dumps({
                "version": "1.0",
                "type": "command",
                "request_id": request_id,
                "command": command,
            }) + "\n").encode("utf-8"))

        send("0", "HELLO")
        send("1", "START")
        deadline = time.time() + 2
        while server.active_motion and time.time() < deadline:
            time.sleep(0.01)
        send("2", "PICK")
        send("3", "STOP")
        deadline = time.time() + 2
        while server.active_motion and time.time() < deadline:
            time.sleep(0.01)
        self.assertEqual(server.machine.state.value, "STOPPED")
        self.assertTrue(any(message.get("event") == "STOPPED" for message in client.messages))
        server.close()
