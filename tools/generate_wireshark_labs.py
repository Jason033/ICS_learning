"""Generate deterministic, fictional Wireshark practice captures (classic pcap)."""
from __future__ import annotations

import ipaddress
import struct
from pathlib import Path

OUT = Path(__file__).resolve().parents[1] / 'docs' / 'assets' / 'labs'
OUT.mkdir(parents=True, exist_ok=True)
BASE_TIME = 1767225600  # Fixed teaching timeline; never captured from a real system.
A = '192.0.2.10'
B = '198.51.100.20'
DNS = '192.0.2.53'
MAC_A = bytes.fromhex('02000000000a')
MAC_B = bytes.fromhex('02000000000b')
MAC_DNS = bytes.fromhex('020000000035')


def checksum(data: bytes) -> int:
    if len(data) % 2:
        data += b'\0'
    words = struct.unpack(f'!{len(data) // 2}H', data)
    total = sum(words)
    while total >> 16:
        total = (total & 0xffff) + (total >> 16)
    return (~total) & 0xffff


def ip_bytes(value: str) -> bytes:
    return ipaddress.IPv4Address(value).packed


def ethernet(payload: bytes, src: bytes, dst: bytes) -> bytes:
    return dst + src + struct.pack('!H', 0x0800) + payload


def ipv4(payload: bytes, src: str, dst: str, protocol: int, ident: int) -> bytes:
    header = struct.pack('!BBHHHBBH4s4s', 0x45, 0, 20 + len(payload), ident,
                         0x4000, 64, protocol, 0, ip_bytes(src), ip_bytes(dst))
    header = header[:10] + struct.pack('!H', checksum(header)) + header[12:]
    return header + payload


def pseudo(src: str, dst: str, protocol: int, length: int) -> bytes:
    return ip_bytes(src) + ip_bytes(dst) + struct.pack('!BBH', 0, protocol, length)


def udp(payload: bytes, src: str, dst: str, sport: int, dport: int) -> bytes:
    length = 8 + len(payload)
    header = struct.pack('!HHHH', sport, dport, length, 0)
    value = checksum(pseudo(src, dst, 17, length) + header + payload) or 0xffff
    return struct.pack('!HHHH', sport, dport, length, value) + payload


def tcp(payload: bytes, src: str, dst: str, sport: int, dport: int,
        seq: int, ack: int, flags: int) -> bytes:
    header = struct.pack('!HHIIHHHH', sport, dport, seq, ack, (5 << 12) | flags,
                         64240, 0, 0)
    value = checksum(pseudo(src, dst, 6, len(header) + len(payload)) + header + payload)
    return header[:16] + struct.pack('!H', value) + header[18:] + payload


def packet(payload: bytes, src: str, dst: str, protocol: int, ident: int,
           mac_src: bytes, mac_dst: bytes) -> bytes:
    return ethernet(ipv4(payload, src, dst, protocol, ident), mac_src, mac_dst)


def write_pcap(name: str, frames: list[tuple[int, bytes]]) -> None:
    result = bytearray(struct.pack('<IHHIIII', 0xa1b2c3d4, 2, 4, 0, 0, 65535, 1))
    for millis, frame in sorted(frames, key=lambda item: item[0]):
        result += struct.pack('<IIII', BASE_TIME + millis // 1000,
                              (millis % 1000) * 1000, len(frame), len(frame))
        result += frame
    (OUT / name).write_bytes(result)
    print(name, len(frames), 'frames', len(result), 'bytes')


def dns_name(name: str) -> bytes:
    return b''.join(bytes([len(part)]) + part.encode('ascii') for part in name.split('.')) + b'\0'


