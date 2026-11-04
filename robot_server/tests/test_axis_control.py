import copy
import json
import threading
import unittest
from pathlib import Path
from unittest.mock import Mock
from app.config_loader import load_config
from app.server import DemoServer
from app.safety import SafetyError
from app.state_machine import DemoState, AXIS_COMMANDS
from app.xarm_adapter import XArmAdapter

class AxisControlTests(unittest.TestCase):
    def setUp(self):
        self.config = load_config(Path(__file__).parents[1] / 'config/demo.json')
        self.config['robot']['simulation_delay_s'] = 0
        self.server = DemoServer(self.config)
        self.messages = []
        self.server._send = self.messages.append
        self.number = 0
        self.send('HELLO'); self.send('START'); self.wait(); self.send('PICK'); self.wait()
    def tearDown(self): self.server.close()
    def send(self, command, request_id=None):
        self.number += 1
        self.server._handle_line(json.dumps(dict(version='1.0', type='command', command=command,
            request_id=request_id or str(self.number))).encode())
    def wait(self):
        if self.server.worker:
            self.server.worker.join(2)
            self.assertFalse(self.server.worker.is_alive(), 'worker did not finish')
    def test_six_directions_preserve_pose(self):
        for command in sorted(AXIS_COMMANDS):
            expected = self.server.adapter.get_pose()
            expected['XYZ'.index(command.split('_')[1])] += 10 if command.endswith('PLUS') else -10
            self.send(command); self.wait()
            self.assertEqual(self.server.adapter.get_pose(), expected)
            self.assertEqual(self.server.machine.state, DemoState.PICKED)
    def test_configurable_step(self):
        self.config['axis_control']['step_mm'] = 5
        before = self.server.adapter.get_pose()
        self.send('MOVE_X_PLUS'); self.wait(); self.send('GET_STATUS')
        self.assertEqual(self.server.adapter.get_pose()[0], before[0]+5)
        self.assertEqual(self.messages[-1]['axis_step_mm'], 5)
    def test_limit_rejection(self):
        self.server.adapter.current_pose[0] = self.config['limits']['x_mm'][1]
        before = self.server.adapter.get_pose()
        self.send('MOVE_X_PLUS'); self.wait()
        self.assertEqual(self.messages[-1]['code'], 'OUT_OF_RANGE')
        self.assertEqual(self.server.adapter.get_pose(), before)
        self.assertEqual(self.server.machine.state, DemoState.PICKED)
        self.send('MOVE_X_MINUS'); self.wait()
        self.assertEqual(self.server.adapter.get_pose()[0], before[0]-10)
    def test_read_failure(self):
        self.server.adapter.get_pose = Mock(side_effect=RuntimeError('read failed'))
        self.server.adapter.move_pose = Mock()
        self.send('MOVE_X_PLUS'); self.wait()
        self.assertEqual(self.messages[-1]['code'], 'POSE_READ_FAILED')
        self.server.adapter.move_pose.assert_not_called()
    def test_duplicate_request(self):
        before = self.server.adapter.get_pose()
        self.send('MOVE_Y_PLUS', 'same'); self.wait()
        self.send('MOVE_Y_PLUS', 'same'); self.wait()
        self.assertEqual(self.messages[-1]['code'], 'DUPLICATE_REQUEST')
        self.assertEqual(self.server.adapter.get_pose()[1], before[1]+10)
    def test_busy_stop_and_reset(self):
        self.config['robot']['simulation_delay_s'] = 0.2
        before = self.server.adapter.get_pose()
        self.send('MOVE_X_PLUS'); self.send('MOVE_Y_PLUS')
        self.assertEqual(self.messages[-1]['code'], 'BUSY')
        self.send('STOP'); self.wait(); self.send('HELLO')
        self.assertEqual(self.server.machine.state, DemoState.STOPPED)
        self.assertEqual(self.server.adapter.get_pose(), before)
        self.send('MOVE_Z_PLUS'); self.assertEqual(self.messages[-1]['code'], 'INVALID_STATE')
        self.send('RESET'); self.wait(); self.assertEqual(self.server.machine.state, DemoState.READY)
    def test_stop_before_worker(self):
        entered = threading.Event(); release = threading.Event()
        original = self.server.sequence.execute
        def gated(*args):
            entered.set(); release.wait(1); return original(*args)
        self.server.sequence.execute = gated
        self.send('MOVE_X_PLUS'); self.assertTrue(entered.wait(1))
        before = self.server.adapter.get_pose()
        self.send('STOP'); release.set(); self.wait()
        self.assertEqual(self.server.machine.state, DemoState.STOPPED)
        self.assertEqual(self.server.adapter.get_pose(), before)
    def test_disconnect(self):
        class ClosedClient:
            def settimeout(self, _): pass
            def recv(self, _): return b''
        self.config['robot']['simulation_delay_s'] = 0.2
        self.send('MOVE_Z_PLUS'); self.server._client_loop(ClosedClient()); self.wait()
        self.assertEqual(self.server.machine.state, DemoState.STOPPED)
    def test_arbitrary_order_then_place(self):
        for cmd in ('MOVE_Z_PLUS', 'MOVE_X_MINUS', 'MOVE_Y_PLUS', 'PLACE'):
            self.send(cmd); self.wait()
        self.assertEqual(self.server.machine.state, DemoState.SUCCESS)
        self.assertEqual(self.server.adapter.get_pose(), self.config['coordinates']['PLACE_RETRACT'])
    def test_bad_settings(self):
        for value in (0, -1, 26, float('nan'), float('inf'), True):
            config = copy.deepcopy(self.config); config['axis_control']['step_mm'] = value
            with self.assertRaises(SafetyError): DemoServer(config)
    def test_before_pick(self):
        self.server.machine.state = DemoState.STARTED
        self.send('MOVE_X_PLUS'); self.assertEqual(self.messages[-1]['code'], 'INVALID_STATE')
    def test_real_pose_read(self):
        adapter = XArmAdapter.__new__(XArmAdapter); adapter.config = self.config; adapter.arm = Mock()
        adapter.arm.get_position.return_value = (0, [300, 0, 200, 180, 0, 0])
        self.assertEqual(adapter.get_pose(), [300, 0, 200, 180, 0, 0])
        adapter.arm.get_position.return_value = (1, [])
        with self.assertRaises(RuntimeError): adapter.get_pose()
