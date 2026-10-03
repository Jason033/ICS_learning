"""Lab-A: loopback-only UDP datagram demo. Python 3, no third-party packages."""
import socket
import threading

HOST = "127.0.0.1"
REQUEST = b"STATUS?"
RESPONSE = b"OK,READY"


def main():
    errors = []
    with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as server:
        server.bind((HOST, 0))
        server.settimeout(3.0)
        endpoint = server.getsockname()

        def serve_once():
            try:
                request, peer = server.recvfrom(1024)
                print(f"Server recvfrom：{request!r}，來源 {peer[0]}:{peer[1]}")
                assert request == REQUEST
                sent = server.sendto(RESPONSE, peer)
                assert sent == len(RESPONSE)
                print(f"Server sendto：{sent} bytes")
            except Exception as exc:
                errors.append(exc)

        worker = threading.Thread(target=serve_once)
        worker.start()
        try:
            with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as client:
                client.settimeout(3.0)
                sent = client.sendto(REQUEST, endpoint)
                assert sent == len(REQUEST)
                print(f"Client sendto：{sent} bytes，目標 {endpoint[0]}:{endpoint[1]}")
                answer, source = client.recvfrom(1024)
                print(f"Client recvfrom：{answer!r}，來源 {source[0]}:{source[1]}")
                assert source == endpoint, "回覆來源不是預期端點"
                assert answer == RESPONSE
        finally:
            worker.join(timeout=4.0)
        if worker.is_alive():
            raise TimeoutError("Server 執行緒未結束")
        if errors:
            raise errors[0]
    print("本機範例通過；sendto 返回值不是遠端已處理的證據。")


if __name__ == "__main__":
    main()
