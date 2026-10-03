"""Lab-A: loopback-only TCP framing demo. Python 3, no third-party packages."""
import socket
import struct
import threading

HOST = "127.0.0.1"
MAX_PAYLOAD = 1024
REQUEST = b"STATUS?"
RESPONSE = b"OK,READY"


def read_exact(conn, count):
    result = bytearray()
    while len(result) < count:
        part = conn.recv(count - len(result))
        if part == b"":
            raise EOFError(f"框架未讀完：{len(result)}/{count} bytes")
        result.extend(part)
    return bytes(result)


def read_frame(conn):
    header = read_exact(conn, 2)
    length = struct.unpack("!H", header)[0]
    if not 1 <= length <= MAX_PAYLOAD:
        raise ValueError(f"不合法的 payload 長度：{length}")
    return read_exact(conn, length)


def frame(payload):
    if not 1 <= len(payload) <= MAX_PAYLOAD:
        raise ValueError("payload 長度不合法")
    return struct.pack("!H", len(payload)) + payload


def main():
    errors = []
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as listener:
        listener.bind((HOST, 0))  # Port 由作業系統分配；只允許本機連線
        listener.listen(1)
        listener.settimeout(3.0)
        port = listener.getsockname()[1]

        def serve_once():
            try:
                conn, peer = listener.accept()
                with conn:
                    conn.settimeout(3.0)
                    print(f"Server accept：{peer[0]}:{peer[1]}")
                    request = read_frame(conn)
                    print(f"Server 收到完整請求：{request!r}")
                    assert request == REQUEST
                    reply = frame(RESPONSE)
                    conn.sendall(reply[:1])
                    conn.sendall(reply[1:])
                    print(f"Server 分兩次呼叫 sendall，共送出 {len(reply)} bytes")
            except Exception as exc:
                errors.append(exc)

        worker = threading.Thread(target=serve_once)
        worker.start()
        try:
            with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as client:
                client.settimeout(3.0)
                client.connect((HOST, port))
                print(f"Client connect：{HOST}:{port}")
                request_frame = frame(REQUEST)
                client.sendall(request_frame[:1])
                client.sendall(request_frame[1:])
                print(f"Client 分兩次呼叫 sendall，共送出 {len(request_frame)} bytes")
                answer = read_frame(client)
                print(f"Client 收到完整回覆：{answer!r}")
                assert answer == RESPONSE
        finally:
            worker.join(timeout=4.0)
        if worker.is_alive():
            raise TimeoutError("Server 執行緒未結束")
        if errors:
            raise errors[0]
    print("本機範例通過；sendall 次數不保證 recv 次數。")


if __name__ == "__main__":
    main()
