import unittest

from app.state_machine import DemoState, StateMachine


class StateMachineTests(unittest.TestCase):
    def test_normal_sequence(self):
        machine = StateMachine()
        machine.connect()
        machine.ready()
        for command, expected in [
            ("START", DemoState.STARTED),
            ("PICK", DemoState.PICKING),
            ("MOVE_A", DemoState.MOVING_A),
            ("MOVE_B", DemoState.MOVING_B),
            ("MOVE_C", DemoState.MOVING_C),
            ("PLACE", DemoState.PLACING),
        ]:
            machine.begin(command)
            self.assertEqual(machine.state, expected)
            machine.complete(command)
        self.assertEqual(machine.state, DemoState.SUCCESS)

    def test_rejects_place_before_move_c(self):
        machine = StateMachine()
        machine.connect()
        machine.ready()
        machine.begin("START")
        machine.complete("START")
        with self.assertRaises(ValueError):
            machine.begin("PLACE")