def first_capture() -> None:
    frames = []
    ident = 1

    def add_udp(ms: int, src: str, dst: str, sport: int, dport: int,
                data: bytes, smac: bytes, dmac: bytes) -> None:
        nonlocal ident
        frames.append((ms, packet(udp(data, src, dst, sport, dport), src, dst, 17,
                                  ident, smac, dmac)))
        ident += 1

    def add_tcp(ms: int, src: str, dst: str, sport: int, dport: int,
                seq: int, ack: int, flags: int, data: bytes,
                smac: bytes, dmac: bytes) -> None:
        nonlocal ident
        frames.append((ms, packet(tcp(data, src, dst, sport, dport, seq, ack, flags),
                                  src, dst, 6, ident, smac, dmac)))
        ident += 1

    name = dns_name('lab.example')
    question = name + struct.pack('!HH', 1, 1)
    query = struct.pack('!HHHHHH', 0x1234, 0x0100, 1, 0, 0, 0) + question
    answer = (struct.pack('!HHHHHH', 0x1234, 0x8180, 1, 1, 0, 0) + question +
              b'\xc0\x0c' + struct.pack('!HHIH', 1, 1, 60, 4) + ip_bytes(B))
    add_udp(0, A, DNS, 53000, 53, query, MAC_A, MAC_DNS)
    add_udp(18, DNS, A, 53, 53000, answer, MAC_DNS, MAC_A)

    request = b'GET /status HTTP/1.1\r\nHost: lab.example\r\nConnection: close\r\n\r\n'
    body = b'{"status":"ready"}'
    response = (b'HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: ' +
                str(len(body)).encode() + b'\r\nConnection: close\r\n\r\n' + body)
    add_tcp(40, A, B, 54000, 8080, 1000, 0, 0x02, b'', MAC_A, MAC_B)  # SYN
    add_tcp(58, B, A, 8080, 54000, 5000, 1001, 0x12, b'', MAC_B, MAC_A)  # SYN ACK
    add_tcp(62, A, B, 54000, 8080, 1001, 5001, 0x10, b'', MAC_A, MAC_B)
    add_tcp(80, A, B, 54000, 8080, 1001, 5001, 0x18, request, MAC_A, MAC_B)
    client_next = 1001 + len(request)
    add_tcp(95, B, A, 8080, 54000, 5001, client_next, 0x10, b'', MAC_B, MAC_A)
    add_tcp(120, B, A, 8080, 54000, 5001, client_next, 0x18, response, MAC_B, MAC_A)
    server_next = 5001 + len(response)
    add_tcp(125, A, B, 54000, 8080, client_next, server_next, 0x10, b'', MAC_A, MAC_B)
    add_tcp(150, B, A, 8080, 54000, server_next, client_next, 0x11, b'', MAC_B, MAC_A)
    add_tcp(155, A, B, 54000, 8080, client_next, server_next + 1, 0x10, b'', MAC_A, MAC_B)
    add_tcp(170, A, B, 54000, 8080, client_next, server_next + 1, 0x11, b'', MAC_A, MAC_B)
    add_tcp(190, B, A, 8080, 54000, server_next + 1, client_next + 1, 0x10, b'', MAC_B, MAC_A)
    write_pcap('wireshark-first-capture.pcap', frames)



def tcp_issues() -> None:
    frames: list[tuple[int, bytes]] = []
    ident = 100

    def add(ms: int, src: str, dst: str, sport: int, dport: int,
            seq: int, ack: int, flags: int, data: bytes,
            smac: bytes, dmac: bytes) -> None:
        nonlocal ident
        frames.append((ms, packet(tcp(data, src, dst, sport, dport, seq, ack, flags),
                                  src, dst, 6, ident, smac, dmac)))
        ident += 1

    # Case 1: three identical SYN attempts, no response at this observation point.
    for ms in (0, 1000, 3000):
        add(ms, A, B, 54001, 7000, 10000, 0, 0x02, b'', MAC_A, MAC_B)

    # Case 2: TCP transport acknowledges the request, but no application reply.
    add(5000, A, B, 54002, 7001, 20000, 0, 0x02, b'', MAC_A, MAC_B)
    add(5020, B, A, 7001, 54002, 30000, 20001, 0x12, b'', MAC_B, MAC_A)
    add(5030, A, B, 54002, 7001, 20001, 30001, 0x10, b'', MAC_A, MAC_B)
    request = b'STATUS?\n'
    add(5060, A, B, 54002, 7001, 20001, 30001, 0x18, request, MAC_A, MAC_B)
    add(5080, B, A, 7001, 54002, 30001, 20001 + len(request), 0x10, b'', MAC_B, MAC_A)
    # The capture ends here: silence is a property of this file, not proof of cause.
    write_pcap('wireshark-tcp-cases.pcap', frames)


def sip_text(start: str, headers: list[tuple[str, str]], body: str = '') -> bytes:
    body_bytes = body.encode('ascii')
    lines = [start] + [f'{name}: {value}' for name, value in headers]
    if body:
        lines.append('Content-Type: application/sdp')
    lines.append(f'Content-Length: {len(body_bytes)}')
    return ('\r\n'.join(lines) + '\r\n\r\n').encode('ascii') + body_bytes


