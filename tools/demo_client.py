import json
import socket
import sys


COMMANDS = ["HELLO", "START", "PICK", "MOVE_A", "MOVE_B", "MOVE_C", "PLACE"]


def read_message(reader):
    line = reader.readline()
    if not line:
        raise RuntimeError("server_closed_connection")
    return json.loads(line)


def send(sock, request_id, command):
    payload = {
        "version": "1.0",
        "type": "command",
        "request_id": request_id,
        "command": command,
    }
    sock.sendall((json.dumps(payload) + "\n").encode("utf-8"))


def main():
    host = sys.argv[1] if len(sys.argv) > 1 else "127.0.0.1"
    port = int(sys.argv[2]) if len(sys.argv) > 2 else 5000
    with socket.create_connection((host, port), timeout=5) as sock:
        sock.settimeout(5)
        reader = sock.makefile("r", encoding="utf-8")
        send(sock, "0", "HELLO")
        while True:
            message = read_message(reader)
            print(message)
            if message.get("event") == "STATUS" and message.get("state") == "READY":
                break
        for index, command in enumerate(COMMANDS[1:], 1):
            send(sock, str(index), command)
            done = False
            while not done:
                message = read_message(reader)
                print(message)
                if message.get("event") == "STEP_COMPLETED" and message.get("step") == command:
                    done = True
                if message.get("event") == "FAULT":
                    raise RuntimeError(message)


if __name__ == "__main__":
    main()
