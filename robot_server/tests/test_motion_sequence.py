import copy
import json
import unittest
from pathlib import Path

from app.config_loader import load_config
from app.motion_sequence import MotionSequence
from app.sim_adapter import SimRobotAdapter


class MotionSequenceTests(unittest.TestCase):
    def setUp(self):
        config_path = Path(__file__).parents[1] / "config" / "demo.json"
        self.config = load_config(config_path)
        self.config["robot"]["simulation_delay_s"] = 0
        self.adapter = SimRobotAdapter(self.config)
        self.sequence = MotionSequence(self.config, self.adapter, lambda *_: None)

    def test_pick_move_place(self):
        self.sequence.execute("START")
        self.sequence.execute("PICK")
        self.sequence.execute("MOVE_A")
        self.sequence.execute("MOVE_B")
        self.sequence.execute("MOVE_C")
        self.sequence.execute("PLACE")
        self.assertEqual(self.adapter.gripper, "OPEN")
        self.assertEqual(self.adapter.current_pose, self.config["coordinates"]["PLACE_RETRACT"])