def sdp(address: str, port: int) -> str:
    return ('v=0\r\n'
            f'o=lab 1 1 IN IP4 {address}\r\n'
            's=Fictional training call\r\n'
            f'c=IN IP4 {address}\r\n'
            't=0 0\r\n'
            f'm=audio {port} RTP/AVP 0\r\n'
            'a=rtpmap:0 PCMU/8000\r\n')


def rtp(seq: int, timestamp: int, ssrc: int) -> bytes:
    # RTP v2, payload type 0 = PCMU; fixed silence bytes, no recorded audio.
    return struct.pack('!BBHII', 0x80, 0, seq, timestamp, ssrc) + b'\xff' * 160


def sip_rtp() -> None:
    frames: list[tuple[int, bytes]] = []
    ident = 200

    def add(ms: int, src: str, dst: str, sport: int, dport: int,
            data: bytes, smac: bytes, dmac: bytes) -> None:
        nonlocal ident
        frames.append((ms, packet(udp(data, src, dst, sport, dport), src, dst, 17,
                                  ident, smac, dmac)))
        ident += 1

    call_id = 'training-call-001@example.invalid'
    via = 'SIP/2.0/UDP 192.0.2.10:5060;branch=z9hG4bK-training-1'
    basic = [('Via', via), ('From', '<sip:alice@example.invalid>;tag=lab-a'),
             ('To', '<sip:bob@example.invalid>'), ('Call-ID', call_id),
             ('CSeq', '1 INVITE')]
    invite = sip_text('INVITE sip:bob@example.invalid SIP/2.0',
                      basic + [('Contact', '<sip:alice@192.0.2.10:5060>')], sdp(A, 40000))
    add(0, A, B, 5060, 5060, invite, MAC_A, MAC_B)
    for ms, status in [(20, '100 Trying'), (80, '180 Ringing')]:
        response_headers = [(key, value.replace('sip:bob@example.invalid>', 'sip:bob@example.invalid>;tag=lab-b')
                             if key == 'To' and ms == 80 else value) for key, value in basic]
        response = sip_text('SIP/2.0 ' + status, response_headers)
        add(ms, B, A, 5060, 5060, response, MAC_B, MAC_A)
    ok_headers = [(key, value.replace('sip:bob@example.invalid>', 'sip:bob@example.invalid>;tag=lab-b')
                   if key == 'To' else value) for key, value in basic]
    ok = sip_text('SIP/2.0 200 OK',
                  ok_headers + [('Contact', '<sip:bob@198.51.100.20:5060>')], sdp(B, 40002))
    add(120, B, A, 5060, 5060, ok, MAC_B, MAC_A)
    ack_headers = [(key, '1 ACK' if key == 'CSeq' else
                    value.replace('training-1', 'training-ack') if key == 'Via' else value)
                   for key, value in ok_headers]
    add(140, A, B, 5060, 5060,
        sip_text('ACK sip:bob@example.invalid SIP/2.0', ack_headers), MAC_A, MAC_B)

    for index, ms in enumerate((160, 180, 200, 220, 240)):
        add(ms, A, B, 40000, 40002, rtp(100 + index, 160 * index, 0x11111111),
            MAC_A, MAC_B)
    # Packet with sequence 302 is absent from the teaching capture. This is
    # a capture-level sequence gap, not proof of where the packet went.
    for seq, ms in ((300, 165), (301, 185), (303, 225), (304, 245)):
        add(ms, B, A, 40002, 40000, rtp(seq, (seq - 300) * 160, 0x22222222),
            MAC_B, MAC_A)

    bye_headers = [(key, '2 BYE' if key == 'CSeq' else
                    value.replace('training-1', 'training-bye') if key == 'Via' else value)
                   for key, value in ok_headers]
    add(300, A, B, 5060, 5060,
        sip_text('BYE sip:bob@example.invalid SIP/2.0', bye_headers), MAC_A, MAC_B)
    add(320, B, A, 5060, 5060, sip_text('SIP/2.0 200 OK', bye_headers), MAC_B, MAC_A)
    write_pcap('wireshark-sip-rtp.pcap', frames)


if __name__ == '__main__':
    first_capture()
    tcp_issues()
    sip_rtp()
