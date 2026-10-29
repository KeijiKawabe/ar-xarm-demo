import unittest

from app.protocol import ProtocolError, decode_line, encode


class ProtocolTests(unittest.TestCase):
    def test_round_trip(self):
        message = {"version": "1.0", "type": "command", "request_id": "1", "command": "HELLO"}
        self.assertEqual(decode_line(encode(message)), message)

    def test_rejects_wrong_version(self):
        with self.assertRaises(ProtocolError):
            decode_line(b'{"version":"2.0","type":"command","request_id":"1","command":"HELLO"}')

    def test_rejects_missing_request_id(self):
        with self.assertRaises(ProtocolError):
            decode_line(b'{"version":"1.0","type":"command","command":"HELLO"}')
